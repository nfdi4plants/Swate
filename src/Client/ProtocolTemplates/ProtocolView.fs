module Protocol

open System

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
//open Fable.Core.JS
open Fable.Core.JsInterop

//open ISADotNet

open Model
open Messages
open Browser.Types
open Fulma.Extensions.Wikiki

open Shared

open OfficeInterop
open Protocol

//let isViableISADotNetProcess (isaProcess:ISADotNet.Process) =

//    // The following comment was written with another function in mind, but can be used as base for documentation.

//    let isExistingChecks =
//        let hasExecProtocol         = "executesProtocol", isaProcess.ExecutesProtocol.IsSome
//        let hasProtocolParams       = "parameterValues", isaProcess.ParameterValues.IsSome
//        let hasExecProtocolParams   =
//            "executesProtocol.parameters",
//                if isaProcess.ExecutesProtocol.IsSome then
//                    isaProcess.ExecutesProtocol.Value.Parameters.IsSome
//                else
//                    false
//        let hasAnnoTSRTAN =
//            isaProcess.ExecutesProtocol.Value.Parameters.Value
//            |> List.map (fun p ->
//                if p.ParameterName.IsSome then
//                    "executesProtocol.parameters.Anno/TSR/TAN",
//                    p.ParameterName.Value.Name.IsSome
//                    && p.ParameterName.Value.TermAccessionNumber.IsSome
//                    && p.ParameterName.Value.TermSourceREF.IsSome
//                else
//                    "executesProtocol.parameters.Anno/TSR/TAN",
//                    false
//            )
//        let hasAnnoTSRTAN2 =
//            isaProcess.ParameterValues.Value
//            |> List.map (fun x ->
//                if x.Category.IsSome then
//                    if x.Category.Value.ParameterName.IsSome then
//                        "parameterValues.category.parameterName.Anno/TST/TAN",
//                        x.Category.Value.ParameterName.Value.Name.IsSome
//                        && x.Category.Value.ParameterName.Value.TermSourceREF.IsSome
//                        && x.Category.Value.ParameterName.Value.TermAccessionNumber.IsSome
//                    else
//                        "parameterValues.category.parameterName.Anno/TST/TAN",
//                        false
//                else
//                    "parameterValues.category.parameterName.Anno/TST/TAN",
//                    false
//            )
//        let hasParameterValueValue =
//            isaProcess.ParameterValues.Value
//            |> List.map (fun x ->
//                "parameterValues.Value",
//                x.Value.IsSome
//            )
//        [|hasExecProtocol; hasProtocolParams; hasExecProtocolParams; yield! hasAnnoTSRTAN; yield! hasAnnoTSRTAN; yield! hasParameterValueValue|]
//        |> Collections.Array.filter (fun (param,isExisting) ->
//            isExisting = false
//        )
//    if isExistingChecks |> Collections.Array.isEmpty then
//        let execParams = isaProcess.ExecutesProtocol.Value.Parameters.Value
//        let paramValuePairs = isaProcess.ParameterValues.Value
//        /// As we want a very controlled environment we establish this failsafe for now. This can be changed later on.
//        let isSameLength = execParams.Length = paramValuePairs.Length
//        /// As we want a very controlled environment we establish this failsafe for now. This can be changed later on.
//        let hasSameEntrys =
//            paramValuePairs
//            |> List.choose (fun paramValuePair ->
//                let param = paramValuePair.Category.Value.ParameterName.Value
//                execParams |> List.tryFind (fun execParam ->
//                    execParam.ParameterName.Value.Name                      = param.Name
//                    && execParam.ParameterName.Value.TermAccessionNumber    = param.TermAccessionNumber
//                    && execParam.ParameterName.Value.TermSourceREF          = param.TermSourceREF
//                )
//            )
//        if not isSameLength then
//            false, Some <| sprintf "Process.ExecutesProtocol.Parameters and Process.ParameterValues has not the same number of items: %i - %i" execParams.Length paramValuePairs.Length
//        elif hasSameEntrys.Length <> paramValuePairs.Length then
//            false, Some <| sprintf "Process.ExecutesProtocol.Parameters and Process.ParameterValues has different values in (category.)parameterName.annotationValue."
//        else
//            true, None
//    else
//        false, Some <| sprintf "Process contains missing values: %A" (isExistingChecks |> Collections.Array.map fst)

