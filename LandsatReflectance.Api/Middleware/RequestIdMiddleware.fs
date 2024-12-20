module FsLandsatApi.Middleware.RequestIdMiddleware

open System
open Giraffe
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging

let requestIdMiddleware (next: HttpFunc) (ctx: HttpContext) =
    
    let loggerFactory = ctx.RequestServices.GetRequiredService<ILoggerFactory>()
    let logger = loggerFactory.CreateLogger("FsLandsatApi.Middleware.RequestIdMiddleware")
    
    let requestId = Guid.NewGuid()
    ctx.Items["requestId"] <- requestId
    
    logger.LogInformation($"[{requestId}] Tagged inbound request to \"{ctx.GetRequestUrl()}\" with a request id.")
    
    next ctx