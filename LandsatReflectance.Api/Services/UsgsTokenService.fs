module FsLandsatApi.Services.UsgsTokenService

open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.Threading.Tasks
open FsLandsatApi.Json.Usgs.LoginToken
open FsLandsatApi.Utils.UsgsHttpClient
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options

open FsLandsatApi.Options


type UsgsTokenService(
    logger: ILogger<UsgsTokenService>,
    usgsHttpClient: UsgsHttpClient,
    usgsOptions: IOptions<UsgsOptions>) =
    
    let mutable authToken: (string * DateTime) option = None
    
    member this.GetToken() =
        task {
            match authToken with
            | None ->
                return! this.GetTokenFromApi()
            | Some (_, exp) when DateTime.UtcNow > exp ->
                return! this.GetTokenFromApi()
            | Some (token, _) ->
                return (Ok token)
        }
        
    member private this.GetTokenFromApi() : Task<Result<string, Exception>> =
        task {
            let getDateTimestamp = fun (dateTime: DateTime) -> dateTime.ToString("s")
            logger.LogInformation($"[{getDateTimestamp(DateTime.Now)}] Fetching new access token.")
                
            let requestBody = Map.ofArray [| ("username", usgsOptions.Value.Username); ("token", usgsOptions.Value.AppToken) |]
                              |> JsonSerializer.Serialize
                              
            use requestContent = new StringContent(requestBody, Encoding.UTF8, "application/json")
            
            
            let! response = usgsHttpClient.HttpClient.PostAsync("login-token", requestContent)
            use streamReader = new StreamReader(response.Content.ReadAsStream())
            let responseContent = streamReader.ReadToEnd()
            
            let tokenResult = tryParseLoginTokenResponse responseContent
            
            // Produce 'side-effects'
            match tokenResult with
            | Ok token ->
                logger.LogInformation($"[{getDateTimestamp(DateTime.Now)}] Successfully fetched new access token \"{token}\". Expected expiration at: \"{getDateTimestamp(DateTime.Now.AddHours(2).AddMinutes(-5))}\"")
                authToken <- Some (token, DateTime.UtcNow.AddHours(2).AddMinutes(-5))
            | Error _ ->
                // Do nothing here, expect caller to handle the error
                () 
            
            return tokenResult
        }