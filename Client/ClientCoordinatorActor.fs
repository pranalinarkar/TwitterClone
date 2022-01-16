module ClientCoordinatorActor

open CommonUtil
open CommonTypes
open DBHelper
open UIServer
open ClientActor

open Akka.FSharp
open Akka
open System
open System.IO
open System.Reflection

let usernamePrefix = "TwitterSimulatorUser"

let clientActorsCoordinator (usersCount: int) (loggingEnabled: bool) (mailbox: Actor<string>) = 
    let logPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/logs/client/coordinator/coordinator.log"

    let rec loop() = actor {
        let! message = mailbox.Receive()

        match message with
        | "INIT" ->
            printfn "[info ]Initializing the user actors"
            let mutable rankDivider = 1;

            for i in 1..usersCount do
                let password = Guid.NewGuid().ToString()
                let username = usernamePrefix + i.ToString()

                let registerResponse: RegisterResponse = post "/user/register" { username=username; password=password } (dict [])

                if registerResponse.status = SUCCESS then
                    if loggingEnabled then
                        insertLog (getLogDTO username LOG_LEVEL.INFO LOG_TYPE.REGISTER (String.Format("[info] User {0} registered successfully", username)))
                    
                    let rank = (usersCount - 1)/rankDivider
                    rankDivider <- rankDivider + 1
                    addUserDetails({id=registerResponse.id.Value; username=username; password=password; rank=rank})
                else
                    if loggingEnabled then
                        insertLog (getLogDTO username LOG_LEVEL.ERROR LOG_TYPE.REGISTER (String.Format("[error] Registration failed for user {0} with error {1}", username, registerResponse.message.Value)))
              
            for user in userDetails.Values do
                let clientActorRef = spawn mailbox.Context user.username (clientActor user usersCount loggingEnabled)
                clientActorRef <! "INIT"
            

            printfn "[info] All user actors initialized successfully and simulator has begun."
            printfn "[info] Visit http://localhost:9000/logs to view all the logs"
            startUIServer()

        | _ -> printfn "Invalid message received"
        return! loop()
    }
    loop()