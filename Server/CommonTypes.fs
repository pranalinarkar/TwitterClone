module CommonTypes


open System.Collections.Generic


type Status = 
    | SUCCESS
    | FAILED


type Entity = 
    | USERS
    | FOLLOWERS
    | CONVERSATIONS
    | TWEETS
    | CONVERSATION_SUBSCRIPTIONS
    | HASHTAGS
    | TWEETS_HASHTAGS
    | TWEETS_MENTIONS



type TWEET_TYPE = 
    | TWEET
    | MENTION



type WSMessageType = 
    | INIT



type RegisterDTO = {
    username: string;
    password: string;
}



type LoginRequestDTO = {
    username: string;
    password: string;
}



type GenericResponse = {
    status: Status;
    message: string
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



//Entities
type UserEntity = {
    id: string
    username: string
    password: string
}



//Websocket messages
type WSMessage = {
    msgType: WSMessageType;
    username: string;
}



type FollowRequestDTO = {
    followeeUsername: string
}



type TweetRequestDTO = {
    tweet: string option
    tweetId: string option
    mutable conversationId: string option
}



type TweetNotificationDTO = {
    id: string;
    tweet: string;
    tweetType: TWEET_TYPE;
    isRetweet: bool;
    postedBy: string;
    isReply: bool;
}



type FollowNotificationDTO = {
    followerUsername: string
}


type UsernamesListDTO = {
    status: Status;
    message: string option;
    usernames: List<string> option;
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