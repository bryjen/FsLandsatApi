module LandsatReflectance.Api.Services.DbUserTargetService

open System
open System.Threading.Tasks
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options

open MySql.Data.MySqlClient

open FsToolkit.ErrorHandling

open LandsatReflectance.Api.Models.User
open LandsatReflectance.Api.Options
open LandsatReflectance.Api.Services.DbUserService



let tryCreateTarget
    (log: string -> unit)
    (connection: MySqlConnection)
    (target: Target) : Result<Target, string> =
    try
        let queryStringRaw = "INSERT INTO Targets (TargetGuid, ScenePath, SceneRow, Latitude, Longitude, MinCloudCover, MaxCloudCover, NotificationOffset) VALUES (@TargetGuid, @ScenePath, @SceneRow, @Latitude, @Longitude, @MinCloudCover, @MaxCloudCover, @NotificationOffset)"
        
        use queryCommand = new MySqlCommand(queryStringRaw, connection)
        queryCommand.Parameters.AddWithValue("@TargetGuid", target.Id) |> ignore
        queryCommand.Parameters.AddWithValue("@ScenePath", target.Path) |> ignore
        queryCommand.Parameters.AddWithValue("@SceneRow", target.Row) |> ignore
        queryCommand.Parameters.AddWithValue("@Latitude", target.Latitude) |> ignore
        queryCommand.Parameters.AddWithValue("@Longitude", target.Longitude) |> ignore
        queryCommand.Parameters.AddWithValue("@MinCloudCover", target.MinCloudCoverFilter) |> ignore
        queryCommand.Parameters.AddWithValue("@MaxCloudCover", target.MaxCloudCoverFilter) |> ignore
        queryCommand.Parameters.AddWithValue("@NotificationOffset", DateTime.MinValue.Add(target.NotificationOffset)) |> ignore
        
        let queryStringToLog = 
            queryStringRaw
                .Replace("@TargetGuid", $"\"{target.Id}\"")
                .Replace("@ScenePath", $"\"{target.Path}\"")
                .Replace("@SceneRow", $"\"{target.Row}\"")
                .Replace("@Latitude", $"\"{target.Latitude}\"")
                .Replace("@Longitude", $"\"{target.Longitude}\"")
                .Replace("@MinCloudCover", $"\"{target.MinCloudCoverFilter}\"")
                .Replace("@MaxCloudCover", $"\"{target.MaxCloudCoverFilter}\"")
                .Replace("@NotificationOffset", $"\"{DateTime.MinValue.Add(target.NotificationOffset)}\"")
        log(queryStringToLog)
        
        let _ = queryCommand.ExecuteNonQuery()  // in case of a duplicate, will throw error instead
        Ok target
    with
    | ex ->
        Error ex.Message
        
// Creates an entry in the join table that links a user and a target
let tryCreateJoinEntry
    (log: string -> unit)
    (connection: MySqlConnection)
    (target: Target) : Result<Target, string> =
    try
        let queryStringRaw =
            "INSERT INTO UsersTargets (UserGuid, TargetGuid) VALUES (@UserGuid, @TargetGuid)"
        
        use queryCommand = new MySqlCommand(queryStringRaw, connection)
        queryCommand.Parameters.AddWithValue("@UserGuid", target.UserId) |> ignore
        queryCommand.Parameters.AddWithValue("@TargetGuid", target.Id) |> ignore
        
        let queryStringToLog = 
            queryStringRaw
                .Replace("@UserGuid", $"{target.UserId}")
                .Replace("@TargetGuid", $"{target.Id}")
        log(queryStringToLog)
        
        let _ = queryCommand.ExecuteNonQuery()  // in case of a duplicate, will throw error instead
        Ok target
    with
    | ex ->
        Error ex.Message
        
