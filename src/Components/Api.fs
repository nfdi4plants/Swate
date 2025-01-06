module Api

open Shared
open Fable.Remoting.Client

let ontology : IOntologyAPIv3 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IOntologyAPIv3>

