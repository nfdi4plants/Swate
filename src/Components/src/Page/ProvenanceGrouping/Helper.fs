namespace Swate.Components.Page.ProvenanceGrouping

[<Fable.Core.Mangle(false)>]
module Exports =

    open Swate.Components.Shared.ProvenanceGrouping.Types
    open Swate.Components.Shared.ProvenanceGrouping.Session
    open Swate.Components.Page.ProvenanceGrouping.Types

    let private sideFromName =
        function
        | "Input" -> ProvenanceSide.Input
        | "Output" -> ProvenanceSide.Output
        | side -> failwithf "Unknown provenance side '%s'." side

    let private propertyHeaderByName propertyName (model: ProvenanceModel) =
        model.PropertyValues
        |> Map.toList
        |> List.map (fun (_, value) -> value.Header)
        |> List.distinct
        |> List.find (fun header -> header.Category.Name = propertyName)

    let sampleDroppedPropertyRailColor sideName propertyName layerColor =
        let session = StoryFixtures.createSampleSession ()
        let layer = Session.activeLayer session
        let side = sideFromName sideName
        let header = propertyHeaderByName propertyName layer.Model

        let uiState =
            State.init session
            |> State.Sides.ensure session
            |> State.PropertyPlacement.place layer.Id side header
            |> State.PropertyColors.setSourceColor layer.Model.Source.Id layerColor

        let projection =
            PropertyProjection.railProjectionWithFilters session layer.Id side layer.Model uiState

        projection.ColorByHeader
        |> Map.tryFind header
        |> Option.bind id
        |> Option.defaultValue ""
