module FsLandsatApi.Models.Usgs.Scene

open System


/// Represents some scene data returned by the 'scene-search' endpoint. Simplified, and does not contain all returned information.
type SceneData =
    { BrowseInfos: BrowseInfo array
      EntityId: string
      DisplayId: string
      Metadata: Metadata array
      PublishDate: DateTimeOffset }
    
and BrowseInfo =
    { BrowseName: string
      BrowsePath: string
      OverlayPath: string
      ThumbnailPath: string }
    
and Metadata =
    { Id: string
      FieldName: string
      DictionaryLink: string
      Value: Object }
    

[<CLIMutable>]
type SimplifiedSceneData =
    { BrowseName: string option
      BrowsePath: string option
      OverlayPath: string option
      ThumbnailPath: string option
      
      EntityId: string
      DisplayId: string
      PublishDate: DateTimeOffset }