// Deletes an entry in the join table that links a user and target
let tryDeleteJoinEntry (logger: ILogger) (connection: MySqlConnection) (userId: Guid) (targetId: Guid) =
    try
        let queryStringRaw = "DELETE FROM UsersTargets WHERE UserGuid = @UserGuid AND TargetGuid = @TargetGuid"
        
        use queryCommand = new MySqlCommand(queryStringRaw, connection)
        queryCommand.Parameters.AddWithValue("@UserGuid", userId) |> ignore
        queryCommand.Parameters.AddWithValue("@TargetGuid", targetId) |> ignore
        
        let queryStringToLog = 
            queryStringRaw
                .Replace("@UserGuid", $"{userId}")
                .Replace("@TargetGuid", $"{targetId}")
        logger.LogInformation(queryStringToLog)
        
        let _ = queryCommand.ExecuteNonQuery()  // in case of a duplicate, will throw error instead
        Ok (userId, targetId)
    with
    | ex ->
        Error ex.Message

// Assumes that the linker entry (if it exists) is already deleted, otherwise DB error because of a foreign key constraint
let tryDeleteTarget (logger: ILogger) (connection: MySqlConnection) (targetId: Guid) =
    try
        let queryStringRaw = "DELETE FROM Targets WHERE TargetGuid = @TargetGuid"
        
        use queryCommand = new MySqlCommand(queryStringRaw, connection)
        queryCommand.Parameters.AddWithValue("@TargetGuid", targetId) |> ignore
        
        let queryStringToLog = queryStringRaw.Replace("@TargetGuid", $"{targetId}")
        logger.LogInformation(queryStringToLog)
        
        let _ = queryCommand.ExecuteNonQuery()  // in case of a duplicate, will throw error instead
        Ok targetId
    with
    | ex ->
        Error ex.Message
    
        
let parseTargetsFromDataReader (reader: MySqlDataReader) (logger: ILogger) =
    let mutable parsedTargetsList: Target list = []
    while reader.Read() do
        try
            let target =
                { UserId = reader.GetGuid(0)
                  Id = reader.GetGuid(1)
                  Path = reader.GetInt32(2)
                  Row = reader.GetInt32(3)
                  Latitude = reader.GetDouble(4)
                  Longitude = reader.GetDouble(5)
                  MinCloudCoverFilter = reader.GetDouble(6)
                  MaxCloudCoverFilter = reader.GetDouble(7)
                  NotificationOffset = reader.GetDateTime(8) - DateTime.MinValue }
                
            parsedTargetsList <- target :: parsedTargetsList
        with
        | ex ->
            logger.LogError("Failed to parse a target from a returned sql data.")
            
    parsedTargetsList


