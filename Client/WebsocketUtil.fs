module WebsocketUtil

open System
open System.Collections.Generic
open System.Collections.Concurrent
open System.Net.WebSockets
open System.Threading
open System.Text

open CommonUtil

let websocketEndpoint = "ws://localhost:8080/websocket"
let userwebSockets = new ConcurrentDictionary<string, ClientWebSocket>()


let initWebsocket (username: string) =
    let ws = new ClientWebSocket()
    let uri = new Uri(websocketEndpoint)
    let cts = new CancellationTokenSource()
    ws.ConnectAsync(uri, cts.Token) |> Async.AwaitTask |> Async.RunSynchronously
    
    userwebSockets.TryAdd((username.ToLower()), ws) |> ignore
    let wsMessage = new Dictionary<string, string>()
    wsMessage.Add("messageType", "INIT")
    wsMessage.Add("username", username)

    task {
        let json = toJson wsMessage
        let bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json))
        do! ws.SendAsync(bytesToSend, WebSocketMessageType.Text, true, cts.Token)
    } |> Async.AwaitTask |> Async.RunSynchronously