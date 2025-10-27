namespace Update

open Elmish

open Messages
open OfficeInterop
open OfficeInterop.Core

open Model

module ARCitect =

    open ARCtrl
    open ARCtrl.Json

    let api = Model.ARCitect.api

    let update
        (state: ARCitect.Model)
        (model: Model.Model)
        (msg: ARCitect.Msg)
        : ARCitect.Model * Model * Cmd<Messages.Msg> =
        match msg with
        | ARCitect.Init msg ->
            match msg with
            | Start() ->
                console.log "ARCitect.Init.Start"

                let cmd =
                    Cmd.OfPromise.either
                        api.Init
                        ()
                        (Finished >> ARCitect.Init >> ARCitectMsg)
                        (curry GenericError Cmd.none >> DevMsg)

                state, model, cmd
            | ApiCall.Finished(Some(arcFile, json, dataMapParent)) ->
                let resolvedArcFile =
                    match arcFile with
                    | ARCitect.Interop.InteropTypes.ARCFile.Assay ->
                        let assay = ArcAssay.fromJsonString json
                        ArcFiles.Assay assay
                    | ARCitect.Interop.InteropTypes.ARCFile.Study ->
                        let study = ArcStudy.fromJsonString json
                        ArcFiles.Study(study, [])
                    | ARCitect.Interop.InteropTypes.ARCFile.Investigation ->
                        let inv = ArcInvestigation.fromJsonString json
                        ArcFiles.Investigation inv
                    | ARCitect.Interop.InteropTypes.ARCFile.Run -> failwith "Run has no fromJsonString implemented yet"
                    | ARCitect.Interop.InteropTypes.ARCFile.Workflow ->
                        failwith "Workflow has no fromJsonString implemented yet"
                    | ARCitect.Interop.InteropTypes.ARCFile.Template ->
                        let template = Template.fromJsonString json
                        ArcFiles.Template template
                    | ARCitect.Interop.InteropTypes.ARCFile.DataMap ->
                        let datamapParent, dataMap =
                            Decode.fromJsonString UpdateUtil.JsonHelper.wholeDatamapDecoder json

                        ArcFiles.DataMap(Some datamapParent, dataMap)

                let cmd = Spreadsheet.InitFromArcFile resolvedArcFile |> SpreadsheetMsg |> Cmd.ofMsg
                state, model, cmd

            | ApiCall.Finished(None) -> state, model, Cmd.none

        | ARCitect.Save arcFile ->
            let arcFileEnum, json =
                match arcFile with
                | ArcFiles.Assay assay -> ARCitect.Interop.InteropTypes.ARCFile.Assay, ArcAssay.toJsonString 0 assay
                | ArcFiles.Study(study, _) -> ARCitect.Interop.InteropTypes.ARCFile.Study, ArcStudy.toJsonString 0 study
                | ArcFiles.Investigation inv ->
                    ARCitect.Interop.InteropTypes.ARCFile.Investigation, ArcInvestigation.toJsonString 0 inv
                | ArcFiles.Run run -> ARCitect.Interop.InteropTypes.ARCFile.Run, ArcRun.toJsonString 0 run
                | ArcFiles.Workflow workflow ->
                    ARCitect.Interop.InteropTypes.ARCFile.Workflow, ArcWorkflow.toJsonString 0 workflow
                | ArcFiles.Template template ->
                    ARCitect.Interop.InteropTypes.ARCFile.Template, Template.toJsonString 0 template
                | ArcFiles.DataMap(datamapParent, datamap) ->
                    let json =
                        if datamapParent.IsSome then
                            UpdateUtil.JsonHelper.wholeDatamapEncoder
                                datamapParent.Value.ParentId
                                datamapParent.Value.Parent
                                datamap
                            |> Encode.toJsonString (Encode.defaultSpaces (Some 0))
                        else
                            failwith "No parent for datamap is available!"

                    ARCitect.Interop.InteropTypes.ARCFile.DataMap, json

            let cmd =
                Cmd.OfPromise.attempt api.Save (arcFileEnum, json) (curry GenericError Cmd.none >> DevMsg)

            state, model, cmd

        | ARCitect.RequestPaths msg ->
            match msg with
            | Start pojo ->
                let cmd =
                    Cmd.OfPromise.either
                        api.RequestPaths
                        pojo
                        (Finished >> ARCitect.RequestPaths >> ARCitectMsg)
                        (curry GenericError Cmd.none >> DevMsg)

                state, model, cmd
            | ApiCall.Finished(wasSuccessful: bool) ->
                let cmd =
                    if wasSuccessful then
                        Cmd.none
                    else
                        GenericError(Cmd.none, exn ("RequestPaths failed")) |> DevMsg |> Cmd.ofMsg

                state, model, cmd

        | ARCitect.ResponsePaths paths ->
            let paths = Array.indexed paths |> List.ofArray

            state,
            {
                model with
                    FilePickerState.FileNames = paths
            },
            Cmd.none

        | ARCitect.RequestFile msg ->
            match msg with
            | Start() ->
                let cmd =
                    Cmd.OfPromise.either
                        api.RequestFile
                        ()
                        (Finished >> ARCitect.RequestFile >> ARCitectMsg)
                        (curry GenericError Cmd.none >> DevMsg)

                let nextModel = {
                    model with
                        DataAnnotatorModel.Loading = true
                        DataAnnotatorModel.DataFile = None
                        DataAnnotatorModel.ParsedFile = None
                }

                state, nextModel, cmd
            | ApiCall.Finished wasSuccessful ->
                let nextModel = {
                    model with
                        DataAnnotatorModel.Loading = false
                }

                let cmd =
                    if wasSuccessful then
                        Cmd.none
                    else
                        GenericError(Cmd.none, exn ("RequestFile failed")) |> DevMsg |> Cmd.ofMsg

                state, nextModel, cmd

        | ARCitect.ResponseFile file ->
            let dataFile =
                DataAnnotator.DataFile.create (file.name, file.mimetype, file.content, file.size)

            let msg = dataFile |> Some |> DataAnnotator.UpdateDataFile |> DataAnnotatorMsg

            state,
            {
                model with
                    DataAnnotatorModel.Loading = true
            },
            Cmd.ofMsg msg

        | ARCitect.RequestPersons msg ->
            match msg with
            | Start() ->
                let cmd =
                    Cmd.OfPromise.either
                        api.RequestPersons
                        ()
                        (Finished >> ARCitect.RequestPersons >> ARCitectMsg)
                        (curry GenericError Cmd.none >> DevMsg)

                state, model, cmd
            | ApiCall.Finished persons ->
                let personsResolved =
                    persons |> Array.map (fun personJson -> Person.fromJsonString personJson)

                { state with Persons = personsResolved }, model, Cmd.none