type DbUserTargetService(
    logger: ILogger<DbUserTargetService>,
    dbOptions: IOptions<DbOptions>,
    userService: DbUserService) =
    
    member this.TryGetTarget(targetId: Guid) =
        task {
            try
                use connection = new MySqlConnection(dbOptions.Value.DbConnectionString)
                connection.Open()
                
                let queryStringRaw =
                    "SELECT u.UserGuid, t.TargetGuid, t.ScenePath, t.SceneRow, t.Latitude, t.Longitude, t.MinCloudCover, t.MaxCloudCover, t.NotificationOffset
                     FROM Targets as t
                     INNER JOIN UsersTargets as ut ON t.TargetGuid = ut.TargetGuid
                     INNER JOIN Users as u ON u.UserGuid = ut.UserGuid
                     WHERE t.TargetGuid = @TargetGuid"

                use queryCommand = new MySqlCommand(queryStringRaw, connection)
                queryCommand.Parameters.AddWithValue("@TargetGuid", targetId) |> ignore

                let queryStringToLog = queryStringRaw.Replace("@TargetGuid", $"\"{targetId}\"")
                logger.LogInformation($"{queryStringToLog}")
                
                use reader = queryCommand.ExecuteReader()
                
                return
                    parseTargetsFromDataReader reader logger
                    |> List.tryHead
                    |> Ok
            with
            | ex ->
                return Error ex.Message
        }
    
    member this.TryGetTargets(userEmail: string) =
        task {
            try
                use connection = new MySqlConnection(dbOptions.Value.DbConnectionString)
                connection.Open()
                
                let queryStringRaw =
                    "SELECT u.UserGuid, t.TargetGuid, t.ScenePath, t.SceneRow, t.Latitude, t.Longitude, t.MinCloudCover, t.MaxCloudCover, t.NotificationOffset
                     FROM Targets as t
                     INNER JOIN UsersTargets as ut ON t.TargetGuid = ut.TargetGuid
                     INNER JOIN Users as u ON u.UserGuid = ut.UserGuid
                     WHERE u.Email = @Email"

                use queryCommand = new MySqlCommand(queryStringRaw, connection)
                queryCommand.Parameters.AddWithValue("@email", userEmail) |> ignore

                let queryStringToLog = queryStringRaw.Replace("@email", $"\"{userEmail}\"")
                logger.LogInformation($"{queryStringToLog}")
                
                
                use reader = queryCommand.ExecuteReader()
                return Ok (parseTargetsFromDataReader reader logger)
            with
            | ex ->
                return Error ex.Message
        }
    
    
    member this.TryAddTarget(target: Target) =
        task {
            try
                use connection = new MySqlConnection(dbOptions.Value.DbConnectionString)
                connection.Open()
                
                let log str = logger.LogInformation($"{str}")
                
                return 
                    tryCreateTarget log connection target
                    |> Result.bind (tryCreateJoinEntry log connection)
            with
            | ex ->
                return Error ex.Message
        }
        
    member this.TryDeleteTarget(userEmail: string, targetId: Guid) =
        task {
            use connection = new MySqlConnection(dbOptions.Value.DbConnectionString)
            connection.Open()
            
            let user = userService.TryGetUserByEmail(userEmail)
            
            return 
                user
                |> Result.map (fun user -> (user.Id, targetId))
                |> Result.bind (fun (userId, targetId) -> tryDeleteJoinEntry logger connection userId targetId)
                |> Result.bind (fun (_, targetId) -> tryDeleteTarget logger connection targetId )
        }
        
    member this.TryEditTarget(transformTarget: Target -> Target, targetId: Guid) : Task<Result<Target, string>> =
        
        // Helper methods
        let assertSomeTarget targetOption =
            // Wrapped with a task CE to be able to use 'TaskResult's bind fn
            match targetOption with
            | Some target -> Ok target
            | None -> Error $"Could not find a target with id \"{targetId}\""
            |> Task.FromResult
        
        let editTargetInfoInDb (target: Target) =
            try
                use connection = new MySqlConnection(dbOptions.Value.DbConnectionString)
                connection.Open()
                
                let queryStringRaw =
                    "UPDATE Targets
                     SET MinCloudCover = @MinCloudCover, MaxCloudCover = @MaxCloudCover, NotificationOffset = @NotificationOffset
                     WHERE TargetGuid = @TargetGuid"
                
                use queryCommand = new MySqlCommand(queryStringRaw, connection)
                queryCommand.Parameters.AddWithValue("@MinCloudCover", target.MinCloudCoverFilter) |> ignore
                queryCommand.Parameters.AddWithValue("@MaxCloudCover", target.MaxCloudCoverFilter) |> ignore
                queryCommand.Parameters.AddWithValue("@NotificationOffset", DateTime.MinValue.Add(target.NotificationOffset)) |> ignore
                queryCommand.Parameters.AddWithValue("@TargetGuid", targetId) |> ignore
                
                let queryStringToLog = queryStringRaw
                                            .Replace("@MinCloudCover", $"\"{target.MinCloudCoverFilter}\"")
                                            .Replace("@MaxCloudCover", $"\"{target.MaxCloudCoverFilter}\"")
                                            .Replace("@NotificationOffset", $"\"{DateTime.MinValue.Add(target.NotificationOffset)}\"")
                                            .Replace("@TargetGuid", $"\"{targetId}\"")
                logger.LogInformation(queryStringToLog)
                
                match queryCommand.ExecuteNonQuery() with
                | i when i = 1 -> Ok target
                | _ -> Error $"There was an error attempting to change the target \"{targetId}\""
            with
            | ex ->
                Error ex.Message
            |> Task.FromResult
            
        
        task {
            return!
                this.TryGetTarget(targetId)
                |> TaskResult.bind assertSomeTarget
                |> TaskResult.map transformTarget
                |> TaskResult.bind editTargetInfoInDb
        }