//let paramValuePairElement (model:Model) (ppv:ISADotNet.ProcessParameterValue) =
//    Table.table [
//        Table.IsFullWidth;
//        Table.IsBordered
//        Table.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.BodyBackground; Color model.SiteStyleState.ColorMode.Text]]
//    ][
//        thead [][
//            tr [][
//                th [Style [Width "50%"; Color model.SiteStyleState.ColorMode.Text]] [
//                    str (annotationValueToString ppv.Category.Value.ParameterName.Value.Name.Value)
//                ]
//                th [Style [Color model.SiteStyleState.ColorMode.Text]][
//                    str (termAccessionReduce ppv.Category.Value.ParameterName.Value.TermAccessionNumber.Value)
//                ]
//            ]
//        ]
//        tbody [][
//            tr [Style [Width "50%"] ][
//                let isOntology = valueIsOntology ppv.Value.Value
//                td [][
//                    str (
//                        if isOntology.IsSome then
//                            isOntology.Value.Name
//                        elif ppv.Unit.IsSome then
//                            let unitName = ppv.Unit.Value.Name.Value |> annotationValueToString
//                            let value = valueToString ppv.Value.Value
//                            sprintf "%s %s" value unitName
//                        else
//                            valueToString ppv.Value.Value
//                    )
//                ]
//                td [][
//                    str (
//                        if isOntology.IsSome then
//                            isOntology.Value.TermAccession
//                        elif ppv.Unit.IsSome then
//                            ppv.Unit.Value.TermAccessionNumber.Value |> termAccessionReduce
//                        else
//                            valueToString ppv.Value.Value
//                    )
//                ]
//            ]
//        ]
//    ]

///// only diplayed if model.ProtocolInsertState.ProcessModel.IsSome
//let displayProtocolInfoElement isViable (errorMsg:string option) (model:Model) dispatch =

//    if not isViable then
//        [
//            Label.label [Label.Props [Style [Color NFDIColors.Red.Base]]][str "The following errors occured:"]
//            str (errorMsg.Value)
//        ] 
//    else 
//        [
//            let paramValuePairs = model.ProtocolInsertState.ProcessModel.Value.ParameterValues.Value
//            Field.div [][
//                yield div [Style [MarginBottom "1rem"]][
//                    b [][ str model.ProtocolInsertState.ProcessModel.Value.ExecutesProtocol.Value.Name.Value ]
//                    str (sprintf " - Version %s" model.ProtocolInsertState.ProcessModel.Value.ExecutesProtocol.Value.Version.Value)
//                ]
//                for paramValuePair in paramValuePairs do
//                    yield paramValuePairElement model paramValuePair
//            ]
//        ]

open Messages
open Elmish

