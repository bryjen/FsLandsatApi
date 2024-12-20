module FsLandsatApi.Handlers.UserHandler

open System
open System.IO
open System.IdentityModel.Tokens.Jwt
open System.Security.Claims
open System.Text
open System.Text.Json

open Microsoft.FSharp.Core
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Options
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Http.Json
open Microsoft.Extensions.DependencyInjection

open Giraffe

open FsToolkit.ErrorHandling

open FsLandsatApi.Options
open FsLandsatApi.Models.User
open FsLandsatApi.Models.ApiResponse
open FsLandsatApi.Utils.PasswordHashing
open FsLandsatApi.Services.DbUserService


[<AutoOpen>]
module private Helpers = 
    let getRequiredQueryParameter (ctx: HttpContext) (paramName: string) =
        match ctx.TryGetQueryStringValue paramName with
        | None -> Error $"Could not find the required query parameter \"{paramName}\""
        | Some value -> Ok value

    let tryParseToInt (paramName: string) (paramValue: string) =
        match Int32.TryParse(paramValue) with
        | true, value -> Ok value
        | false, _ -> Error $"Could not parse the value of  \"{paramName}\" to an int"
        
    let tryGetUserEmail (ctx: HttpContext): Result<string, string> =
        try
            match ctx.User.Identity.IsAuthenticated with
            | true ->
                let emailClaimOption = ctx.User.Claims |> Seq.filter (fun (claim: Claim) -> claim.Type = ClaimTypes.Email) |> Seq.tryHead
                match emailClaimOption with
                | Some value ->
                    Ok value.Value
                | None ->
                    Error "Auth error: Could not infer the user email from the auth payload"
            | false ->
                Error "User is not authenticated"
        with
        | ex ->
            Error ex.Message
        
        
[<AutoOpen>]
module private Tokens =
    let createJwtToken (secret: string) (user: User) =
        let signingKey = SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        let credentials = SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        
        let mutable claims = [
            Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            Claim(JwtRegisteredClaimNames.GivenName, user.FirstName)
            Claim(JwtRegisteredClaimNames.FamilyName, user.LastName)
            Claim(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}")
            Claim(JwtRegisteredClaimNames.Email, user.Email)
        ]
        
        if user.IsAdmin then
            claims <- Claim("role", "Admin") :: claims
        
        // TODO: Properly setup issuer/audience before publishing
        let token = JwtSecurityToken(
            issuer = "FlatEarthers",
            audience = "FlatEarthers",
            claims = claims,
            notBefore = DateTime.UtcNow,
            expires = DateTime.UtcNow.AddHours(1),
            signingCredentials = credentials)
        
        JwtSecurityTokenHandler().WriteToken(token)
        
        
