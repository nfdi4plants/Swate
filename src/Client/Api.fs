module Api

open Shared
open Fable.Remoting.Client

/// A proxy you can use to talk to server directly
let api : IAnnotatorAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IAnnotatorAPIv1>

let serviceApi : IServiceAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IServiceAPIv1>

let isaDotNetCommonApi : IISADotNetCommonAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IISADotNetCommonAPIv1>