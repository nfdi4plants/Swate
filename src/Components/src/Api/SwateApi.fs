module Swate.Components.Api.SwateApi

open Swate.Components
open Swate.Components.Shared
open Fable.Core
open Fable.Core.JsInterop
open Fable.Remoting.Client

let SwateApi: IOntologyAPIv3 =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IOntologyAPIv3>

let SwateTemplateApi: ITemplateAPIv1 =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITemplateAPIv1>

let loadTemplates () = async {
    try
        let! json = SwateTemplateApi.getTemplates ()
        return Ok(ARCtrl.Json.Templates.fromJsonString json)
    with error ->
        return Error($"Error loading templates: {error.Message}")
}
