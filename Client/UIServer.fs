module UIServer

open System
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Successful
open DBHelper
open CommonUtil
open System.Text.Json
open Suave.RequestErrors

let httpPort = 9000

let httpConfig = {
    defaultConfig with bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" httpPort ]
}

let httpApp = 
    choose

        [ GET >=> choose
    
            [ 
                path "/logs" >=> Files.file "views/logs.html"
                
                path "/api/logs" >=> 
                    request (fun r ->
                            let mutable pageNo = 1
                            let pageSize = 10
                            match r.queryParam "pageNo" with
                            | Choice1Of2 pageNo -> 
                                match r.queryParam "pageSize" with
                                    | Choice1Of2 pageSize -> 
                                        match r.queryParam "uQuery" with
                                        | Choice1Of2 username ->
                                            match r.queryParam "mQuery" with
                                            | Choice1Of2 message ->
                                                match r.queryParam "level" with
                                                | Choice1Of2 level -> 
                                                   match r.queryParam "type" with
                                                   | Choice1Of2 logType -> 
                                                       Writers.setHeader "Content-Type" "application/json" >=>
                                                       OK (
                                                           let logs = getLogs ((int) pageNo) ((int) pageSize) username message level logType
                                                           JsonSerializer.Serialize logs
                                                       )
                                                   | Choice2Of2 msg -> BAD_REQUEST msg
                                                | Choice2Of2 msg -> BAD_REQUEST msg
                                            | Choice2Of2 msg -> BAD_REQUEST msg
                                        | Choice2Of2 msg -> BAD_REQUEST msg
                                    | Choice2Of2 msg -> BAD_REQUEST msg
                            | Choice2Of2 msg -> BAD_REQUEST msg
                    )

                pathRegex "(.*)\.(css|png|js)" >=> Files.browseHome
            ]
    
        ]

let startUIServer() = 
    startWebServer httpConfig httpApp
