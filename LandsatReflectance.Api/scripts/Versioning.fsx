
open System

type Version =
    { Major: int
      Minor: int
      Patch: int }
with
    static member Default =
        { Major = 0
          Minor = 0
          Patch = 0 }
        
    member this.IncrementMajor() =
        { this with Major = this.Major + 1 }
        
    member this.IncrementMinor() =
        { this with Minor = this.Minor + 1 }
        
    member this.IncrementPatch() =
        { this with Patch = this.Patch + 1 }
        
        
    override this.ToString() =
        $"{this.Major}.{this.Minor}.{this.Patch}"
        
    
let private tryParseVersionString (versionString: string) =
    let tokens = versionString.Split('.')
    match tokens.Length with
    | len when len = 3 ->
        
        let major () = 
            match Int32.TryParse tokens[0] with
            | true, major -> Ok major
            | false, _ -> Error $"Could not parse \"{tokens[0]}\" as \"Major\""
            
        let minor () = 
            match Int32.TryParse tokens[1] with
            | true, minor -> Ok minor
            | false, _ -> Error $"Could not parse \"{tokens[1]}\" as \"Minor\""
            
        let patch () = 
            match Int32.TryParse tokens[2] with
            | true, minor -> Ok minor
            | false, _ -> Error $"Could not parse \"{tokens[2]}\" as \"Patch\""
            
        major ()
        |> Result.bind (fun major ->
            minor ()
            |> Result.bind (fun minor ->
                patch ()
                |> Result.map (fun patch ->
                    { Major = major
                      Minor = minor
                      Patch = patch }
                )
            )
        )        
    | _ ->
        Error $"Splitting the string resulted in {tokens.Length} tokens (expected 3)"
    
    
let private envVarName = "LANDSAT_API_IMAGE_VERSION"
    
    
let getImageVersion (logInfo: string -> unit) (logWarning: string -> unit) =
    let imageVerString: string = Environment.GetEnvironmentVariable(envVarName)
    match imageVerString with
    | null ->
        logWarning $"Could not find version information from the environment variable \"{envVarName}\""
        Version.Default
    | s ->
        match tryParseVersionString s with
        | Ok version ->
            logInfo $"Successfully parsed version: {version}"
            version
        | Error errorMsg ->
            logWarning $"Could not parse the version string \"{s}\", with error \"{errorMsg}\". Returned default."
            Version.Default
            
let setImageVersion (version: Version) =
    Environment.SetEnvironmentVariable(envVarName, version.ToString())
            
(*
let logInfo msg = printfn $"[INFO] {msg}"
let logWarning msg = printfn $"[WARNING] {msg}"
getImageVersion logInfo logWarning
*)