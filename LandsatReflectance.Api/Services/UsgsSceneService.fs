module FsLandsatApi.Services.UsgsSceneService

open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open FsLandsatApi.Json.Usgs.SceneSearch
open FsLandsatApi.Models.Usgs.Scene
open FsLandsatApi.Utils.UsgsHttpClient
open Microsoft.Extensions.Logging
open FsLandsatApi.Services.UsgsTokenService
open Microsoft.FSharp.Control

let simplifySceneData (sceneData: SceneData) =
    match Array.tryHead sceneData.BrowseInfos with
    | None ->
          { BrowseName = None
            BrowsePath = None
            OverlayPath = None
            ThumbnailPath = None
            
            EntityId = sceneData.EntityId
            DisplayId = sceneData.DisplayId
            PublishDate = sceneData.PublishDate }
    | Some value -> 
          { BrowseName = Some value.BrowseName
            BrowsePath = Some value.BrowsePath
            OverlayPath = Some value.OverlayPath
            ThumbnailPath = Some value.ThumbnailPath
            
            EntityId = sceneData.EntityId
            DisplayId = sceneData.DisplayId
            PublishDate = sceneData.PublishDate }


type UsgsSceneService(
    usgsHttpClient: UsgsHttpClient,
    usgsTokenService: UsgsTokenService) =
    
    member this.GetScenes(path: int, row: int, results: int) =
        task {
            let sceneSearchRequest = createSceneSearchRequest path row results
            use requestContent = new StringContent(sceneSearchRequest, Encoding.UTF8, "application/json")
            
            let! authTokenResult = usgsTokenService.GetToken()
            return 
                match authTokenResult with
                | Ok authToken ->
                    usgsHttpClient.HttpClient.DefaultRequestHeaders.Add("X-Auth-Token", authToken)
                    
                    let responseTask = usgsHttpClient.HttpClient.PostAsync("scene-search", requestContent)
                    responseTask.Wait()
                    let response = responseTask.Result
                    
                    use streamReader = new StreamReader(response.Content.ReadAsStream())
                    let responseContent = streamReader.ReadToEnd()
                    
                    tryParseSceneSearchResponse responseContent
                    |> Result.map (fun sceneDataArray -> Array.map simplifySceneData sceneDataArray)
                | Error errorValue ->
                    Error errorValue
        }
        
