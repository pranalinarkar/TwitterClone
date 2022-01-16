module ClientShellUtil

open System
open System.Net.WebSockets
open System.Threading
open System.Text
open System.Collections.Generic

open CommonUtil
open CommonTypes
open WebsocketUtil


let mutable tokenId = ""
let mutable loggedInUsername = ""


let processRegisterOption() =
    Console.Clear()
    displayHeader()

    printfn "Registering new user:"
    printf "\tUsername: "
    let username = Console.ReadLine()

    printf "\tPassword: "
    let password = getPassword()

    printfn "\n"
    let registerResponse: RegisterResponse = post "/user/register" {username=username; password=password} (dict [])

    if registerResponse.status = SUCCESS then
        printfn "Your account is created successfully."
    else
        printfn "%s" registerResponse.message.Value

    displayReturnMenu()



let processLoginOption() =
    Console.Clear()
    displayHeader()

    printfn "Login to the Twitter Simulator:"
    printf "\tUsername: "
    let username = Console.ReadLine()

    printf "\tPassword: "
    let password = getPassword()

    printfn "\n\n"
    let loginResponse: LoginResponse = post "/user/login" {username=username; password=password} (dict [])

    if loginResponse.status = SUCCESS then
        tokenId <- loginResponse.tokenId.Value
        loggedInUsername <- username
        printfn "You have successfully logged in."
        initWebsocket username
    else
        printfn "%s" loginResponse.message.Value

    displayReturnMenu()



let processFollowOption() =
    Console.Clear()
    displayHeader()

    let topUsers: UsernamesListDTO = get "/users?count=20" (dict [ ("tokenId", tokenId) ])

    if topUsers.usernames.IsSome && topUsers.usernames.Value.Length > 0 then
        printfn "Suggested users with highest followers:"
        for username in topUsers.usernames.Value do
            printfn "\t* %s" username
        printfn "\n\n"

    printf "Enter the username that you want to follow: "
    let followeeUsername = Console.ReadLine()

    let followResponse: GenericResponse = post "/user/follow" { followeeUsername=followeeUsername } (dict [ ("tokenId", tokenId) ])

    if followResponse.status = SUCCESS then
        printfn "You have successfully started following %s" followeeUsername
    else
        printfn "%s" followResponse.message

    displayReturnMenu()



let processPostTweetOption() =
    Console.Clear()
    displayHeader()

    printfn "Post a new tweet: "
    let tweet = Console.ReadLine()

    if tweet.Length > 0 then
        let postTweetResponse: GenericResponse = post "/user/tweet" { tweet=Some(tweet); tweetId=None } (dict[ ("tokenId", tokenId) ])

        if postTweetResponse.status = SUCCESS then
            printfn "Tweet posted successfully."
        else
            printfn "%s" postTweetResponse.message

    displayReturnMenu()



let displayLatestTweets (tweetType: TWEET_TYPE) =
    Console.Clear()
    displayHeader()

    let endpoint = 
        if tweetType = TWEET_TYPE.TWEET then "/users/followers/tweets"
        else "/users/tweets/mentioned"

    printfn "Loading latest tweets..."
    let tweetsResponse: TweetsResponseDTO = get endpoint (dict [ ("tokenId", tokenId) ])

    if tweetsResponse.status = SUCCESS then
        Console.Clear()
        displayHeader()

        if tweetsResponse.tweets.IsSome && tweetsResponse.tweets.Value.Length > 0 then
            printfn "Lastest Tweets:"
            let mutable counter = 1
            let tweetsDict = new Dictionary<int, string>()
            for tweet in tweetsResponse.tweets.Value do
                printfn "%d. %s - by %s" counter tweet.tweet tweet.username
                tweetsDict.Add(counter, tweet.id)
                counter <- counter + 1

            printf "Enter the SR No for a tweet: "
            let tweetNo = Console.ReadLine() |> int

            printf "Enter tweet description: "
            let tweetText = Console.ReadLine()

            if tweetsDict.ContainsKey tweetNo then
                let retweetResponse: GenericResponse = post "/user/retweet" { tweet=Some(tweetText); tweetId=Some(tweetsDict.[tweetNo]) } (dict [ ("tokenId", tokenId) ])

                if retweetResponse.status = SUCCESS then
                    printfn "You have successfully retweeted."
                else
                    printfn "%s" retweetResponse.message
            else
                printfn "Invalid tweet number entered"
        else
            printfn "No tweets found."

    else
        printfn "Something went wrong while fetching the tweets."

    displayReturnMenu()



let processRetweetOption() =
    Console.Clear()
    displayHeader()

    printfn "Retweet a tweet:"
    displayTweetMenu()

    let option = Console.ReadLine() |> int
    match option with
    | 1 -> displayLatestTweets TWEET_TYPE.TWEET
    | 2 -> displayLatestTweets TWEET_TYPE.MENTION
    | _ -> printfn "Invalid option"



let processNotificationsOption() =
    Console.Clear()
    displayHeader()

    printfn "Realtime notifications:\n"

    let ws = userwebSockets.[loggedInUsername.ToLower()]

    while (ws.State = WebSocketState.Open) do
        let receiverBuffer: byte[] = Array.zeroCreate 1024
        let mutable messageReceived = false
        let mutable message = ""
        let mutable offset = 0
        let cts = new CancellationTokenSource()
        while not(messageReceived) do
            let receivedBytes = new ArraySegment<byte>(receiverBuffer, offset, 10)
            let result = ws.ReceiveAsync(receivedBytes, cts.Token) |> Async.AwaitTask |> Async.RunSynchronously
            message <- message + Encoding.UTF8.GetString(receiverBuffer, offset, result.Count)
            offset <- offset + result.Count
            if result.EndOfMessage then
                messageReceived <- true
        
        let newMessage = fromJSON<Dictionary<string, string>>(message)

        match newMessage.["messageType"] with
        | "FOLLOW_NOTIFICATION" ->
            let followerMessage: FollowNotificationDTO = fromJSON newMessage.["data"]
            printfn "%s has started following you." followerMessage.followerUsername
        | "TWEET_NOTIFICATION" ->
            let tweetNotification: TweetNotificationDTO = fromJSON newMessage.["data"]
            match tweetNotification.tweetType with
            | TWEET_TYPE.TWEET ->
                printfn "%s has posted a new tweet: %s" tweetNotification.postedBy tweetNotification.tweet
            | TWEET_TYPE.MENTION ->
                printfn "%s has mentioned you in the tweet: %s" tweetNotification.postedBy tweetNotification.tweet
        | _ -> printfn "Invalid message received."




let initClientShell() =
    let mutable exitShell = false

    while not(exitShell) do
        Console.Clear()
        displayHeader()
        displayClientMainMenu();

        let option = Console.ReadLine() |> int

        match option with
        | 1 -> processRegisterOption()
        | 2 -> processLoginOption()
        | 3 -> processFollowOption()
        | 4 -> processPostTweetOption()
        | 5 -> processRetweetOption()
        | 6 -> processNotificationsOption()
        | _ -> printfn "Invalid option"