module TemplateFromJsonFile =

    let fileUploadButton (model:Model) dispatch =
        let uploadId = "UploadFiles_ElementId"
        Label.label [Label.Props [Style [FontWeight "normal"]]][
            Input.input [
                Input.Props [
                    Id uploadId
                    Type "file"; Style [Display DisplayOptions.None]
                    OnChange (fun ev ->
                        let files : FileList = ev.target?files

                        let fileNames =
                            [ for i=0 to (files.length - 1) do yield files.item i ]
                            |> List.map (fun f -> f.slice() )

                        let reader = Browser.Dom.FileReader.Create()

                        reader.onload <- fun evt ->
                            UpdateUploadFile evt.target?result |> ProtocolMsg |> dispatch
                                   
                        reader.onerror <- fun evt ->
                            curry GenericLog Cmd.none ("Error", evt.Value) |> DevMsg |> dispatch

                        reader.readAsText(fileNames |> List.head)

                        let picker = Browser.Dom.document.getElementById(uploadId)
                        // https://stackoverflow.com/questions/3528359/html-input-type-file-file-selection-event/3528376
                        picker?value <- null
                    )
                ]
            ]
            Button.a [Button.Color Color.IsInfo; Button.IsFullWidth][
                str "Upload protocol"
            ]
        ]

    let fileUploadEle (model:Model) dispatch =
        let hasData = model.ProtocolState.UploadedFile <> ""
        Columns.columns [Columns.IsMobile][
            Column.column [][
                fileUploadButton model dispatch
            ]
            if hasData then
                Column.column [Column.Width(Screen.All, Column.IsNarrow)][
                    Button.a [
                        Button.OnClick (fun e -> UpdateUploadFile "" |> ProtocolMsg |> dispatch)
                        Button.Color IsDanger
                    ][
                        Fa.i [Fa.Solid.Times][]
                    ]
                ]
        ]

    let dropdownItem (exportType:JsonExportType) (model:Model) msg (isActive:bool) =
        Dropdown.Item.a [
            Dropdown.Item.Props [
                TabIndex 0
                OnClick (fun e ->
                    e.stopPropagation()
                    exportType |> msg
                )
                OnKeyDown (fun k -> if (int k.which) = 13 then exportType |> msg)
                Style [if isActive then BackgroundColor model.SiteStyleState.ColorMode.ControlForeground]
            ]
    
        ][
            Text.span [
                CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipRight + " " + Tooltip.IsMultiline)
                Props [
                    Tooltip.dataTooltip (exportType.toExplanation)
                    Style [FontSize "1.1rem"; PaddingRight "10px"; TextAlign TextAlignOptions.Center; Color NFDIColors.Yellow.Darker20]
                ]
            ] [
                Fa.i [Fa.Solid.InfoCircle] []
            ]
    
            Text.span [] [str (exportType.ToString())]
        ]
    
    let parseJsonToTableEle (model:Model) (dispatch:Messages.Msg -> unit) =
        let hasData = model.ProtocolState.UploadedFile <> ""
        Field.div [Field.HasAddons][
            Control.div [][
                Dropdown.dropdown [
                    Dropdown.IsActive model.ProtocolState.ShowJsonTypeDropdown
                ][
                    Dropdown.trigger [][
                        Button.a [
                            Button.OnClick (fun e -> e.stopPropagation(); UpdateShowJsonTypeDropdown (not model.ProtocolState.ShowJsonTypeDropdown) |> ProtocolMsg |> dispatch )
                        ][
                            span [Style [MarginRight "5px"]] [str (model.ProtocolState.JsonExportType.ToString())]
                            Fa.i [Fa.Solid.AngleDown] []
                        ]
                    ]
                    Dropdown.menu [][
                        Dropdown.content [][
                            let msg = (UpdateJsonExportType >> ProtocolMsg >> dispatch)
                            dropdownItem JsonExportType.Assay model msg (model.ProtocolState.JsonExportType = JsonExportType.Assay)
                            dropdownItem JsonExportType.Table model msg (model.ProtocolState.JsonExportType = JsonExportType.Table)
                            dropdownItem JsonExportType.ProcessSeq model msg (model.ProtocolState.JsonExportType = JsonExportType.ProcessSeq)
                        ]
                    ]
                ]
            ]
            Control.div [Control.IsExpanded][
                Button.a [
                    if hasData then
                        Button.IsActive true
                    else
                        Button.Color Color.IsDanger
                        Button.Props [Disabled true]
                    Button.Color IsInfo
                    Button.IsFullWidth
                    Button.OnClick(fun e ->
                        ProtocolMsg ParseUploadedFileRequest |> dispatch
                    )
                ][
                    str "Insert json"
                ]
            ]
        ]

    let protocolInsertElement (model:Model) dispatch =
        mainFunctionContainer [
            Field.div [][
                Help.help [][
                    b [] [
                        str "Upload a "
                        a [Href "https://github.com/nfdi4plants/Swate/wiki/Insert-via-Process.json"; Target "_Blank"][ str "process.json" ]
                        str " file."
                    ]
                    str " The building blocks in this file can be group-inserted into a Swate table."
                    str " In the future these files will be accessible either by "
                    a [Href "https://github.com/nfdi4plants/Spawn"; Target "_Blank"] [str "Spawn"]
                    str " or offered as download!"
                ]
            ]

            Field.div [][
                fileUploadEle model dispatch
            ]

            parseJsonToTableEle model dispatch
        ]

