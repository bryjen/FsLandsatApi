module LandsatReflectance.Api.Handlers.NotFoundHandler

open System

open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open Giraffe

open LandsatReflectance.Api.Models.ApiResponse
open LandsatReflectance.Api.Middleware.RequestIdMiddleware



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

