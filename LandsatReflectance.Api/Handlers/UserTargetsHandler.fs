module LandsatReflectance.Api.Handlers.UserTargetsHandler

open System
open System.IO
open System.Security.Claims
open System.Text.Json

open Microsoft.AspNetCore.Http.Json
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Microsoft.FSharp.Core
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection

open Giraffe

open FsToolkit.ErrorHandling

open LandsatReflectance.Api.Models.User
open LandsatReflectance.Api.Models.ApiResponse
open LandsatReflectance.Api.Services.DbUserService
open LandsatReflectance.Api.Services.DbUserTargetService



let private getRequestId (ctx: HttpContext) =
    match ctx.Items.TryGetValue("requestId") with
    | true, value -> value :?> Guid
    | false, _ -> Guid.Empty
    
let private getRequiredQueryParameter (ctx: HttpContext) (paramName: string) =
    match ctx.TryGetQueryStringValue paramName with
    | None -> Error $"Could not find the required query parameter \"{paramName}\""
    | Some value -> Ok value

let private tryParseToGuid (paramName: string) (paramValue: string) =
    match Guid.TryParse(paramValue) with
    | true, value -> Ok value
    | false, _ -> Error $"Could not parse the value of  \"{paramName}\" to an int"
    
let private tryGetTargetId (ctx: HttpContext) =
    getRequiredQueryParameter ctx "target-id" |> Result.bind (tryParseToGuid "path")
    
    
/// 'Target' without the attached 'UserId', to prevent user ids from being exposed to end users.
[<CLIMutable>]
type SimplifiedTarget =
    { Id: Guid
      Path: int
      Row: int
      Latitude: double
      Longitude: double
      MinCloudCoverFilter: double
      MaxCloudCoverFilter: double
      NotificationOffset: TimeSpan }
with
    static member FromTarget(target: Target) =
        { Id = target.Id
          Path = target.Path
          Row = target.Row
          Latitude = target.Latitude
          Longitude = target.Longitude
          MinCloudCoverFilter = target.MinCloudCoverFilter
          MaxCloudCoverFilter = target.MaxCloudCoverFilter
          NotificationOffset = target.NotificationOffset }


[<AutoOpen>]
module private TokenHelpers =
    let tryGetUserEmail (ctx: HttpContext): Result<string, string> =
        try
            match ctx.User.Identity.IsAuthenticated with
            | true ->
                let emailClaimOption = ctx.User.Claims |> Seq.filter (fun (claim: Claim) -> claim.Type = ClaimTypes.Email) |> Seq.tryHead
                match emailClaimOption with
                | Some value ->
                    Ok value.Value
                | None ->
                    Error "Auth error: Could not infer the user email from the auth payload"
            | false ->
                Error "User is not authenticated"
        with
        | ex ->
            Error ex.Message

