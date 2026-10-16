module ARCitISA.Components.Api.ARCitISAApi

open ARCitISA.Components
open ARCitISA.Components.Shared
open Fable.Core
open Fable.Core.JsInterop
open Fable.Remoting.Client

let ARCitISAApi: IOntologyAPIv3 =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IOntologyAPIv3>

let ARCitISAApiTemplateApi: ITemplateAPIv1 =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITemplateAPIv1>
