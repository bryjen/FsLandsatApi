module LandsatReflectance.Api.Json.Usgs.LoginToken

open System
open System.Text.Json

open LandsatReflectance.Api.Errors



let tryParseLoginTokenResponse (jsonResponse: string) : Result<string, Exception> =
    try
        use jsonDoc = JsonDocument.Parse(jsonResponse)
        let root = jsonDoc.RootElement
        
        let errorCodeProperty = root.GetProperty("errorCode")
        let errorMessageProperty = root.GetProperty("errorMessage")
        match errorCodeProperty.ValueKind, errorMessageProperty.ValueKind with
        | JsonValueKind.String, JsonValueKind.String ->
            Error (UsgsApiException("login-token", errorCodeProperty.GetString(), errorMessageProperty.GetString()))
        | JsonValueKind.String, _ ->
            Error (UsgsApiException("login-token", errorCodeProperty.GetString(), "<no error message>"))
        | _, JsonValueKind.String ->
            Error (UsgsApiException("login-token", "<no error code>", errorMessageProperty.GetString()))
        | _, _ ->
            Ok (root.GetProperty("data").GetString())
    with
    | ex ->
        Error (Exception("There was an error parsing the response from 'login-token'", ex))
    