module LandsatReflectance.Api.Extensions

open Microsoft.Extensions.Options
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open LandsatReflectance.Api.Options


        
type IServiceCollection with
    member services.TryAddUsgsOptions(logger: ILogger, anyOptionInvalid: bool ref) = 
        match UsgsOptions.CreateUsgsOptions() with
        | Ok usgsOptions ->
            usgsOptions.Print(logger.LogInformation)
            services.AddSingleton<IOptions<UsgsOptions>>(Options.Create(usgsOptions)) |> ignore
        | Error errorList ->
            anyOptionInvalid.contents <- true
            Array.iter (fun error -> logger.LogError(error.ToString())) errorList
        services
        
    member services.TryAddDbOptions(logger: ILogger, anyOptionInvalid: bool ref) = 
        match DbOptions.CreateDbOptions() with
        | Ok dbOptions ->
            dbOptions.Print(logger.LogInformation)
            services.AddSingleton<IOptions<DbOptions>>(Options.Create(dbOptions)) |> ignore
        | Error errorList ->
            anyOptionInvalid.contents <- true
            Array.iter (fun error -> logger.LogError(error.ToString())) errorList
        services
        
    member services.TryAddTokenOptions(logger: ILogger, anyOptionInvalid: bool ref) = 
        match AuthTokenOptions.CreateTokenOptions() with
        | Ok tokenOptions ->
            tokenOptions.Print(logger.LogInformation)
            services.AddSingleton<IOptions<AuthTokenOptions>>(Options.Create(tokenOptions)) |> ignore
        | Error errorList ->
            anyOptionInvalid.contents <- true
            Array.iter (fun error -> logger.LogError(error.ToString())) errorList
        services
        