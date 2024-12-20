module FsLandsatApi.Handlers.NotFoundHandler

open System

open FsLandsatApi.Middleware.RequestIdMiddleware
open FsLandsatApi.Models
open Giraffe.ComputationExpressions
open Microsoft.AspNetCore.Http

open Giraffe

open FsLandsatApi.Models.ApiResponse
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging


let notFoundHandler: HttpHandler =
    requestIdMiddleware
    >=>
    fun (next: HttpFunc) (ctx: HttpContext) ->
        let loggerFactory = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
        let logger = loggerFactory.CreateLogger("FsLandsatApi.Handlers.SceneHandler")
        
        let requestId: Guid =
            match ctx.Items.TryGetValue("requestId") with
            | true, value -> value :?> Guid
            | false, _ -> Guid.Empty
            
        let response =
            { RequestGuid = requestId
              ErrorMessage = Some "Not found"
              Data = None }
            
        logger.LogInformation($"[{requestId}] Request to invalid endpoint.")
        RequestErrors.notFound (json<ApiResponse<obj>> response) next ctx