module TemplateFromDB = 

    let toProtocolSearchElement (model:Model) dispatch =
        Button.span [
            Button.OnClick(fun e -> UpdatePageState (Some Routing.Route.ProtocolSearch) |> dispatch)
            Button.Color IsInfo
            Button.IsFullWidth
            Button.Props [Style [Margin "1rem 0"]]
        ] [str "Browse database"]

    let addFromDBToTableButton (model:Messages.Model) dispatch =
        Columns.columns [Columns.IsMobile][
            Column.column [][
                Field.div [] [
                    Control.div [] [
                        Button.a [
                            if model.ProtocolState.ProtocolSelected.IsSome (*&& model.ProtocolInsertState.ValidationXml.IsSome*) then
                                Button.IsActive true
                            else
                                Button.Color Color.IsDanger
                                Button.Props [Disabled true]
                            Button.IsFullWidth
                            Button.Color IsSuccess
                            Button.OnClick (fun e ->
                                let p = model.ProtocolState.ProtocolSelected.Value
                                /// Use x.Value |> Some to force an error if isNone. Otherwise AddAnnotationBlocks would just ignore it and it might be overlooked.
                                //let validation =
                                //    model.ProtocolInsertState.ValidationXml.Value |> Some
                                ProtocolIncreaseTimesUsed p.Name |> ProtocolMsg |> dispatch
                                AddAnnotationBlocks p.TemplateBuildingBlocks |> OfficeInteropMsg |> dispatch
                            )
                        ] [
                            str "Add template"
                        ]
                    ]
                ]
            ]
            if model.ProtocolState.ProtocolSelected.IsSome then
                Column.column [Column.Width(Screen.All, Column.IsNarrow)][
                    Button.a [
                        Button.OnClick (fun e -> RemoveSelectedProtocol |> ProtocolMsg |> dispatch)
                        Button.Color IsDanger
                    ][
                        Fa.i [Fa.Solid.Times][]
                    ]
                ]
        ]

    let displaySelectedProtocolEle (model:Model) dispatch =
        [
            div [Style [OverflowX OverflowOptions.Auto; MarginBottom "1rem"]] [
                Table.table [
                    Table.IsFullWidth;
                    Table.IsBordered
                    Table.Props [Style [Color model.SiteStyleState.ColorMode.Text; BackgroundColor model.SiteStyleState.ColorMode.BodyBackground]]
                ][
                    thead [][
                        tr [][
                            th [Style [Color model.SiteStyleState.ColorMode.Text]][str "Column"]
                            th [Style [Color model.SiteStyleState.ColorMode.Text]][str "Column TAN"]
                            th [Style [Color model.SiteStyleState.ColorMode.Text]][str "Unit"]
                            th [Style [Color model.SiteStyleState.ColorMode.Text]][str "Unit TAN"]
                        ]
                    ]
                    tbody [][
                        for insertBB in model.ProtocolState.ProtocolSelected.Value.TemplateBuildingBlocks do
                            yield
                                tr [][
                                    td [][str (insertBB.Column.toAnnotationTableHeader())]
                                    td [][str (if insertBB.HasExistingTerm then insertBB.ColumnTerm.Value.TermAccession else "-")]
                                    td [][str (if insertBB.HasUnit then insertBB.UnitTerm.Value.Name else "-")]
                                    td [][str (if insertBB.HasUnit then insertBB.UnitTerm.Value.TermAccession else "-")]
                                ]
                    ]
                ]
            ]
            addFromDBToTableButton model dispatch
        ]
    

    let showDatabaseProtocolTemplate (model:Messages.Model) dispatch =
        mainFunctionContainer [
            Field.div [][
                Help.help [][
                    b [] [str "Search the database for protocol templates."]
                    str " The building blocks from these templates can be inserted into the Swate table. "
                    span [Style [Color NFDIColors.Red.Base]][str "Only missing building blocks will be added."]
                ]
            ]
            Field.div [][
                toProtocolSearchElement model dispatch
            ]

            Field.div [][
                addFromDBToTableButton model dispatch
            ]
            if model.ProtocolState.ProtocolSelected.IsSome then
                Field.div [][
                    yield! displaySelectedProtocolEle model dispatch
                ]
        ]


let fileUploadViewComponent (model:Messages.Model) dispatch =
    Content.content [ Content.Props [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
        OnClick (fun e ->
            if model.ProtocolState.ShowJsonTypeDropdown then
                UpdateShowJsonTypeDropdown false |> ProtocolMsg |> dispatch
        )
        Style [MinHeight "100vh"]
    ]] [
        
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "Templates"]

        /// Box 1
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Add template from database."]

        TemplateFromDB.showDatabaseProtocolTemplate model dispatch

        /// Box 2
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Add annotation building blocks from file."]

        TemplateFromJsonFile.protocolInsertElement model dispatch

        //div [][
        //    str (
        //        let dataStr = model.ProtocolState.UploadData
        //        if dataStr = "" then "no upload data found" else sprintf "%A" model.ProtocolState.UploadData
        //    )
        //]
    ]