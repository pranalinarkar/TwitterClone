module SimulatorUtil


open System
open Akka.FSharp
open Akka

open ClientCoordinatorActor 
open DBHelper 

let initSimulator() =
    Console.Clear()
    printfn "Starting simulator"

    printf "Enter users count: "
    let usersCount = Console.ReadLine() |> int
    let loggingEnabled = true

    let systemName = "TwitterSystem"
    let clientCoordinatorActorName = "TwitterClientCoordinator"
    let clientSystem = System.create systemName <| Configuration.defaultConfig()
    let clientCoordinatorRef = spawn clientSystem clientCoordinatorActorName (clientActorsCoordinator usersCount loggingEnabled)
    initDB()
    clientCoordinatorRef <! "INIT"

    Console.ReadLine() |> ignore