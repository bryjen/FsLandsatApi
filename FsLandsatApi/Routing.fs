﻿module FsLandsatApi.Routing

open FsLandsatApi.Handlers.SceneHandler
open FsLandsatApi.Handlers.UserHandler
open FsLandsatApi.Handlers.UserTargetsHandler
open FsLandsatApi.Handlers.UserTargetsHandler.UserTargetsPost
open FsLandsatApi.Middleware.RequestIdMiddleware
open FsLandsatApi.Models.ApiResponse
open FsLandsatApi.Models.User
open FsLandsatApi.Models.Usgs.Scene
open Giraffe.EndpointRouting
open Giraffe
open Giraffe.OpenApi
open Giraffe.ViewEngine
open Microsoft.OpenApi.Models
open Microsoft.AspNetCore.Builder



let intSchema: OpenApiSchema = OpenApiSchema(Type = "integer", Format = "int32")

[<RequireQualifiedAccess>]
module private EndpointOpenApiConfigs =
    let sceneEndpointConfig = 
        OpenApiConfig(
            responseBodies = [| ResponseBody(typeof<ApiResponse<SimplifiedSceneData array>>) |],
            configureOperation = (fun o ->
                o.Tags.Clear()
                let tag = OpenApiTag()
                tag.Name <- "scene"
                o.Tags.Add(tag)
                
                o.OperationId <- "GET_scene"
                o.Summary <- "Fetches scene information"
                o.Description <-"Fetches landsat scene information from the **\"landsat_ot_c2_l2\"** database related to a specific path and row."
                
                
                let securityRequirement = OpenApiSecurityRequirement()
                securityRequirement.Add(
                    OpenApiSecurityScheme(
                        Reference = OpenApiReference(
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        )
                    ),
                    [|  |] 
                )
                o.Security <- [| securityRequirement |]
                
                
                let pathParam = OpenApiParameter()
                pathParam.Name <- "path"
                pathParam.In <- ParameterLocation.Query
                pathParam.Required <- true
                pathParam.Schema <- intSchema
                o.Parameters.Add(pathParam)
                
                let rowParam = OpenApiParameter()
                rowParam.Name <- "row"
                rowParam.In <- ParameterLocation.Query
                rowParam.Required <- true
                rowParam.Schema <- intSchema
                o.Parameters.Add(rowParam)
                
                let resultsParam = OpenApiParameter()
                resultsParam.Name <- "results"
                resultsParam.In <- ParameterLocation.Query
                resultsParam.Schema <- intSchema
                o.Parameters.Add(resultsParam)
                
                
                o))
        
    let POST_userEndpointConfig = 
        OpenApiConfig(
            responseBodies = [| ResponseBody(typeof<ApiResponse<string>>) |],
            requestBody = RequestBody(typeof<UserLoginPost.LoginUserRequest>),
            configureOperation = (fun o ->
                o.Tags.Clear()
                let tag = OpenApiTag()
                tag.Name <- "user"
                o.Tags.Add(tag)
                
                o.OperationId <- "POST_user"
                o.Summary <- "Attempts to login a user, given credentials"
                o))
        
    let POST_createUserEndpointConfig = 
        OpenApiConfig(
            responseBodies = [| ResponseBody(typeof<ApiResponse<string>>) |],
            requestBody = RequestBody(typeof<UserCreatePost.CreateUserRequest>),
            configureOperation = (fun o ->
                o.Tags.Clear()
                let tag = OpenApiTag()
                tag.Name <- "user"
                o.Tags.Add(tag)
                
                o.OperationId <- "POST_createUser"
                o.Summary <- "Attempts to create a user"
                o))
        
        
    let GET_userTargetsEndpointConfig = 
        OpenApiConfig(
            responseBodies = [| ResponseBody(typeof<ApiResponse<SimplifiedTarget list>>) |],
            configureOperation = (fun o ->
                o.Tags.Clear()
                let tag = OpenApiTag()
                tag.Name <- "targets"
                o.Tags.Add(tag)
                
                let securityRequirement = OpenApiSecurityRequirement()
                securityRequirement.Add(
                    OpenApiSecurityScheme(
                        Reference = OpenApiReference(
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        )
                    ),
                    [|  |] 
                )
                o.Security <- [| securityRequirement |]
                
                o.OperationId <- "GET_targets"
                o.Summary <- "Gets all emails bound to a specified user"
                o))
    
    let POST_userTargetsEndpointConfig = 
        OpenApiConfig(
            responseBodies = [| ResponseBody(typeof<ApiResponse<SimplifiedTarget>>) |],
            requestBody = RequestBody(typeof<CreateTargetRequest>),
            configureOperation = (fun o ->
                o.Tags.Clear()
                let tag = OpenApiTag()
                tag.Name <- "targets"
                o.Tags.Add(tag)
                
                let securityRequirement = OpenApiSecurityRequirement()
                securityRequirement.Add(
                    OpenApiSecurityScheme(
                        Reference = OpenApiReference(
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        )
                    ),
                    [|  |] 
                )
                o.Security <- [| securityRequirement |]
                
                o.OperationId <- "POST_targets"
                o.Summary <- "Attempts to create a target"
                o))
        
let notLoggedIn =
    RequestErrors.UNAUTHORIZED
        "Bearer"
        "Some Realm"
        "You must be logged in."

let mustBeLoggedIn: HttpHandler = requiresAuthentication notLoggedIn
    
    
let endpoints: Routers.Endpoint list = [
    Routers.GET [
        Routers.route "/scene" (GET >=> mustBeLoggedIn >=> requestIdMiddleware >=> sceneHandler)
        |> addOpenApi EndpointOpenApiConfigs.sceneEndpointConfig
        
        Routers.route "/user/targets" (GET >=> mustBeLoggedIn >=> requestIdMiddleware >=> UserTargetsGet.handler)
        |> addOpenApi EndpointOpenApiConfigs.GET_userTargetsEndpointConfig
    ]
    Routers.POST [
        Routers.route "/user" (POST >=> requestIdMiddleware >=> UserLoginPost.handler)
        |> addOpenApi EndpointOpenApiConfigs.POST_userEndpointConfig
        
        Routers.route "/user/create" (POST >=> requestIdMiddleware >=> UserCreatePost.handler)
        |> addOpenApi EndpointOpenApiConfigs.POST_createUserEndpointConfig
        
        Routers.route "/user/targets" (POST >=> mustBeLoggedIn >=> requestIdMiddleware >=> UserTargetsPost.handler)
        |> addOpenApi EndpointOpenApiConfigs.POST_userTargetsEndpointConfig
    ]
    Routers.PATCH [
    ]
    Routers.DELETE [
    ]
]
