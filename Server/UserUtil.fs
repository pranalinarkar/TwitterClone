module UserUtil

open System
open System.Collections.Generic

open CommonUtil
open CommonTypes
open DBHelper
open WebsocketUtil
open TwitterRestAPI.Rest.RestUtil

let mutable loggingEnabled = true



let registerUser ((registerDTO, user) : RegisterDTO*UserEntity option) = 
    let mutable response = Unchecked.defaultof<RegisterResponse>

    if not(isUserPresent registerDTO.username) then
        if loggingEnabled then
            printfn "[info] Register request received for user %s" registerDTO.username

        let id = 
            insert 
                (string Entity.USERS) 
                (dict [ ("@username", registerDTO.username); ("@password", (hashString registerDTO.password)) ]) true
        if loggingEnabled then
            printfn "[info] User %s registered successfully" registerDTO.username
        response <- { status=Status.SUCCESS; message=None; id=Some(id) }
    else
        response <- { status=Status.FAILED; message=Some("User already exists"); id=None }

    response



let loginUser ((loginDTO, user): LoginRequestDTO*UserEntity option) =
    if loggingEnabled then
        printfn "[info] Login request received for user %s" loginDTO.username
    let mutable loginResponse: LoginResponse = Unchecked.defaultof<LoginResponse>
    let user: UserEntity = getUserByName loginDTO.username

    if obj.ReferenceEquals(user, null) then
        if loggingEnabled then
            printfn "[error] Login failed for user %s" loginDTO.username
        loginResponse <- {
            status=Status.FAILED;
            message=Some("User not found");
            tokenId=None
        }
    else
        if user.password = (hashString loginDTO.password) then
            if loggingEnabled then
                printfn "[info] Login was successful for user %s" loginDTO.username
            let tokenId = Guid.NewGuid().ToString()
            loginResponse <- {
                status=Status.SUCCESS;
                message=None;
                tokenId=Some(tokenId)
            }
            addLoggedInUser tokenId user 
        else
            if loggingEnabled then
                printfn "[error] Login failed for user %s" loginDTO.username
            loginResponse <- {
                status=Status.FAILED;
                message=Some("Invalid password");
                tokenId=None
            }
    loginResponse
    


let followUser ((followRequest, user): FollowRequestDTO*UserEntity option) = 
    let mutable response = Unchecked.defaultof<GenericResponse>

    if (user.IsNone) then
       response <- getNotLoggedInResponse()
    else
        let userIds = getUserIdsByNames(new List<string>([ followRequest.followeeUsername.ToLower(); user.Value.username.ToLower() ]))
    
        if not(userIds.ContainsKey(followRequest.followeeUsername.ToLower())) then
            response <- { status=Status.FAILED; message="The user you want to follow does not exist." }
        elif isFollowingUser userIds.[followRequest.followeeUsername.ToLower()] user.Value.id then
            response <- { status=Status.FAILED; message="You are already following the user." }
        else
            insert 
                (string Entity.FOLLOWERS) 
                (dict [ ("@followeeId", userIds.[followRequest.followeeUsername.ToLower()]); ("@followerId", userIds.[user.Value.username.ToLower()]) ]) false |> ignore
            response <- { status=Status.SUCCESS; message=(sprintf "You are now following %s" followRequest.followeeUsername) }
            sendWsMessage followRequest.followeeUsername (dict [ ("messageType", "FOLLOW_NOTIFICATION"); ("data", (toJson { followerUsername=user.Value.username })) ]) 
            if loggingEnabled then
                printfn "[info] %s started following %s" user.Value.username followRequest.followeeUsername
    response



let sendTweetPostNotification tweetId tweet isRetweet username isReply tweetType postedBy =
    let notification = toJson { id=tweetId; tweet=tweet; tweetType=tweetType; isRetweet=isRetweet; isReply=isReply; postedBy=postedBy }
    let wsMessage = new Dictionary<string, string>(dict [ ("messageType", "TWEET_NOTIFICATION"); ("data", notification) ])
    sendWsMessage username wsMessage



