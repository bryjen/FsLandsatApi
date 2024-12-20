module FsLandsatApi.Models.ApiResponse

open System

[<CLIMutable>]
type ApiResponse<'a> =
    { RequestGuid: Guid
      ErrorMessage: string option
      Data: 'a option }