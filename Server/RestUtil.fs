namespace TwitterRestAPI.Rest

open Newtonsoft.Json
open Newtonsoft.Json.Serialization
open Suave.Successful
open Suave
open Suave.Operators

open CommonUtil

[<AutoOpen>]
module RestUtil =

    let fromJSON<'a> json = 
        JsonConvert.DeserializeObject(json, typeof<'a>) :?> 'a



    let toJson data = 
        let serializerSettings = new JsonSerializerSettings()
        serializerSettings.ContractResolver <- new CamelCasePropertyNamesContractResolver()
        JsonConvert.SerializeObject(data, serializerSettings)

        

    let getLoggedInUser (request: HttpRequest) =
        let mutable loggedInUser = None
        let tokenId = getTokenIdFromRequest request

        if tokenId.IsSome then
            loggedInUser <- getLoggedInUserByTokenId tokenId.Value
        loggedInUser



    let getBodyFromRequest<'a> (request: HttpRequest) = 
        let getString (requestData: byte[]) = 
            System.Text.Encoding.UTF8.GetString(requestData)
        ((request.rawForm |> getString |> fromJSON<'a>), (getLoggedInUser request))



    let JSONResponse response = 
        toJson response |> OK >=> Writers.setMimeType "application/json"