[<RequireQualifiedAccess>]
module UserLoginPost =
    [<CLIMutable>]
    type LoginUserRequest =
        { Email: string
          Password: string }
    
    let handler (next: HttpFunc) (ctx: HttpContext) : HttpFuncResult =
        task {
            let requestId: Guid =
                match ctx.Items.TryGetValue("requestId") with
                | true, value -> value :?> Guid
                | false, _ -> Guid.Empty
            
            let dbUserService = ctx.RequestServices.GetRequiredService<DbUserService>()
            let authTokenOptions = ctx.RequestServices.GetRequiredService<IOptions<AuthTokenOptions>>().Value
            let jsonSerializerOptions = ctx.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions
            
            use requestBodyReader = new StreamReader(ctx.Request.Body)
            let! requestBody = requestBodyReader.ReadToEndAsync()
            let registerUserRequest = JsonSerializer.Deserialize<LoginUserRequest>(requestBody, jsonSerializerOptions)
            
            let email = registerUserRequest.Email
            let password = registerUserRequest.Password
            
            return!
                dbUserService.TryGetUserByCredentials(email, password)
                |> Result.map (createJwtToken authTokenOptions.SigningKey)
                |> function
                    | Ok loginToken -> 
                        let asApiResponseObj =
                            { RequestGuid = requestId
                              ErrorMessage = None
                              Data = Some loginToken }
                        (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
                    | Error error ->
                        let asApiResponseObj =
                            { RequestGuid = requestId
                              ErrorMessage = Some error
                              Data = None }
                        (Successful.ok (json<ApiResponse<obj>> asApiResponseObj)) next ctx
        }
       
       
[<RequireQualifiedAccess>]
module UserCreatePost =
    [<CLIMutable>]
    type CreateUserRequest =
        { Email: string
          FirstName: string
          LastName: string
          Password: string
          IsEmailEnabled: bool }
    
    let handler (next: HttpFunc) (ctx: HttpContext) : HttpFuncResult =
        task {
            let requestId: Guid =
                match ctx.Items.TryGetValue("requestId") with
                | true, value -> value :?> Guid
                | false, _ -> Guid.Empty
            
            let dbUserService = ctx.RequestServices.GetRequiredService<DbUserService>()
            let authTokenOptions = ctx.RequestServices.GetRequiredService<IOptions<AuthTokenOptions>>().Value
            let jsonSerializerOptions = ctx.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions
            
            use requestBodyReader = new StreamReader(ctx.Request.Body)
            let! requestBody = requestBodyReader.ReadToEndAsync()
            let req = JsonSerializer.Deserialize<CreateUserRequest>(requestBody, jsonSerializerOptions)
           
            return!
                dbUserService.TryCreateUser(req.FirstName, req.LastName, req.Email, req.Password, req.IsEmailEnabled)
                |> Result.map (createJwtToken authTokenOptions.SigningKey)
                |> function
                    | Ok loginToken -> 
                        let asApiResponseObj =
                            { RequestGuid = requestId
                              ErrorMessage = None
                              Data = Some loginToken }
                        (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
                    | Error error ->
                        let asApiResponseObj =
                            { RequestGuid = requestId
                              ErrorMessage = Some error
                              Data = None }
                        (Successful.ok (json<ApiResponse<obj>> asApiResponseObj)) next ctx
        }
       
        
[<RequireQualifiedAccess>]
module UserPatch =
    [<CLIMutable>]
    type PatchUserRequest =
        { Email: string | null
          FirstName: string | null
          LastName: string | null
          Password: string | null
          IsEmailEnabled: Nullable<bool> }
        
    let private transformUserWithPatch (patchUserReq: PatchUserRequest) (user: User): User =
        let newEmail = if isNull patchUserReq.Email then user.Email else patchUserReq.Email
        { user with
            Email = newEmail
            FirstName = if isNull patchUserReq.FirstName then user.FirstName else patchUserReq.FirstName
            LastName = if isNull patchUserReq.LastName then user.LastName else patchUserReq.LastName
            PasswordHash = if isNull patchUserReq.Password then user.PasswordHash else (hashPassword newEmail patchUserReq.Password)
            IsEmailEnabled = if patchUserReq.IsEmailEnabled.HasValue then patchUserReq.IsEmailEnabled.Value else user.IsEmailEnabled }
        
    let private transformResult next ctx requestId result : HttpFuncResult =
        match result with
        | Ok reAuthToken -> 
            let asApiResponseObj =
                { RequestGuid = requestId
                  ErrorMessage = None
                  Data = Some reAuthToken }
            (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
        | Error error ->
            let asApiResponseObj =
                { RequestGuid = requestId
                  ErrorMessage = Some error
                  Data = None }
            (Successful.ok (json<ApiResponse<obj>> asApiResponseObj)) next ctx
        
    let handler (next: HttpFunc) (ctx: HttpContext) : HttpFuncResult =
        task {
            let dbUserService = ctx.RequestServices.GetRequiredService<DbUserService>()
            let authTokenOptions = ctx.RequestServices.GetRequiredService<IOptions<AuthTokenOptions>>().Value
            let jsonSerializerOptions = ctx.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions
            
            let requestId: Guid =
                match ctx.Items.TryGetValue("requestId") with
                | true, value -> value :?> Guid
                | false, _ -> Guid.Empty
                
            use requestBodyReader = new StreamReader(ctx.Request.Body)
            let! requestBody = requestBodyReader.ReadToEndAsync()
            let patchUserReq = JsonSerializer.Deserialize<PatchUserRequest>(requestBody, jsonSerializerOptions)
            
            let transformUser = transformUserWithPatch patchUserReq
            let tryEditUser userEmail = dbUserService.TryEditUser(transformUser, userEmail)
            let createJwtToken = createJwtToken authTokenOptions.SigningKey
            
            return!
                tryGetUserEmail ctx
                |> TaskResult.ofResult
                |> TaskResult.bind tryEditUser
                |> TaskResult.map createJwtToken 
                |> Task.bind (transformResult next ctx requestId)
        }
        
        
[<RequireQualifiedAccess>]
module UserDelete =
    let handler (next: HttpFunc) (ctx: HttpContext) =
        task {
            let requestId: Guid =
                match ctx.Items.TryGetValue("requestId") with
                | true, value -> value :?> Guid
                | false, _ -> Guid.Empty
            
            let dbUserService = ctx.RequestServices.GetRequiredService<DbUserService>()
           
            return!
                tryGetUserEmail ctx
                |> Result.bind dbUserService.TryDeleteUser
                |> function
                    | Ok _ -> 
                        let asApiResponseObj =
                            { RequestGuid = requestId
                              ErrorMessage = None
                              Data = Some "Successfully deleted user" }
                        (Successful.ok (json<ApiResponse<string>> asApiResponseObj)) next ctx
                    | Error error ->
                        let asApiResponseObj =
                            { RequestGuid = requestId
                              ErrorMessage = Some error
                              Data = None }
                        (Successful.ok (json<ApiResponse<obj>> asApiResponseObj)) next ctx
        }
