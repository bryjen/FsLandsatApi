open System
open System.Text
open System.Net.Http
open System.Net.Http.Headers
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Json
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

open Giraffe
open Giraffe.EndpointRouting

open Microsoft.OpenApi.Models
open Microsoft.Extensions.Options
open Microsoft.IdentityModel.Tokens

open LandsatReflectance.Api.Extensions
open LandsatReflectance.Api.Handlers.NotFoundHandler
open LandsatReflectance.Api.Options
open LandsatReflectance.Api.Services.DbUserService
open LandsatReflectance.Api.Services.DbUserTargetService
open LandsatReflectance.Api.Services.UsgsSceneService
open LandsatReflectance.Api.Services.UsgsTokenService
open LandsatReflectance.Api.Utils.AppJsonSerializer
open LandsatReflectance.Api.Utils.UsgsHttpClient


    
let configureAppOptions (services: IServiceCollection) =
    let provider = services.BuildServiceProvider()
    let logger = provider.GetService<ILoggerFactory>().CreateLogger("ServiceConfiguration")
    
    // Configure option types
    let anyOptionInvalid = ref false
    services.TryAddUsgsOptions(logger, anyOptionInvalid) |> ignore
    services.TryAddDbOptions(logger, anyOptionInvalid) |> ignore
    services.TryAddTokenOptions(logger, anyOptionInvalid) |> ignore
    
    if anyOptionInvalid.Value then
        failwith "Startup configuration failed. Could not initialize some options"
        
    services
    
    
let configureAuth (services: IServiceCollection) =
    services.AddAuthentication(fun options ->
                options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(fun options ->
                // Configure 'TokenValidationParameters'
                let authTokenOptions =
                    match AuthTokenOptions.CreateTokenOptions() with
                    | Ok value -> value
                    | Error _ -> failwith "Could not get the authentication token options while configuring authentication, \"AuthTokenOptions\" could not be initialized."
                let signingKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(authTokenOptions.SigningKey));
                
                let tokenValidationParams = TokenValidationParameters()
                tokenValidationParams.ValidateIssuer <- true
                tokenValidationParams.ValidateAudience <- true
                tokenValidationParams.ValidateLifetime <- true
                tokenValidationParams.ValidateIssuerSigningKey <- true
                
                tokenValidationParams.ValidIssuer <- "FlatEarthers"
                tokenValidationParams.ValidAudience <- "FlatEarthers"
                tokenValidationParams.ClockSkew <- TimeSpan.FromMinutes(5.0)
                tokenValidationParams.IssuerSigningKey <- signingKey
                
                options.TokenValidationParameters <- tokenValidationParams
                
                (*
                let jwtBearerEvents = JwtBearerEvents()
                jwtBearerEvents.OnMessageReceived <- fun msgReceivedCtx ->
                    match msgReceivedCtx.Request.Headers.TryGetValue("X-Auth-Token") with
                    | true, value -> msgReceivedCtx.Token <- value
                    | false, _ -> ()
                    Task.CompletedTask
                
                options.Events <- jwtBearerEvents
                *)
                ())
    |> ignore
    
    services.AddAuthorization() |> ignore
    
    services
    
    
let configureServices (services: IServiceCollection) =
    services.ConfigureHttpJsonOptions(fun jsonOptions ->
        jsonOptions.SerializerOptions.PropertyNameCaseInsensitive <- true
        jsonOptions.SerializerOptions.WriteIndented <- true) |> ignore
        
    // Configure http clients
    services.AddHttpClient<UsgsHttpClient>(fun httpClient ->
        httpClient.BaseAddress <- Uri("https://m2m.cr.usgs.gov/api/api/json/stable/")
        httpClient.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json")))
    |> ignore
    
    services.AddSingleton<UsgsTokenService>() |> ignore
    services.AddTransient<UsgsSceneService>() |> ignore
    services.AddScoped<DbUserService>() |> ignore
    services.AddScoped<DbUserTargetService>() |> ignore
        
    services.AddGiraffe() |> ignore
    services.AddSingleton<Json.ISerializer>(fun serviceProvider ->
        let jsonSerializerOptions = serviceProvider.GetRequiredService<IOptions<JsonOptions>>()
        AppJsonSerializer(jsonSerializerOptions.Value.SerializerOptions) :> Json.ISerializer) |> ignore
    
    services.AddRouting() |> ignore
    services.AddEndpointsApiExplorer() |> ignore
    services
    
    
let configureOpenApi (services: IServiceCollection) =
    services.AddSwaggerGen(fun config ->
        let openApiInfo = OpenApiInfo()
        openApiInfo.Title <- "FsLandsaApi"
        openApiInfo.Version <- "0.1.0"
        openApiInfo.Description <- "PLACEHOLDER"
        config.SwaggerDoc("v1", openApiInfo)
        
        // Auth config
        let securityScheme = OpenApiSecurityScheme()
        securityScheme.Name <- "Authorization"
        securityScheme.Type <- SecuritySchemeType.Http
        securityScheme.Scheme <- "bearer"
        securityScheme.BearerFormat <- "JWT"
        securityScheme.In <- ParameterLocation.Header
        securityScheme.Description <- "Enter the JWT token obtained from logging in."
        config.AddSecurityDefinition("Bearer", securityScheme)
        ())
    |> ignore
    services


let configureApp (app: IApplicationBuilder) =
    app.UseRouting()
       .UseSwagger()
       .UseSwaggerUI()
       
       .UseAuthentication()
       .UseAuthorization()
       
       .UseGiraffe(LandsatReflectance.Api.Routing.endpoints)
       .UseGiraffe(notFoundHandler)

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    builder.Services
    |> configureAppOptions
    |> configureServices
    |> configureAuth
    |> configureOpenApi
    |> ignore
    
    let app = builder.Build()
    
    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    configureApp app
    app.Run()
    
    0 // Exit code
