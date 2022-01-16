module CommonTypes


type Status = 
    | SUCCESS
    | FAILED



type TWEET_TYPE = 
    | TWEET
    | MENTION



type LOG_LEVEL =
    | INFO
    | ERROR



type LOG_TYPE =
    | LOGIN
    | REGISTER
    | TWEET
    | RETWEET
    | FOLLOW
    | TWEETNOTIFICATION
    | REPLY
    | QUERY



type RegisterDTO = {
    username: string;
    password: string;
}



type LoginRequestDTO = {
    username: string;
    password: string;
}



type RegisterResponse = {
    status: Status;
    message: string option;
    id: string option;
}



type LoginResponse = {
    status: Status;
    message: string option;
    tokenId: string option;
}



type UsernamesListDTO = {
    status: Status;
    message: string option;
    usernames: List<string> option;
}



type FollowRequestDTO = {
    followeeUsername: string
}



type GenericResponse = {
    status: Status;
    message: string
}



type FollowNotificationDTO = {
    followerUsername: string
}



type TweetNotificationDTO = {
    id: string;
    tweet: string;
    tweetType: TWEET_TYPE;
    isRetweet: bool;
    postedBy: string;
    isReply: bool;
}



type TweetRequestDTO = {
    tweet: string option
    tweetId: string option
}



type LogDTO = {
    ID: string
    logLevel: LOG_LEVEL
    logType: LOG_TYPE
    logTime: int64
    username: string
    log: string
}



type UserEntity = {
    id: string
    username: string
    password: string
    rank: int
}



type TweetDTO = {
    id: string;
    tweet: string;
    username: string;
}


type TweetsResponseDTO = {
    status: Status;
    tweets: List<TweetDTO> option
    message: string option
}