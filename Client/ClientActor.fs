module ClientActor

open CommonUtil
open CommonTypes
open DBHelper
open Akka.FSharp
open Akka
open System
open System.IO
open System.Reflection
open WSClientActor

let clientActor (user: UserEntity) (usersCount: int) (loggingEnabled: bool) (mailbox: Actor<_>) = 
    let mutable sessionId = ""
    let mutable followRequestsProcessed = 0
    let mutable sentTweetsCount = 0
    let mutable receivedNotificationCount = 0

    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with
        | "INIT" ->
            let loginResponse: LoginResponse = post "/user/login" { username=user.username; password=user.password } (dict [])

            if loginResponse.status = SUCCESS then
                sessionId <- loginResponse.tokenId.Value

                if loggingEnabled then
                    insertLog (getLogDTO user.username LOG_LEVEL.INFO LOG_TYPE.LOGIN (String.Format("[info] Logged in successfully with session id {0}", sessionId)))

                let wsclientActorRef = spawn mailbox.Context ("WS_" + user.username) (wsClientActor user loggingEnabled sessionId)
                wsclientActorRef <! "INIT"
                mailbox.Self <! "FOLLOW"
            else
                if loggingEnabled then
                    insertLog (getLogDTO user.username LOG_LEVEL.ERROR LOG_TYPE.LOGIN (String.Format("[error] Login failed with error {0}", loginResponse.message.Value)))


        | "FOLLOW" ->
            let users = getShuffledUsers()
            let mutable counter = 0
            while counter < user.rank do
                let requestId = Guid.NewGuid.ToString()
                let followResponse: GenericResponse = post "/user/follow" { followeeUsername=users.[counter].username } (dict[ ("tokenId", sessionId) ])
                
                if followResponse.status = SUCCESS then
                    if loggingEnabled then
                        insertLog (getLogDTO user.username LOG_LEVEL.INFO LOG_TYPE.FOLLOW (String.Format("[info] Started following the user {0}", users.[counter].username)))

                    counter <- counter + 1
                    followRequestsProcessed <- followRequestsProcessed + 1

                    if followRequestsProcessed = user.rank then
                        let interval = usersCount - user.rank
                        mailbox.Context.System.Scheduler.ScheduleTellRepeatedly(
                            System.TimeSpan.FromMilliseconds(1000.0), System.TimeSpan.FromMilliseconds(interval), mailbox.Self, "TWEET", mailbox.Self
                        )
                else
                    if loggingEnabled then
                        insertLog (getLogDTO user.username LOG_LEVEL.ERROR LOG_TYPE.FOLLOW (String.Format("[error] Error while following the user {0} - {1}", users.[counter].username, followResponse.message)))


        | "TWEET" ->
            let tweet = generateTweet()
            let tweetResponse: GenericResponse = post "/user/tweet" { tweet=Some(tweet); tweetId=None } (dict[ ("tokenId", sessionId) ])

            if tweetResponse.status = SUCCESS then
                sentTweetsCount <- sentTweetsCount + 1
                if loggingEnabled then
                    insertLog (getLogDTO user.username LOG_LEVEL.INFO LOG_TYPE.TWEET (String.Format("[info] Posting a tweet: {0}", tweet)))
            else
                if loggingEnabled then
                    insertLog (getLogDTO user.username LOG_LEVEL.ERROR LOG_TYPE.TWEET (String.Format("[error] Error while posting tweet: {0}", tweetResponse.message)))
        | _ -> printfn "Invalid message received %s" message
        
        return! loop()
    }
    loop()

