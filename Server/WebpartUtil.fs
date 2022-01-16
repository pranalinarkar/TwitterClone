module WebpartUtil

open Suave.Http
open Suave
open Suave.Filters
open Suave.Operators
open Suave.WebSocket
open Suave.RequestErrors

open TwitterRestAPI.Rest.RestUtil
open UserUtil
open WebsocketUtil


let app: WebPart = 
    choose [
        POST >=> 
            path "/user/register" >=> request (getBodyFromRequest >> registerUser >> JSONResponse)
            path "/user/login" >=> request (getBodyFromRequest >> loginUser >> JSONResponse)
            path "/user/follow" >=> request (getBodyFromRequest >> followUser >> JSONResponse)
            path "/user/tweet" >=> request (getBodyFromRequest >> postTweet >> JSONResponse)
            path "/user/retweet" >=> request (getBodyFromRequest >> reTweet >> JSONResponse)
        GET >=>
            path "/users" >=> request (fun ctx -> 
                let loggedInUser = getLoggedInUser ctx
                match ctx.queryParam "count" with
                | Choice1Of2 count ->
                    getUsersWithTopFollowers count loggedInUser |> JSONResponse
                | Choice2Of2 msg -> BAD_REQUEST msg
            )
            path "/users/followers/tweets" >=> request (getLoggedInUser >> getTopTweets >> JSONResponse)
            path "/users/tweets/mentioned" >=> request (getLoggedInUser >> getTopMentionedTweets >> JSONResponse)
        path "/websocket" >=> handShake ws
    ]

