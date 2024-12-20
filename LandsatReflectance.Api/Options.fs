module FsLandsatApi.Options

open System

open Errors


let private tryGetEnvironmentVariable (envVarName: string): string option =
    let value: string | null = Environment.GetEnvironmentVariable(envVarName)
    match value with
    | null -> None
    | s -> Some s
    
    
/// The number of characters allocated to printing the name of an environment variable during printing.
/// Includes the semicolon ':'
let private envVarNameCharAlloc = 35


type UsgsOptions =
    { Username: string
      AppToken: string }
with
    static member private EnvVarNames = [| "LANDSAT_API_USGS_USERNAME"; "LANDSAT_API_USGS_APP_TOKEN" |]
    
    static member CreateUsgsOptions() = 
        let envVarValueOptions = UsgsOptions.EnvVarNames |> Array.map tryGetEnvironmentVariable
        let asTuples = Array.zip UsgsOptions.EnvVarNames envVarValueOptions
        
        match Array.exists Option.isNone envVarValueOptions with
        | true -> asTuples
                  |> Array.filter (fun tuple -> Option.isNone (snd tuple))
                  |> Array.map fst
                  |> Array.map MissingEnvironmentVariableException
                  |> Error
        | false ->
            { Username = envVarValueOptions[0].Value
              AppToken = envVarValueOptions[1].Value }
            |> Ok
            
    member this.Print(printFn: string -> unit) =
        let spaces count = String.replicate count " "
        printFn $"{UsgsOptions.EnvVarNames[0]}:{spaces (envVarNameCharAlloc - UsgsOptions.EnvVarNames[0].Length)}{this.Username}"
        printFn $"{UsgsOptions.EnvVarNames[1]}:{spaces (envVarNameCharAlloc - UsgsOptions.EnvVarNames[1].Length)}{this.AppToken}"


type DbOptions =
    { DbConnectionString: string }
with
    static member private EnvVarNames = [| "LANDSAT_API_DB_CONNECTION_STRING" |]
    
    static member CreateDbOptions() = 
        let envVarValueOptions = DbOptions.EnvVarNames |> Array.map tryGetEnvironmentVariable
        let asTuples = Array.zip DbOptions.EnvVarNames envVarValueOptions
        
        match Array.exists Option.isNone envVarValueOptions with
        | true -> asTuples
                  |> Array.filter (fun tuple -> Option.isNone (snd tuple))
                  |> Array.map fst
                  |> Array.map MissingEnvironmentVariableException
                  |> Error
        | false ->
            { DbConnectionString = envVarValueOptions[0].Value }
            |> Ok
            
    member this.Print(printFn: string -> unit) =
        let spaces count = String.replicate count " "
        printFn $"{DbOptions.EnvVarNames[0]}:{spaces (envVarNameCharAlloc - DbOptions.EnvVarNames[0].Length)}{this.DbConnectionString}"


type AuthTokenOptions =
    { SigningKey: string }
with
    static member private EnvVarNames = [| "LANDSAT_API_SIGNING_KEY" |]
    
    static member CreateTokenOptions() = 
        let envVarValueOptions = AuthTokenOptions.EnvVarNames |> Array.map tryGetEnvironmentVariable
        let asTuples = Array.zip AuthTokenOptions.EnvVarNames envVarValueOptions
        
        match Array.exists Option.isNone envVarValueOptions with
        | true -> asTuples
                  |> Array.filter (fun tuple -> Option.isNone (snd tuple))
                  |> Array.map fst
                  |> Array.map MissingEnvironmentVariableException
                  |> Error
        | false ->
            { SigningKey = envVarValueOptions[0].Value }
            |> Ok
            
    member this.Print(printFn: string -> unit) =
        let spaces count = String.replicate count " "
        printFn $"{AuthTokenOptions.EnvVarNames[0]}:{spaces (envVarNameCharAlloc - AuthTokenOptions.EnvVarNames[0].Length)}{this.SigningKey}"
