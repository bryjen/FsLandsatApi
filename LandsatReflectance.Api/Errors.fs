module LandsatReflectance.Api.Errors

open System

let unwrapToNullable (exOpt: Exception option) : Exception | null =
    match exOpt with
    | None -> null
    | Some ex -> ex
    

type MissingEnvironmentVariableException(envVarName: string) =
    inherit System.Exception($"Missing the environment variable \"{envVarName}\".")
    
    member _.EnvVarName = envVarName
    
    override this.ToString() =
        $"Missing the environment variable \"{this.EnvVarName}\"."
        
        
type UsgsApiException(endpoint: string, errorCode: string, errorMessage: string, innerException: Exception option) =
    inherit System.Exception($"[{errorCode}] ({endpoint}) {errorMessage}.", unwrapToNullable innerException)
    
    member _.Endpoint = endpoint
    member _.ErrorCode = errorCode
    member _.ErrorMessage = errorMessage
    
    new(endpoint: string, errorCode: string, errorMessage: string) =
        UsgsApiException(endpoint, errorCode, errorMessage, None)
    
    override this.ToString() =
        $"[{errorCode}] ({endpoint}) {errorMessage}."
