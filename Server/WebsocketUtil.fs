module WebsocketUtil

open System
open Suave
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open System.Collections.Concurrent
open System.Collections.Generic

open TwitterRestAPI.Rest.RestUtil


let userSocketConnections = new ConcurrentDictionary<string, WebSocket>()


let ws (webSocket : WebSocket) (context: HttpContext) =
    socket {
        let mutable loop = true
  
        while loop do
        let! msg = webSocket.read()
  
        match msg with
        | (Text, data, true) ->
            let wsMessage = fromJSON<Dictionary<string, string>> (UTF8.toString data)
            match wsMessage.["messageType"] with
            | "INIT" ->
                printfn "[info] Websocket connection established for user %s" wsMessage.["username"]
                userSocketConnections.TryAdd(wsMessage.["username"].ToLower(), webSocket) |> ignore
            | _ -> printfn "Invalid message received"
          
  
        | (Close, _, _) ->
            let emptyResponse = [||] |> ByteSegment
            do! webSocket.send Close emptyResponse true
  
            // after sending a Close message, stop the loop
            loop <- false
  
        | _ -> ()
    }



let sendWsMessage (username: string) (wsMessage: IDictionary<string, string>) = 
    if userSocketConnections.ContainsKey (username.ToLower()) then
        let ws = userSocketConnections.[username.ToLower()]
        let jsonMessage = toJson wsMessage

        let byteResponse =
            jsonMessage
            |> System.Text.Encoding.ASCII.GetBytes
            |> ByteSegment

        lock ws (fun () -> 
            async {
                ws.send Text byteResponse true |> Async.StartAsTask |> Async.AwaitTask |> Async.RunSynchronously |> ignore
            } |> Async.RunSynchronously
            printfn "[info] Websocket message pushed"
        )

