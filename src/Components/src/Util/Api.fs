module Swate.Components.Api

open Shared
open Fable.Core
open Fable.Remoting.Client

let ontology : IOntologyAPIv3 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IOntologyAPIv3>



// https://api.terminology.tib.eu/swagger-ui/index.html
open Fetch

let private makeQueryParam (name: string, value: obj) =
  Fable.Core.JS.encodeURIComponent name + "=" + (string >> Fable.Core.JS.encodeURIComponent) value

let private makeQueryParamStr (queryParams: (string * obj) list) : string =
    queryParams
    |> List.map (fun (name, value) -> makeQueryParam(name, value))
    |> String.concat "&"
    |> (+) "?"

let private appendQueryParams (url: string) (queryParams: (string * obj) list) : string =
    url + makeQueryParamStr queryParams


[<RequireQualifiedAccess>]
module TIBTypes =
    type Term =
        abstract label: string
        abstract iri: string
        abstract obo_id: string
        abstract ontology_name: string
        abstract ontology_prefix: string option
        abstract description: string []
        abstract hasChildren: bool option

    type TermArray =
        abstract terms: Term []

    type TermApi =
        abstract _embedded: TermArray

[<AttachMembers>]
type TIB =
    static member getIRIFromOboId (oboId: string) =
        fetch
            (appendQueryParams "https://api.terminology.tib.eu/api/terms" ["obo_id", oboId])
            [
                RequestProperties.Method HttpMethod.GET
                requestHeaders  [ HttpRequestHeaders.Accept "application/json" ]
            ]
        |> Promise.bind(fun response ->
            response.json<TIBTypes.TermApi>()
        )
