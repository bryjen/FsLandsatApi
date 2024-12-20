module LandsatReflectance.Api.Handlers.SceneHandler

open System

open Microsoft.FSharp.Core
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open Giraffe

open FsToolkit.ErrorHandling

open LandsatReflectance.Api.Models.Usgs.Scene
open LandsatReflectance.Api.Models.ApiResponse
open LandsatReflectance.Api.Services.UsgsSceneService



let private assertValidHttpMethod (ctx: HttpContext) =
    match ctx.Request.Method with
    | "GET" ->
        Ok ()
    | method ->
        Error $"Invalid HTTP method '{method}'"
        
let private getRequiredQueryParameter (ctx: HttpContext) (paramName: string) =
    match ctx.TryGetQueryStringValue paramName with
    | None -> Error $"Could not find the required query parameter \"{paramName}\""
    | Some value -> Ok value

let private tryParseToInt (paramName: string) (paramValue: string) =
    match Int32.TryParse(paramValue) with
    | true, value -> Ok value
    | false, _ -> Error $"Could not parse the value of  \"{paramName}\" to an int"

let private tryGetParameters (ctx: HttpContext) =
    result {
        let! path = getRequiredQueryParameter ctx "path" |> Result.bind (tryParseToInt "path")
        let! row = getRequiredQueryParameter ctx "row" |> Result.bind (tryParseToInt "row")
        let! results =
            match ctx.TryGetQueryStringValue "results" with
            | None -> Ok 10
            | Some value -> tryParseToInt "results" value
            
        return path, row, results
    }
    
    
// wrapper around 'UsgsSceneService' that logs and processes output
let private getScenes (ctx: HttpContext) (logger: ILogger) (requestId: Guid) (infoTuple: int * int * int) =
    task {
        let path, row, results = infoTuple
        let sceneService = ctx.RequestServices.GetRequiredService<UsgsSceneService>()
        let! sceneDataResult = sceneService.GetScenes(path, row, results)
        match sceneDataResult with
        | Ok sceneData ->
            let successfulResponse =
                { RequestGuid = requestId
                  ErrorMessage = None
                  Data = Some sceneData }
            logger.LogInformation($"[{requestId}] Successful \"GET\", returned {sceneData.Length} item(s)")
            return successfulResponse
            
        | Error sceneRequestError ->
            let unsuccessfulResponse =
                { RequestGuid = Guid.NewGuid()
                  ErrorMessage = Some (sceneRequestError.ToString())
                  Data = None }
            logger.LogInformation($"[{requestId}] Failed \"GET\". Bad request with error message \"{sceneRequestError.ToString()}\"")
            return unsuccessfulResponse
    }


let sceneHandler: HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let requestId: Guid =
            match ctx.Items.TryGetValue("requestId") with
            | true, value -> value :?> Guid
            | false, _ -> Guid.Empty
            
        let loggerFactory = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
        let logger = loggerFactory.CreateLogger("FsLandsatApi.Handlers.SceneHandler")
        
        assertValidHttpMethod ctx
        |> Result.bind (fun _ -> tryGetParameters ctx)
        |> Result.map (getScenes ctx logger requestId)
        |> function
           | Ok validRequestResponseTask -> task {
                   let! validRequestResponse = validRequestResponseTask
                   return! (Successful.ok (json<ApiResponse<SimplifiedSceneData array>> validRequestResponse)) next ctx
               }
           | Error invalidRequestErrorMsg -> task {
                   let asApiResponseObj =
                       { RequestGuid = requestId
                         ErrorMessage = Some invalidRequestErrorMsg
                         Data = None }
                   logger.LogInformation($"[{requestId}] Failed \"GET\". Bad request with error message \"{invalidRequestErrorMsg}\"")
                   return! (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
               }