let processHashtagsAndUsers (tweet: string) (tweetId: string) (conversationId: string) (userId: string) (username: string) (isRetweet: bool) = 
    let hashtags = getHashTagsFromTweet tweet
    let usernames = getUsernamesFromTweet tweet
    let notifiedUsers: HashSet<string> = new HashSet<string>()

    insert 
        (string Entity.CONVERSATION_SUBSCRIPTIONS) 
        (dict [ ("@coversationId", conversationId); ("@userId", userId) ]) false |> ignore
    
    if hashtags.Count > 0 then
        let existingHashtags = getHashTagsByValues hashtags
        let newHashtags = getListsDiff hashtags (new List<string>(existingHashtags.Keys))
        let newHashtagsMapping = new Dictionary<string, string>()
    
        if newHashtags.Count > 0 then
            for hashtag in newHashtags do
                newHashtagsMapping.Add(hashtag, insert (string Entity.HASHTAGS) (dict [ ("@value", hashtag) ]) true)
    
        for hashtag in hashtags do
            let mutable hashtagId = ""
    
            if (existingHashtags.ContainsKey(hashtag)) then
                hashtagId <- existingHashtags.[hashtag]
            else
                hashtagId <- newHashtagsMapping.[hashtag]
    
            insert 
                (string Entity.TWEETS_HASHTAGS) 
                (dict [ ("@tweetId", tweetId); ("@hashtagId", hashtag) ]) false |> ignore
    
    if usernames.Count > 0 then
        let lowerUsernames = new List<string>()
        for mentionedUsername in usernames do
            lowerUsernames.Add(mentionedUsername.ToLower())

        let existingUsers = getUserIdsByNames lowerUsernames
    
        for KeyValue(mentionedUsername, id) in existingUsers do
            if not(isSubscriptionExists id conversationId) then
                insert 
                    (string Entity.CONVERSATION_SUBSCRIPTIONS) 
                    (dict [ ("@coversationId", conversationId); ("@userId", id) ]) false |> ignore
    
            insert 
                (string Entity.TWEETS_MENTIONS) 
                (dict [ ("@tweetId", tweetId); ("@userId", id) ]) false |> ignore

            sendTweetPostNotification tweetId tweet isRetweet mentionedUsername false MENTION username
            notifiedUsers.Add mentionedUsername |> ignore
    
    notifiedUsers



let notifyFollowers (userId: string) (username: string) (notifiedUsers: HashSet<String>) (tweetId: string) (tweet: string) (isRetweet: bool) (isReply: bool) =
    let followers = getFollowerUsernames userId
    
    if followers.Count > 0 then
        for follower in followers do
            if not(notifiedUsers.Contains follower) then
                sendTweetPostNotification tweetId tweet isRetweet follower isReply TWEET username
    

    
let postTweet ((tweetRequest, user): TweetRequestDTO*UserEntity option) =
    let mutable response = Unchecked.defaultof<GenericResponse>

    if (user.IsNone) then
        response <- getNotLoggedInResponse()
    else
        if tweetRequest.conversationId.IsNone then
            tweetRequest.conversationId <- Some(insert (string Entity.CONVERSATIONS) (new Dictionary<string, _>()) true)

        let tweetId = 
            insert 
                (string Entity.TWEETS) 
                (dict [ ("@conversationId", tweetRequest.conversationId.Value); ("@userId", user.Value.id); ("@tweet", tweetRequest.tweet.Value); ("@originTweetId", null); ("@postTime", (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString())); ("@replyingTo", null) ]) true

        let notifiedUsers = processHashtagsAndUsers tweetRequest.tweet.Value tweetId tweetRequest.conversationId.Value user.Value.id user.Value.username false
        notifyFollowers user.Value.id user.Value.username notifiedUsers tweetId tweetRequest.tweet.Value false false
        printfn "[info] %s posted a new tweet" user.Value.username
        response <- {status=SUCCESS; message="Tweet posted successfully."}

    response



let getUsersWithTopFollowers count (loggedInUser: UserEntity option) =
    if loggedInUser.IsSome then
        let usernames = getUsersWithTopFollowers count loggedInUser.Value.id
        { status=SUCCESS; usernames=Some(usernames); message=None }
    else
        { status=FAILED; usernames=None; message=Some("Logged in user not found.") }



let getTopTweets (loggedInUser: UserEntity option) =
    if loggedInUser.IsSome then
        let tweets = getLatestTweets loggedInUser.Value.id
        { status=SUCCESS; tweets=Some(tweets); message=None }
    else
        { status=FAILED; tweets=None; message=Some("Logged in user not found.") }



let getTopMentionedTweets (loggedInUser: UserEntity option) =
    if loggedInUser.IsSome then
        let tweets = getLastestMentionedTweets loggedInUser.Value.id
        { status=SUCCESS; tweets=Some(tweets); message=None }
    else
        { status=FAILED; tweets=None; message=Some("Logged in user not found.") }

       


let reTweet ((tweetRequest, loggedInUser): TweetRequestDTO*UserEntity option) = 
    let mutable response: GenericResponse = Unchecked.defaultof<GenericResponse>
    if loggedInUser.IsSome then
        let conversationId = insert (string Entity.CONVERSATIONS) (new Dictionary<string, _>()) true
        let tweetId = 
                insert 
                    (string Entity.TWEETS) 
                    (dict [ ("@conversationId", conversationId); ("@userId", loggedInUser.Value.id); ("@tweet", tweetRequest.tweet.Value); ("@originTweetId", tweetRequest.tweetId.Value); ("@postTime", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()); ("@replyingTo", null) ])
                    true

        let mutable notifiedUsers: HashSet<string> = new HashSet<string>()

        if tweetRequest.tweet.IsSome then
            notifiedUsers <- processHashtagsAndUsers tweetRequest.tweet.Value tweetId conversationId loggedInUser.Value.id loggedInUser.Value.username true
        notifyFollowers loggedInUser.Value.id loggedInUser.Value.username notifiedUsers tweetId tweetRequest.tweet.Value true false
        response <- { status=SUCCESS; message="Retweet was done successfully" }
    else
        response <- getNotLoggedInResponse()

    response
        
