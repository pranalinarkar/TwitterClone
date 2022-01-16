module DBHelper

open System
open System.Data.SQLite
open System.Collections.Generic

open CommonTypes

let CREATE_USERS_TABLE_QUERY = 
    "CREATE TABLE USERS ("
    + "ID varchar(255) NOT NULL PRIMARY KEY, "
    + "USERNAME varchar(255), "
    + "PASSWORD varchar(255) "
    + ")"

let CREATE_FOLLOWERS_TABLE_QUERY = 
    "CREATE TABLE FOLLOWERS ("
    + "USER_ID varchar(255), "
    + "FOLLOWER_USER_ID varchar(255), "
    + "PRIMARY KEY (USER_ID, FOLLOWER_USER_ID)"
    + ")"

let CREATE_CONVERSATIONS_TABLE_QUERY = 
    "CREATE TABLE CONVERSATIONS ("
    + "ID varchar(255) NOT NULL PRIMARY KEY "
    + ")"

let CREATE_TWEETS_TABLE_QUERY = 
    "CREATE TABLE TWEETS ("
    + "ID varchar(255) NOT NULL PRIMARY KEY, "
    + "CONVERSATION_ID varchar(255), "
    + "USER_ID varchar(255), "
    + "TWEET varchar(280), "
    + "ORIGIN_TWEET_ID varchar(255), "
    + "POST_TIME integer(1), "
    + "REPLYING_TO varchar(255), "
    + "FOREIGN KEY(CONVERSATION_ID) REFERENCES CONVERSATIONS(ID) "
    + "FOREIGN KEY(ORIGIN_TWEET_ID) REFERENCES TWEETS(ID) "
    + "FOREIGN KEY(REPLYING_TO) REFERENCES TWEETS(ID)"
    + ")"

let CREATE_CONVERSATION_SUBSCRIPTIONS_TABLE_QUERY = 
    "CREATE TABLE CONVERSATION_SUBSCRIPTIONS ("
    + "CONVERSATION_ID varchar(255), "
    + "USER_ID varchar(255), "
    + "PRIMARY KEY (CONVERSATION_ID, USER_ID)"
    + ")"

let CREATE_HASHTAGS_TABLE_QUERY = 
    "CREATE TABLE HASHTAGS ("
    + "ID varchar(255) NOT NULL PRIMARY KEY, "
    + "VALUE varchar(255) "
    + ")"

let CREATE_TWEETS_HASHTAGS_TABLE_QUERY = 
    "CREATE TABLE TWEETS_HASHTAGS ("
    + "TWEET_ID varchar(255), "
    + "HASHTAG_ID varchar(255), "
    + "PRIMARY KEY (TWEET_ID, HASHTAG_ID)"
    + ")"

let CREATE_TWEETS_MENTIONS_TABLE_QUERY = 
    "CREATE TABLE TWEETS_MENTIONS ("
    + "TWEET_ID varchar(255), "
    + "USER_ID varchar(255), "
    + "PRIMARY KEY (TWEET_ID, USER_ID)"
    + ")"


let mutable dbConnection: SQLiteConnection = Unchecked.defaultof<SQLiteConnection>


let createSQLiteCommand (query: string) (queryParams: IDictionary<string, _>) = 
    let command = new SQLiteCommand(query, dbConnection);
    for KeyValue(key, value) in queryParams do
        command.Parameters.AddWithValue(key, value) |> ignore
    command



let executeQuery (query: string) =
    let command = createSQLiteCommand query (dict [])
    command.ExecuteNonQuery()



let generateInQuery (query: string) (key: string) (inParams: List<String>) (paramsDict: Dictionary<string, _>) = 
    let newParams = paramsDict
    let parameters = Array.zeroCreate inParams.Count
    let mutable counter = 0;

    for param in inParams do
        let placeholderParam = String.Format(key + "{0}", counter)
        parameters.[counter] <- placeholderParam
        newParams.Add(placeholderParam, param)
        counter <- counter + 1

    (query.Replace(key, String.Join(", ", parameters)), newParams)

    

let initDB() = 
    printfn "Intializing the database"
    dbConnection <- new SQLiteConnection("Data Source=:memory:;Version=3;New=True;")
    dbConnection.Open()
    executeQuery CREATE_USERS_TABLE_QUERY |> ignore
    executeQuery CREATE_FOLLOWERS_TABLE_QUERY |> ignore
    executeQuery CREATE_CONVERSATIONS_TABLE_QUERY |> ignore
    executeQuery CREATE_TWEETS_TABLE_QUERY |> ignore
    executeQuery CREATE_CONVERSATION_SUBSCRIPTIONS_TABLE_QUERY |> ignore
    executeQuery CREATE_HASHTAGS_TABLE_QUERY |> ignore
    executeQuery CREATE_TWEETS_HASHTAGS_TABLE_QUERY |> ignore
    executeQuery CREATE_TWEETS_MENTIONS_TABLE_QUERY |> ignore
    printfn "Database initalized successfully"



let isUserPresent (username: string) = 
    let mutable isUserPresent = false
    
    let selectUserByName = "select count(*) as userCount from users where lower(username) = @username"
    let command = createSQLiteCommand selectUserByName (dict [ ("@username", username.ToLower()) ])

    let reader = command.ExecuteReader()
    while reader.Read() do
        isUserPresent <- if System.Convert.ToInt32(reader.["userCount"]) = 1 then true else false
    isUserPresent



