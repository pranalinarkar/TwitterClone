module CommonUtil

open CommonTypes

open System.Security.Cryptography
open System.Collections.Generic
open System.Collections.Concurrent
open Suave.Http


let loggedInUsers = new ConcurrentDictionary<string, UserEntity>()



let hashString (input: string) = 
    use sha256hash = SHA256Managed.Create();
    let inputBytes = System.Text.Encoding.ASCII.GetBytes(input)
    sha256hash.ComputeHash(inputBytes) |> Array.map (sprintf "%02X") |> String.concat ""



let addLoggedInUser tokenId user =
    loggedInUsers.TryAdd(tokenId, user) |> ignore



let getTokenIdFromRequest (request: HttpRequest) = 
    let headers = request.headers
    let mutable tokenId = None
    for header in headers do
        if (fst header) = "tokenid" then
            tokenId <- Some(snd header)
    tokenId



let getLoggedInUserByTokenId tokenId =
    let mutable user = None
    if loggedInUsers.ContainsKey tokenId then
        user <- Some(loggedInUsers.[tokenId])
    user


    
let getNotLoggedInResponse() =
    {status=FAILED; message="Logged in user not found"}



let extractDataFromTweet (tweet: string) (delimiter: string) = 
    let data = new List<string>()
    if tweet.Contains(delimiter) then
        let words = tweet.Split(" ")
        for word in words do
            if word.StartsWith(delimiter) && word.Length > 1 then
                data.Add(word.Substring(1, word.Length-1))
    data



let getHashTagsFromTweet (tweet: string) = 
    extractDataFromTweet tweet "#"
    


let getUsernamesFromTweet (tweet: string) = 
    extractDataFromTweet tweet "@"



let getListsDiff (list1: List<string>) (list2: List<string>) = 
    let diff = new List<string>()
    for ele in list1 do
        if not(list2.Contains ele) then
            diff.Add(ele)
    diff
