open Suave

open WebpartUtil
open DBHelper


[<EntryPoint>]
let main argv =

    initDB()
    startWebServer (defaultConfig) app
    
    0 //returning empty value