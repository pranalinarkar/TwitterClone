module DBHelper

open CommonTypes
open CommonUtil
open System
open System.Collections.Generic
open System.Data.SQLite

let mutable dbConnection: SQLiteConnection = Unchecked.defaultof<SQLiteConnection>

let CREATE_LOGS_TABLE_QUERY = 
    "CREATE TABLE LOGS ("
    + "ID varchar(255) NOT NULL PRIMARY KEY, "
    + "LOG_TIME integer, "
    + "LEVEL varchar(255), "
    + "TYPE varchar(255), "
    + "USERNAME varchar(255), "
    + "LOG varchar(1000) "
    + ")"


let createSQLiteCommand (query: string) (queryParams: IDictionary<string, _>) = 
    let command = new SQLiteCommand(query, dbConnection);
    for KeyValue(key, value) in queryParams do
        command.Parameters.AddWithValue(key, value) |> ignore
    command


let executeQuery (query: string) =
    let command = createSQLiteCommand query (dict [])
    command.ExecuteNonQuery()


let initDB() = 
    printfn "Intializing the database"
    dbConnection <- new SQLiteConnection("Data Source=:memory:;Version=3;New=True;")
    dbConnection.Open()
    executeQuery CREATE_LOGS_TABLE_QUERY |> ignore
    printfn "Database initalized successfully"


let getLogs (pageNo: int) (pageSize: int) (username: string) (message: string) (level: string) (logType: string) = 
    let offset = (pageNo-1)*pageSize
    let mutable query = "select id, log_time, level, type, username, log from logs";
    let qParams = new Dictionary<string, Object>()
    let mutable whereAdded = false

    if username.Length > 0 then
        query <- query + " where username like '%" + (username.ToLower()) + "%' "
        whereAdded <- true

    if message.Length > 0 then
        if whereAdded then
            query <- query + " and "
        else
            query <- query + " where "
            whereAdded <- true
        query <- query + " lower(log) like '%" + message.ToLower() + "%' "

    if level.Length > 0 && level <> "All" then
        if whereAdded then
            query <- query + " and "
        else 
            query <- query + " where "
            whereAdded <- true
        query <- query + " level = '" + level.ToUpper() + "'"

    if logType.Length > 0 && logType <> "All" then
        if whereAdded then
            query <- query + " and "
        else 
            query <- query + " where "
        query <- query + " type = '" + logType.ToUpper() + "'"

    query <- query + " order by log_time desc limit " + pageSize.ToString() + " offset " + offset.ToString()

    printfn "%s" query
    printfn "%s" (string LOG_TYPE.TWEETNOTIFICATION)

    let command = createSQLiteCommand query qParams
    let logs: List<LogDTO> = new List<LogDTO>()
    let reader = command.ExecuteReader()

    while reader.Read() do
        logs.Add({
            ID = reader.["id"].ToString();
            logTime = reader.["log_time"] :?> int64;
            logType = getLogType (reader.["type"].ToString())
            logLevel = getLogLevel (reader.["level"].ToString());
            log = reader.["log"].ToString();
            username = reader.["username"].ToString()
        })

    logs


let insertLog (logDTO: LogDTO) = 
    let query = "insert into logs values (@id, @logTime, @level, @type, @username, @log)";
    let command = createSQLiteCommand query (dict [ ("@id", logDTO.ID); ("@logTime", logDTO.logTime.ToString()); ("@level", string logDTO.logLevel); ("@type", string logDTO.logType); ("@username", logDTO.username); ("@log", logDTO.log); ])
    command.ExecuteNonQuery() |> ignore