module FsLandsatApi.Utils.UsgsHttpClient

open System.Net.Http

// We have to use a typed client, because named clients don't get configured properly - for some reason.

type UsgsHttpClient(httpClient: HttpClient) =
    member _.HttpClient = httpClient