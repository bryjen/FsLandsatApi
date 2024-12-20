module FsLandsatApi.Utils.AppJsonSerializer

open System.Text.Json
open Giraffe

// Need to create custom type that wraps around 'System.Text.JsonSerializer' cause the program just doesn't work for
// some reason.
type AppJsonSerializer(jsonSerializerOptions: JsonSerializerOptions) =
    
    member _.JsonSerializerOptions = jsonSerializerOptions
    
    interface Json.ISerializer with
        member this.SerializeToString<'T>(x: 'T) =
            JsonSerializer.Serialize<'T>(x, jsonSerializerOptions)
            
        member this.SerializeToBytes<'T>(x: 'T) =
            JsonSerializer.SerializeToUtf8Bytes<'T>(x, jsonSerializerOptions)
            
        member this.SerializeToStreamAsync<'T> (x: 'T) stream =
            JsonSerializer.SerializeAsync(stream, x, jsonSerializerOptions)
        
        member this.Deserialize<'T>(bytes: byte array): 'T =
            JsonSerializer.Deserialize<'T>(bytes, jsonSerializerOptions)
            
        member this.Deserialize<'T>(json: string): 'T =
            JsonSerializer.Deserialize<'T>(json, jsonSerializerOptions)
            
        member this.DeserializeAsync(stream) =
            JsonSerializer.DeserializeAsync(stream, jsonSerializerOptions).AsTask()
