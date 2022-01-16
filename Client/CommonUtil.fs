module CommonUtil

open System
open System.Text
open System.Collections.Generic
open System.Net.Http
open Newtonsoft.Json.Serialization
open Newtonsoft.Json


open CommonTypes
open Quotes


let serverBaseURL = "http://localhost:8080"
let userDetails: Dictionary<string, UserEntity> = new Dictionary<string, UserEntity>()


let displayHeader() =
    printfn "========================================================"
    printfn "Twitter Simulator Client"
    printfn "========================================================"
    printfn "\n\n"

    

let displayClientMainMenu() = 
    printfn "\t1. Register"
    printfn "\t2. Login"
    printfn "\t3. Follow"
    printfn "\t4. Post Tweet"
    printfn "\t5. Retweet"
    printfn "\t6. Notifications"
    printfn "\t7. Exit"
    printf "Enter your option: "



let displayTweetMenu() = 
    printfn "\t1. Top tweets from your followers"
    printfn "\t2. Top tweets you were mentioned in"
    printf "Enter your option: "



let displayReturnMenu() =
    printfn "Press any key to go back to the main menu"
    Console.ReadLine() |> ignore



let fromJSON<'a> json = 
    JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a



let toJson data = 
    let serializerSettings = new JsonSerializerSettings()
    serializerSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()
    JsonConvert.SerializeObject(data, serializerSettings)



let getPassword() =
    let mutable password = ""
    let mutable passwordDone = false

    while not(passwordDone) do
        let keyInfo = Console.ReadKey(true)
        if keyInfo.Key = ConsoleKey.Enter then
            passwordDone <- true
        elif keyInfo.Key = ConsoleKey.Backspace then
            if (password.Length > 0) then
                password <- password.Substring(0, password.Length-1)
                Console.Write("\b \b")
        elif keyInfo.KeyChar <> '\u0000' then
            password <- password + (string(keyInfo.KeyChar))
            Console.Write("*")

    password



let post<'a> (endpoint: string) (requestBody: Object) (headers: IDictionary<string, string>) =
    let url = serverBaseURL + endpoint
    let response = 
        task {
            use client = new HttpClient()

            for KeyValue(key, value) in headers do
                client.DefaultRequestHeaders.Add(key, value)

            let jsonRequest = JsonConvert.SerializeObject(requestBody)
            use content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
            let! response = client.PostAsync(url, content)
            let! strResponse = response.Content.ReadAsStringAsync()
            return fromJSON<'a> strResponse
        } |> Async.AwaitTask |> Async.RunSynchronously
    response



let get<'a> (endpoint: string) (headers: IDictionary<string, string>) =
    let url = serverBaseURL + endpoint
    let response = 
        task {
            use client = new HttpClient()

            for KeyValue(key, value) in headers do
                client.DefaultRequestHeaders.Add(key, value)

            let! response = client.GetAsync(url)
            let! strResponse = response.Content.ReadAsStringAsync()
            return fromJSON<'a> strResponse
        } |> Async.AwaitTask |> Async.RunSynchronously
    response



let getLogType (value: string) =
    printfn "Getting log type for %s" value
    match value with
    | "LOGIN" -> LOG_TYPE.LOGIN
    | "REGISTER" -> LOG_TYPE.REGISTER
    | "TWEET" -> LOG_TYPE.TWEET
    | "RETWEET" -> LOG_TYPE.RETWEET
    | "TWEETNOTIFICATION" -> LOG_TYPE.TWEETNOTIFICATION
    | "REPLY" -> LOG_TYPE.REPLY
    | "QUERY" -> LOG_TYPE.QUERY
    | "FOLLOW" -> LOG_TYPE.FOLLOW
    | _ -> LOG_TYPE.TWEET



let getLogLevel (value: string) = 
   match value with 
   | "INFO" -> LOG_LEVEL.INFO
   | "ERROR" -> LOG_LEVEL.ERROR
   | _ -> LOG_LEVEL.INFO



let getLogDTO (username: string) (level: LOG_LEVEL) (logType: LOG_TYPE) (log: string)  = 
    {ID=(Guid.NewGuid().ToString()); username=username; logLevel=level; logType=logType; log=log; logTime=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}



let addUserDetails (user: UserEntity) = 
    userDetails.Add(user.username.ToLower(), user)



let shuffleArray (array: UserEntity[]) =
    let random = Random()
    for i in 0 .. array.Length - 1 do
        let j = random.Next(i, array.Length)
        let pom = array.[i]
        array.[i] <- array.[j]
        array.[j] <- pom
    array



let getShuffledUsers() =
    userDetails.Values |> Seq.toArray |> shuffleArray



let getRandomUsername() = 
    let users = userDetails.Values |> Seq.toList
    "@" + users.[Random().Next(0, users.Length)].username



let generateTweet() =
    let mutable quote = getRandomQuote()
    let hashtag = getRandomHashtag()
    let taggedUser = getRandomUsername()
    quote <- quote + " " + hashtag
    quote <- quote + " " + taggedUser
    quote



let generateRetweet() = 
    "Hey guys, check out this amazing quote."