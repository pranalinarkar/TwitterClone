module WSClientActor

open System
open System.Text
open System.Net.WebSockets
open System.Threading
open System.Collections.Generic

open Akka
open Akka.FSharp

open CommonTypes
open CommonUtil
open DBHelper


let wsClientActor (user: UserEntity) (loggingEnabled: bool) (tokenId: string) (mailbox: Actor<_>) = 

    let mutable ws = Unchecked.defaultof<ClientWebSocket>
    let mutable receivedNotificationCount = 0

    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with
        | "INIT" ->
            ws <- new ClientWebSocket()
            let uri = new Uri("ws://localhost:8080/websocket")
            let cts = new CancellationTokenSource()
            ws.ConnectAsync(uri, cts.Token) |> Async.AwaitTask |> Async.RunSynchronously

            let wsMessage = new Dictionary<string, string>()
            wsMessage.Add("messageType", "INIT")
            wsMessage.Add("username", user.username)

            Akka.Dispatch.ActorTaskScheduler.RunTask(fun () -> 
                async {
                    let bytesToSend = new ArraySegment<byte>(Encoding.UTF8.GetBytes(toJson wsMessage))
                    ws.SendAsync(bytesToSend, WebSocketMessageType.Text, true, cts.Token) |> Async.AwaitTask |> ignore
                } |> Async.StartAsTask :> Threading.Tasks.Task
            )

            
            Akka.Dispatch.ActorTaskScheduler.RunTask(fun () -> 
                async {
                    let receiverBuffer: byte[] = Array.zeroCreate 1024
                    let mutable messageReceived = false
                    let mutable message = ""
                    let mutable offset = 0

                    while not(messageReceived) do
                        let receivedBytes = new ArraySegment<byte>(receiverBuffer, offset, 10)
                        let! result = ws.ReceiveAsync(receivedBytes, cts.Token) |> Async.AwaitTask
                        message <- message + Encoding.UTF8.GetString(receiverBuffer, offset, result.Count)
                        offset <- offset + result.Count

                        if result.EndOfMessage then
                            messageReceived <- true

                    let receivedMessage: Dictionary<string, string> = fromJSON<Dictionary<string, string>>(message)

                    match receivedMessage.["messageType"] with
                    | "TWEET_NOTIFICATION" ->
                        let tweetNotification: TweetNotificationDTO = fromJSON receivedMessage.["data"]

                        if loggingEnabled then
                            insertLog (getLogDTO user.username LOG_LEVEL.INFO LOG_TYPE.TWEETNOTIFICATION (String.Format("[info] Received tweet notification: [Tweet={0}, Posted By={1}, Retweet={2}]", tweetNotification.tweet, tweetNotification.postedBy, tweetNotification.isRetweet)))

                            if not(tweetNotification.isRetweet) then
                                receivedNotificationCount <- receivedNotificationCount + 1
                                    
                                if receivedNotificationCount%10 = 0 then
                                    let retweetResponse: GenericResponse = post "/user/retweet" { tweet=Some(generateRetweet()); tweetId=Some(tweetNotification.id) } (dict [ ("tokenId", tokenId) ])
                                        
                                    if loggingEnabled then
                                        insertLog (getLogDTO user.username LOG_LEVEL.INFO LOG_TYPE.RETWEET (String.Format("[info] Retweeting: {0}", tweetNotification.tweet)))
                    | _ -> printfn "Invalid message received."
                } |> Async.StartAsTask :> Threading.Tasks.Task
                
            )

            mailbox.Self <! "READ"


        | "READ" ->

            if ws.State = WebSocketState.Open then
                let cts = new CancellationTokenSource()
                Akka.Dispatch.ActorTaskScheduler.RunTask(fun () -> 
                    async {
                        let receiverBuffer: byte[] = Array.zeroCreate 1024
                        let mutable messageReceived = false
                        let mutable message = ""
                        let mutable offset = 0

                        while not(messageReceived) do
                            let receivedBytes = new ArraySegment<byte>(receiverBuffer, offset, 10)
                            let! result = ws.ReceiveAsync(receivedBytes, cts.Token) |> Async.AwaitTask
                            message <- message + Encoding.UTF8.GetString(receiverBuffer, offset, result.Count)
                            offset <- offset + result.Count

                            if result.EndOfMessage then
                                messageReceived <- true

                        let receivedMessage: Dictionary<string, string> = fromJSON<Dictionary<string, string>>(message)

                        match receivedMessage.["messageType"] with
                        | "TWEET_NOTIFICATION" ->
                            let tweetNotification: TweetNotificationDTO = fromJSON receivedMessage.["data"]

                            if loggingEnabled then
                                insertLog (getLogDTO user.username LOG_LEVEL.INFO LOG_TYPE.TWEETNOTIFICATION (String.Format("[info] Received tweet notification: [Tweet={0}, Posted By={1}, Retweet={2}]", tweetNotification.tweet, tweetNotification.postedBy, tweetNotification.isRetweet)))

                                if not(tweetNotification.isRetweet) then
                                    receivedNotificationCount <- receivedNotificationCount + 1
                                        
                                    if receivedNotificationCount%10 = 0 then
                                        let retweetResponse: GenericResponse = post "/user/retweet" { tweet=Some(generateRetweet()); tweetId=Some(tweetNotification.id) } (dict [ ("tokenId", tokenId) ])
                                            
                                        if loggingEnabled then
                                            insertLog (getLogDTO user.username LOG_LEVEL.INFO LOG_TYPE.RETWEET (String.Format("[info] Retweeting: {0}", tweetNotification.tweet)))
                        | _ -> printfn "Invalid message received."
                    } |> Async.StartAsTask :> Threading.Tasks.Task
                    
                )
                mailbox.Self <! "READ"

        | _ -> printfn "Invalid message received"
        
        return! loop()
    }
    loop()

