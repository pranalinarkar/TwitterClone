module MenuUtil

open System

open CommonUtil
open ClientShellUtil
open SimulatorUtil


let showMenu() = 
    displayHeader()
    printfn "\t1. Start Simulator"
    printfn "\t2. Open Shell"
    printf "Enter your option: "

    let option = Console.ReadLine() |> int

    match option with
    | 1 ->
        initSimulator()

    | 2 ->
        initClientShell()

    | _ ->  printfn "Invalid option entered. Exting the simulator"

    0 // returning empty value