let insert (entity: string) (queryParams: IDictionary<string, _>) (addId: bool) = 
    let mutable insertQuery = "insert into " + entity + " values("
    let mutable paramsStr = ""
    let id = Guid.NewGuid().ToString()
    let updatedQueryParams: Dictionary<string, _> = new Dictionary<string, _>(queryParams)

    if addId then
        paramsStr <- "@id, "
        updatedQueryParams.Add("@id", id)

    for key in updatedQueryParams.Keys do
        if not(key = "@id") then
            paramsStr <- paramsStr + key + ", "

    insertQuery <- insertQuery + paramsStr.Substring(0, paramsStr.Length-2) + ")"
   
    let command = createSQLiteCommand insertQuery updatedQueryParams

    command.ExecuteNonQuery() |> ignore
    id



let getUserByName (username: string) = 
    let mutable userEntity: UserEntity = Unchecked.defaultof<UserEntity>
    let mutable selectUserQuery = "select * from users where lower(username) = @username"
    let command = createSQLiteCommand selectUserQuery (dict [ ("@username", username.ToLower()) ])
    let reader = command.ExecuteReader()

    while reader.Read() do
        userEntity <- {
            id=reader.["ID"].ToString();
            username=reader.["USERNAME"].ToString();
            password=reader.["PASSWORD"].ToString();
        }
    userEntity



let getUserIdsByNames (usernames: List<string>) = 
    let mutable userIds = new Dictionary<string, string>()
    let mutable getUserIdQuery = "select id, username from users where lower(username) in (@usernames)"
    let mutable queryParams = new Dictionary<string, _>()

    let (newQuery, inParams) = generateInQuery getUserIdQuery "@usernames" usernames queryParams
    getUserIdQuery <- newQuery
    queryParams <- inParams

    let command = createSQLiteCommand getUserIdQuery queryParams
    let reader = command.ExecuteReader()

    while reader.Read() do
        userIds.Add(reader.["username"].ToString().ToLower(), reader.["ID"].ToString())

    userIds



let getHashTagsByValues (hashtags: List<string>) = 
    let existingHashtags = new Dictionary<string, string>()
    let mutable query = "select id, value from HASHTAGS where value in (@hashtags)"
    let mutable queryParams = new Dictionary<string, _>()

    let (newQuery, inParams) = generateInQuery query "@hashtags" hashtags queryParams
    query <- newQuery
    queryParams <- inParams

    let command = createSQLiteCommand query queryParams
    let reader = command.ExecuteReader()

    while reader.Read() do
        if not(existingHashtags.ContainsKey (reader.["value"].ToString())) then
            existingHashtags.Add(reader.["value"].ToString(), reader.["id"].ToString())

    existingHashtags



let isSubscriptionExists (userId: string) (conversationId: string) = 
    let query = "select count(*) as count from conversation_subscriptions where conversation_id = @convId and user_id = @userId"
    let command = createSQLiteCommand query (dict [ ("@convId", conversationId); ("@userId", userId) ])
    let mutable convSubExists: bool = false
    let reader = command.ExecuteReader()

    while reader.Read() do
        convSubExists <- if System.Convert.ToInt32(reader.["count"]) = 1 then true else false

    convSubExists



let getFollowerUsernames (userId: string) = 
    let query = "select username from users where id in (select follower_user_id from followers where user_id = @userId)"
    let command = createSQLiteCommand query (dict [ ("@userId", userId) ])
    let reader = command.ExecuteReader()
    let followers: List<string> = new List<string>()

    while reader.Read() do
        followers.Add(reader.["username"].ToString())

    followers



let getUsersWithTopFollowers limit userId =
    let query = "select us.id as USER_ID, us.username as USERNAME, count(*) as follower_count from users us " +
                "inner join followers fw on fw.USER_ID = us.ID " +
                "where us.id != @userId " +
                "group by fw.USER_ID " +
                "order by follower_count desc " +
                "limit @limit ";

    let command = createSQLiteCommand query (dict [ ("@limit", limit); ("@userId", userId) ])
    let reader = command.ExecuteReader()
    let users: List<string> = new List<string>()

    while reader.Read() do
        users.Add(reader.["USERNAME"].ToString())

    users



let isFollowingUser followeeId followerId =
    let query = "select count(*) as userCount from followers where user_id = @followeeId and follower_user_id = @followerId"
    let command = createSQLiteCommand query (dict [ ("@followeeId", followeeId); ("@followerId", followerId) ])
    let reader = command.ExecuteReader()
    let mutable following = false

    while reader.Read() do
        following <- if System.Convert.ToInt32(reader.["userCount"]) = 1 then true else false

    following



let getLatestTweets userId =
    let query = "select tw.id as id, tw.tweet as tweet, us.username as username from tweets tw " + 
                "inner join followers fw on fw.user_id = tw.user_id " + 
                "inner join users us on us.id = tw.user_id " +
                "where fw.follower_user_id = @userId " + 
                "order by post_time desc " + 
                "limit 20 ";
    let command = createSQLiteCommand query ( dict[ ("@userId", userId) ] )
    let reader = command.ExecuteReader()
    let tweets = new List<TweetDTO>()

    while reader.Read() do
        tweets.Add( { id=reader.["id"].ToString(); tweet=reader.["tweet"].ToString(); username=reader.["username"].ToString()} )

    tweets



let getLastestMentionedTweets userId =
    let query = "select tw.id as id, tw.tweet as tweet, us.username as username from tweets tw " +
                "inner join tweets_mentions tm on tm.tweet_id = tw.id " +
                "inner join users us on us.id = tw.user_id " +
                "where tm.user_id = @userId " +
                "order by tw.post_time desc " +
                "limit 20" 
    let command = createSQLiteCommand query (dict [ ("@userId", userId) ])
    let reader = command.ExecuteReader()
    let tweets = new List<TweetDTO>()

    while reader.Read() do
        tweets.Add( { id=reader.["id"].ToString(); tweet=reader.["tweet"].ToString(); username=reader.["username"].ToString() } )

    tweets


    
