module FsLandsatApi.Json.Usgs.SceneSearch

open System
open System.Text.Json
open System.Text.Json.Nodes
open FsLandsatApi.Errors
open FsLandsatApi.Models.Usgs.Scene
open FsToolkit.ErrorHandling


/// Creates a request to the endpoint 'scene-search' for some scene data.
let createSceneSearchRequest (path: int) (row: int) (results: int) : string =
    let pathMetadataValueFilterJsonObj = JsonObject()
    pathMetadataValueFilterJsonObj["filterType"] <- JsonValue.Create("value")
    pathMetadataValueFilterJsonObj["filterId"] <- JsonValue.Create("5e83d14fb9436d88")
    pathMetadataValueFilterJsonObj["value"] <- JsonValue.Create(path.ToString())
    pathMetadataValueFilterJsonObj["operand"] <- JsonValue.Create("=")
    
    let rowMetadataValueFilterJsonObj = JsonObject()
    rowMetadataValueFilterJsonObj["filterType"] <- JsonValue.Create("value")
    rowMetadataValueFilterJsonObj["filterId"] <- JsonValue.Create("5e83d14ff1eda1b8")
    rowMetadataValueFilterJsonObj["value"] <- JsonValue.Create(row.ToString())
    rowMetadataValueFilterJsonObj["operand"] <- JsonValue.Create("=")
    
    let metadataFilterJsonObj = JsonObject()
    metadataFilterJsonObj["filterType"] <- JsonValue.Create("and")
    metadataFilterJsonObj["childFilters"] <- JsonValue.Create([| pathMetadataValueFilterJsonObj; rowMetadataValueFilterJsonObj |])
    
    
    let sceneFilterJsonObj = JsonObject()
    sceneFilterJsonObj["metadataFilter"] <- metadataFilterJsonObj
    
    
    let requestJsonObj = JsonObject()
    requestJsonObj["datasetName"] <- JsonValue.Create("landsat_ot_c2_l2")
    requestJsonObj["maxResults"] <- JsonValue.Create(results.ToString())
    requestJsonObj["useCustomization"] <- JsonValue.Create(false)
    requestJsonObj["sceneFilter"] <- sceneFilterJsonObj

    requestJsonObj.ToString()


/// Attempts to parse the response from a request to the 'scene-search' endpoint.
let rec tryParseSceneSearchResponse (jsonResponse: string) : Result<SceneData array, Exception> =
    result {
        let! dataJsonString = tryParseOuterJson jsonResponse
        return! tryParseDataJson dataJsonString
    }
    
and private tryParseOuterJson (jsonResponse: string) : Result<string, Exception> =
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
            Ok (root.GetProperty("data").ToString())
    with
    | ex ->
        Error (Exception("There was an error parsing the response from 'login-token'", ex))
        
and private tryParseDataJson (dataJsonString: string) : Result<SceneData array, Exception> =
    try
        use jsonDoc = JsonDocument.Parse(dataJsonString)
        let dataJson = jsonDoc.RootElement
        
        let results = dataJson.GetProperty("results")
        let arrayEnumRef = ref (results.EnumerateArray())
        tryParseArray arrayEnumRef []
    with
    | ex ->
        Error (Exception("There was an error parsing the data from the response @ 'login-token'", ex))
        
and private tryParseArray (arrayEnumerator: JsonElement.ArrayEnumerator ref) (acc: SceneData list) : Result<SceneData array, Exception> =
    match arrayEnumerator.contents.MoveNext() with
    | true ->
        match tryParseToSceneData arrayEnumerator.contents.Current with
        | Ok sceneData -> tryParseArray arrayEnumerator (sceneData :: acc)
        | Error error -> Error error 
    | false ->
        Ok (List.toArray acc)
    
and private tryParseToSceneData (jsonElement: JsonElement) : Result<SceneData, Exception> =
    try
        let jsonSerializerOptions = JsonSerializerOptions()
        jsonSerializerOptions.WriteIndented <- true
        jsonSerializerOptions.PropertyNameCaseInsensitive <- true
        
        let browseJsonElement = jsonElement.GetProperty("browse")
        let browseInfos = JsonSerializer.Deserialize<BrowseInfo array>(browseJsonElement, jsonSerializerOptions)
        
        let metadataJsonElement = jsonElement.GetProperty("metadata")
        let metadata = JsonSerializer.Deserialize<Metadata array>(metadataJsonElement, jsonSerializerOptions)
        
        let entityId = jsonElement.GetProperty("entityId").GetString()
        let displayId = jsonElement.GetProperty("displayId").GetString()
        let publishDate = DateTimeOffset.Parse(jsonElement.GetProperty("publishDate").GetString())
        
        { BrowseInfos = browseInfos
          EntityId = entityId
          DisplayId = displayId
          Metadata = metadata
          PublishDate = publishDate }
        |> Ok
    with
    | ex ->
        Error (UsgsApiException("get-token", "<no error code>", "Could not parse a 'result' JsonObject into a 'SceneData' type.", Some ex)) 