module UserTargetsGet =
    let private tranformGetResult next ctx (logger: ILogger) requestId result =
        match result with
        | Ok targets ->
            let asApiResponseObj = { RequestGuid = requestId; ErrorMessage = None; Data = Some targets }
            logger.LogInformation($"[{requestId}] Successfully fetched \"{List.length targets}\" target(s)")
            logger.LogDebug($"[{requestId}] {JsonSerializer.Serialize(targets)}")
            (Successful.ok (json<ApiResponse<SimplifiedTarget list>> asApiResponseObj)) next ctx
        | Error errorMsg -> 
            let asApiResponseObj = { RequestGuid = requestId; ErrorMessage = Some errorMsg; Data = None }
            logger.LogInformation($"[{requestId}] Failed to get targets: \"{errorMsg}\"")
            (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
        
    
    let handler (next: HttpFunc) (ctx: HttpContext) : HttpFuncResult =
        task {
            let requestId = getRequestId ctx
            
            let loggerFactory = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            let logger = loggerFactory.CreateLogger("FsLandsatApi.Handlers.UserTargetsHandler.UserTargetsPost")
            
            let dbUserTargetService = ctx.RequestServices.GetRequiredService<DbUserTargetService>()
            
            return!
                tryGetUserEmail ctx
                |> TaskResult.ofResult
                |> TaskResult.bind dbUserTargetService.TryGetTargets
                |> TaskResult.map (List.map SimplifiedTarget.FromTarget)
                |> Task.bind (tranformGetResult next ctx logger requestId)
        }
        
        
module UserTargetsPost =
    [<CLIMutable>]
    type CreateTargetRequest =
        { Path: int
          Row: int
          Latitude: double
          Longitude: double
          MinCloudCoverFilter: double
          MaxCloudCoverFilter: double
          NotificationOffset: TimeSpan }
    with
        member this.CreateTargetDto(userId: Guid) =
            { UserId = userId
              Id = Guid.NewGuid()
              Path = this.Path
              Row = this.Row
              Latitude = this.Latitude
              Longitude = this.Longitude
              MinCloudCoverFilter = this.MinCloudCoverFilter
              MaxCloudCoverFilter = this.MaxCloudCoverFilter
              NotificationOffset = this.NotificationOffset }
            
    let private tranformPostResult next ctx (logger: ILogger) requestId result =
        match result with
        | Ok target ->
            let asApiResponseObj = { RequestGuid = requestId; ErrorMessage = None; Data = Some target }
            logger.LogInformation($"[{requestId}] Successfully created target \"{target.Id}\"")
            logger.LogDebug($"[{requestId}] {JsonSerializer.Serialize(target)}")
            (Successful.ok (json<ApiResponse<SimplifiedTarget>> asApiResponseObj)) next ctx
        | Error errorMsg -> 
            let asApiResponseObj = { RequestGuid = requestId; ErrorMessage = Some errorMsg; Data = None }
            logger.LogInformation($"[{requestId}] Failed to create a target: \"{errorMsg}\"")
            (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
    
    
    let handler (next: HttpFunc) (ctx: HttpContext) : HttpFuncResult =
        task {
            let requestId = getRequestId ctx
            
            let loggerFactory = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            let logger = loggerFactory.CreateLogger("FsLandsatApi.Handlers.UserTargetsHandler.UserTargetsPost")
            
            let dbUserService = ctx.RequestServices.GetRequiredService<DbUserService>()
            let dbUserTargetService = ctx.RequestServices.GetRequiredService<DbUserTargetService>()
            let jsonSerializerOptions = ctx.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions
            
            use requestBodyReader = new StreamReader(ctx.Request.Body)
            let! requestBody = requestBodyReader.ReadToEndAsync()
            let requestModel = JsonSerializer.Deserialize<CreateTargetRequest>(requestBody, jsonSerializerOptions)
            
            return! 
                tryGetUserEmail ctx
                |> Result.bind dbUserService.TryGetUserByEmail
                |> Result.map (fun user -> requestModel.CreateTargetDto(user.Id))
                |> TaskResult.ofResult
                |> TaskResult.bind dbUserTargetService.TryAddTarget
                |> TaskResult.map SimplifiedTarget.FromTarget
                |> Task.bind (tranformPostResult next ctx logger requestId)
        }
        
        
module UserTargetsPatch =
    
    // remarks:
    // The rest of the target information shouldn't be able to be edited. Generally, the below pertain to notification
    // or other information.
    [<CLIMutable>]
    type PatchTargetRequest =
        { MinCloudCoverFilter: Nullable<double>
          MaxCloudCoverFilter: Nullable<double>
          NotificationOffset: Nullable<TimeSpan> }
        
    let private transformTargetWithPatch (patchTargetsReq: PatchTargetRequest) (target: Target): Target =
        { target with
            MinCloudCoverFilter = if patchTargetsReq.MinCloudCoverFilter.HasValue then patchTargetsReq.MinCloudCoverFilter.Value else target.MinCloudCoverFilter
            MaxCloudCoverFilter = if patchTargetsReq.MaxCloudCoverFilter.HasValue then patchTargetsReq.MaxCloudCoverFilter.Value else target.MaxCloudCoverFilter
            NotificationOffset = if patchTargetsReq.NotificationOffset.HasValue then patchTargetsReq.NotificationOffset.Value else target.NotificationOffset }
        
    let private transformPostResult next ctx (logger: ILogger) requestId result =
        match result with
        | Ok target ->
            let asApiResponseObj = { RequestGuid = requestId; ErrorMessage = None; Data = Some target }
            logger.LogInformation($"[{requestId}] Successfully edited target")
            (Successful.ok (json<ApiResponse<SimplifiedTarget>> asApiResponseObj)) next ctx
        | Error errorMsg -> 
            let asApiResponseObj = { RequestGuid = requestId; ErrorMessage = Some errorMsg; Data = None }
            logger.LogWarning($"[{requestId}] Failed to create a target: \"{errorMsg}\"")
            (ServerErrors.internalError (json<ApiResponse<string>> asApiResponseObj)) next ctx
    
    let handler (next: HttpFunc) (ctx: HttpContext) =
        task {
            let loggerFactory = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            let logger = loggerFactory.CreateLogger("FsLandsatApi.Handlers.UserTargetsHandler.UserTargetsPatch")
            
            let dbUserTargetsService = ctx.RequestServices.GetRequiredService<DbUserTargetService>()
            let jsonSerializerOptions = ctx.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions
            
            let requestId: Guid = getRequestId ctx
                
            use requestBodyReader = new StreamReader(ctx.Request.Body)
            let! requestBody = requestBodyReader.ReadToEndAsync()
            let patchUserReq = JsonSerializer.Deserialize<PatchTargetRequest>(requestBody, jsonSerializerOptions)
            
            let transformTarget = transformTargetWithPatch patchUserReq
            let tryEditTarget targetId = dbUserTargetsService.TryEditTarget(transformTarget, targetId)
            
            return!
                // TODO: Email assertion goes here
                tryGetTargetId ctx
                |> TaskResult.ofResult
                |> TaskResult.bind tryEditTarget
                |> TaskResult.map SimplifiedTarget.FromTarget 
                |> Task.bind (transformPostResult next ctx logger requestId)
        }
        
        
module UserTargetsDelete =
    let private tryGetEmailAndTargetId (ctx: HttpContext) =
        result {
            let! email = tryGetUserEmail ctx
            let! targetId = tryGetTargetId ctx
            return email, targetId
        }
        
    let private transformDeleteResult next ctx (logger: ILogger) requestId result =
        match result with
        | Ok _ ->
            let msg = "Successfully deleted target"
            let asApiResponseObj = { RequestGuid = requestId; ErrorMessage = None; Data = Some msg }
            logger.LogInformation($"[{requestId}] {msg}")
            (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
        | Error errorMsg -> 
            let asApiResponseObj = { RequestGuid = requestId; ErrorMessage = Some errorMsg; Data = None }
            logger.LogInformation($"[{requestId}] Failed to create a target: \"{errorMsg}\"")
            (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
            
    
    let handler (next: HttpFunc) (ctx: HttpContext) =
        task {
            let loggerFactory = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
            let logger = loggerFactory.CreateLogger("FsLandsatApi.Handlers.UserTargetsHandler.UserTargetsDelete")
            let dbUserTargetService = ctx.RequestServices.GetRequiredService<DbUserTargetService>()
            
            let requestId = getRequestId ctx
            
            return! 
                tryGetEmailAndTargetId ctx
                |> TaskResult.ofResult
                |> TaskResult.bind dbUserTargetService.TryDeleteTarget
                |> Task.bind (transformDeleteResult next ctx logger requestId)
        }