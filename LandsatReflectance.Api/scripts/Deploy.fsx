#r "paket:
nuget Fake.IO.FileSystem
nuget Fake.Core.Target //"

#load "./.fake/deploy.fsx/intellisense.fsx"
#load "Versioning.fsx"

open System
open System.Globalization
open System.IO
open System.Net.Http
open Fake.Core
open Fake.Core.TargetOperators
open Versioning



[<AutoOpen>]
module private Logging =
    let keyNameCharAlloc = 20
    
    let spaces count = String.replicate count " "

    let formatNameAndValue (varName: string) (value: string) =
        $"{varName}:{spaces (keyNameCharAlloc - varName.Length)}{value}"
        
    
    let prependTimestamp msg =
        let timestamp = DateTime.Now.ToString("T", CultureInfo.InvariantCulture)
        $"[{timestamp}] {msg}"
        
    let log = Trace.log
        
    let logWithLevel logLevel msg =
        prependTimestamp msg
        |> (fun fmtMsg -> Trace.logToConsole(fmtMsg, logLevel))
        
    let logInfo: string -> unit = logWithLevel Trace.Information
    
    let logWarning: string -> unit = logWithLevel Trace.Warning



let buildDockerImage = "Build Docker Image"
let publishDockerImage = "Publish Docker Image"
let deployImage = "Deploy Published Image"

let scriptDir = Directory.GetCurrentDirectory().Replace("/", "\\")
let solutionDir = DirectoryInfo(scriptDir).Parent.Parent.FullName
let projectDir = Path.Join(solutionDir, "FsLandsatApi")
let workingDir = Path.Join(solutionDir, "build-artifacts")

let imageVersion = (getImageVersion logInfo logWarning |> _.IncrementPatch) ()
let imageTag = "chronoalpha/fs_landsat_api"



Target.create buildDockerImage (fun _ ->
    let imageVersionStr = imageVersion.ToString()
    
    log (formatNameAndValue "scriptDir" scriptDir)
    log (formatNameAndValue "solutionDir" solutionDir)
    log (formatNameAndValue "projectDir" projectDir)
    log (formatNameAndValue "workingDir" workingDir)
    log (formatNameAndValue "imageVersionStr" imageVersionStr)
    
    log (formatNameAndValue "imageTag" imageTag)
    
    if not (Directory.Exists(workingDir)) then
        log $"Creating working directory `{workingDir}`"
        Directory.CreateDirectory(workingDir) |> ignore
        
    // docker build
    let dockerBuildArgs =
        Arguments.Empty
        |> Arguments.append ["build"]
        |> Arguments.append ["-f"; Path.Join(projectDir, "Dockerfile")]
        |> Arguments.append ["-t"; imageTag]
        |> Arguments.append ["--no-cache"]
        |> Arguments.append [projectDir]
    
    Command.RawCommand("docker", dockerBuildArgs)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> Proc.run
    |> ignore
    
    logInfo "Successfully built docker image"
    
    
    // docker save
    let dockerBuildArgs =
        Arguments.Empty
        |> Arguments.append ["save"; imageTag]
        |> Arguments.append ["-o"; Path.Join(workingDir, $"fs_landsat_api_{imageVersionStr}.tar")]
    
    Command.RawCommand("docker", dockerBuildArgs)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> Proc.run
    |> ignore
    
    logInfo "Successfully saved docker image"
)

Target.create publishDockerImage (fun _ ->
    // docker hub auth
    let dockerHubUsername = Environment.GetEnvironmentVariable("DOCKER_HUB_USERNAME")
    let dockerHubPassword = Environment.GetEnvironmentVariable("DOCKER_HUB_PASSWORD")
    
    let dockerLoginArgs =
        Arguments.Empty
        |> Arguments.append ["login"]
        |> Arguments.append ["-u"; dockerHubUsername]
        |> Arguments.append ["-p"; dockerHubPassword]
    
    Command.RawCommand("docker", dockerLoginArgs)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.redirectOutput
    |> CreateProcess.disableTraceCommand
    |> Proc.run
    |> ignore
    
    logInfo "Successfully logged into docker hub"
    
    
    // docker push
    // assumes you have a repo with the same name as 'imageTag' in docker hub
    let dockerPushArgs = Arguments.append ["push"; imageTag] Arguments.Empty
    
    Command.RawCommand("docker", dockerPushArgs)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> Proc.run
    |> ignore
    
    logInfo "Successfully pushed"
)

Target.create deployImage (fun _ ->
    task {
        let deploymentHook = Environment.GetEnvironmentVariable("LANDSAT_API_RENDER_DEPLOY_HOOK")
        use httpClient = new HttpClient()
        
        use httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, deploymentHook)
        let response = httpClient.Send(httpRequestMessage)
        let! responseContentsStr = response.Content.ReadAsStringAsync()
        
        logInfo responseContentsStr
        logInfo "Successfully pushed"
    }
    |> _.Wait()
)



buildDockerImage
  ==> publishDockerImage
  ==> deployImage

Target.runOrDefault deployImage