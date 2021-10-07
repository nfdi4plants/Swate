module SettingsXml

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
//open Fable.Core.JS
open Fable.Core.JsInterop
open Elmish

open Shared

open Model
open Messages.SettingsXml

let update (msg:Msg) (currentState: SettingsXml.Model) : SettingsXml.Model * Cmd<Messages.Msg> =

        //let matchXmlTypeToUpdateMsg msg (xmlType:OfficeInterop.Types.Xml.XmlTypes) =
        //    match xmlType with
        //    | OfficeInterop.Types.Xml.XmlTypes.ValidationType v ->
        //        Msg.Batch [
        //            GenericLog ("Info", msg) |> Dev
        //            GetAllValidationXmlParsedRequest |> SettingsXmlMsg
        //        ]
        //    | OfficeInterop.Types.Xml.XmlTypes.GroupType _ | OfficeInterop.Types.Xml.XmlTypes.ProtocolType _ ->
        //        Msg.Batch [
        //            GenericLog ("Info", msg) |> Dev
        //            GetAllProtocolGroupXmlParsedRequest |> SettingsXmlMsg
        //            UpdateProtocolGroupHeader |> ExcelInterop
        //        ]
        
        match msg with
    //    // // Client // //
    //    // Validation Xml
    //    | UpdateActiveSwateValidation nextActiveTableValid ->
    //        let nextState = {
    //            currentState with
    //                ActiveSwateValidation                   = nextActiveTableValid
    //                NextAnnotationTableForActiveValidation  = None
    //        }
    //        nextState, Cmd.none
    //    | UpdateNextAnnotationTableForActiveValidation nextAnnoTable ->
    //        let nextState = {
    //            currentState with
    //                NextAnnotationTableForActiveValidation = nextAnnoTable
    //        }
    //        nextState, Cmd.none
    //    | UpdateValidationXmls newValXmls ->
    //        let nextState = {
    //            currentState with
    //                ActiveSwateValidation                   = None
    //                NextAnnotationTableForActiveValidation  = None
    //                ValidationXmls                          = newValXmls
    //        }
    //        nextState, Cmd.none
    //    // Protocol group xml
    //    | UpdateProtocolGroupXmls newProtXmls ->
    //        let nextState = {
    //            currentState with
    //                ActiveProtocolGroup                     = None
    //                NextAnnotationTableForActiveProtGroup   = None
    //                ActiveProtocol                          = None
    //                NextAnnotationTableForActiveProtocol    = None
    //                ProtocolGroupXmls                       = newProtXmls
    //        }
    //        nextState, Cmd.none
    //    | UpdateActiveProtocolGroup nextActiveProtGroup ->
    //        let nextState= {
    //            currentState with
    //                ActiveProtocolGroup                     = nextActiveProtGroup
    //                NextAnnotationTableForActiveProtGroup   = None
    //        }
    //        nextState, Cmd.none
    //    | UpdateNextAnnotationTableForActiveProtGroup nextAnnoTable ->
    //        let nextState = {
    //            currentState with
    //                NextAnnotationTableForActiveProtGroup = nextAnnoTable
    //        }
    //        nextState, Cmd.none
    //    // Protocol xml
    //    | UpdateActiveProtocol protocol ->
    //        let nextState = {
    //            currentState with
    //                ActiveProtocol                          = protocol
    //                NextAnnotationTableForActiveProtocol    = None
    //        }
    //        nextState, Cmd.none
    //    | UpdateNextAnnotationTableForActiveProtocol nextAnnoTable ->
    //        let nextState = {
    //            currentState with
    //                NextAnnotationTableForActiveProtocol = nextAnnoTable
    //        }
    //        nextState, Cmd.none
        //
        | UpdateRawCustomXml rawXmlStr ->
            let nextState = {
                currentState with
                    RawXml      = rawXmlStr
                    NextRawXml  = rawXmlStr
            }
            nextState, Cmd.none
        | UpdateNextRawCustomXml nextRawCustomXml ->
            let nextState = {
                currentState with
                    NextRawXml = nextRawCustomXml
            }
            nextState, Cmd.none
    //    // OfficeInterop
    //    | GetAllValidationXmlParsedRequest ->
    //        let nextState = {
    //            currentState with
    //                ActiveSwateValidation                   = None
    //                NextAnnotationTableForActiveValidation  = None
    //        }
    //        let cmd =
    //            Cmd.OfPromise.either
    //                OfficeInterop.getAllValidationXmlParsed
    //                ()
    //                (GetAllValidationXmlParsedResponse >> SettingsXmlMsg)
    //                (GenericError >> Dev)
    //        nextState, cmd
    //    | GetAllValidationXmlParsedResponse (tableValidations, annoTables) ->
    //        let nextState = {
    //            currentState with
    //                FoundTables = annoTables
    //                ValidationXmls = tableValidations |> Array.ofList
    //        }
    //        let infoMsg = "Info", sprintf "Found %i checklist XML(s)." tableValidations.Length
    //        let infoCmd = GenericLog infoMsg |> Dev |> Cmd.ofMsg
    //        nextState, infoCmd
    //    | GetAllProtocolGroupXmlParsedRequest ->
    //        let nextState = {
    //            currentState with
    //                ActiveProtocolGroup                     = None
    //                NextAnnotationTableForActiveProtGroup   = None
    //                ActiveProtocol                          = None
    //                NextAnnotationTableForActiveProtocol    = None
    //        }
    //        let cmd =
    //            Cmd.OfPromise.either
    //                OfficeInterop.getAllProtocolGroupXmlParsed
    //                ()
    //                (GetAllProtocolGroupXmlParsedResponse >> SettingsXmlMsg)
    //                (GenericError >> Dev)
    //        nextState, cmd
    //    | GetAllProtocolGroupXmlParsedResponse (protocolGroupXmls, annoTables) ->
    //        let nextState = {
    //            currentState with
    //                FoundTables = annoTables
    //                ProtocolGroupXmls = protocolGroupXmls |> Array.ofList
    //        }
    //        let infoMsg = "Info", sprintf "Found %i protocol group XML(s)." protocolGroupXmls.Length
    //        let infoCmd = GenericLog infoMsg |> Dev |> Cmd.ofMsg
    //        nextState, infoCmd
    //    | RemoveCustomXmlRequest xmlType ->
    //        let cmd =
    //            Cmd.OfPromise.either
    //                OfficeInterop.removeXmlType
    //                xmlType
    //                (fun msg ->  matchXmlTypeToUpdateMsg msg xmlType)
    //                (GenericError >> Dev)
    //        currentState, cmd
    //    | ReassignCustomXmlRequest (prevXml,newXml) ->
    //        let cmd =
    //            Cmd.OfPromise.either
    //                OfficeInterop.updateAnnotationTableByXmlType
    //                (prevXml,newXml)
    //                // can use prevXml or newXml. Both are checked during 'updateAnnotationTableByXmlType' to be of the same kind
    //                (fun msg -> matchXmlTypeToUpdateMsg msg prevXml)
    //                (GenericError >> Dev)
    //        currentState, cmd

open Messages

let dangerZone (model:Model) dispatch =
    div [][
        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Text]]][str "Dangerzone"]
        Container.container [
            Container.Props [Style [
                Padding "1rem"
                Border (sprintf "2.5px solid %s" NFDIColors.Red.Base)
                BorderRadius "10px"
            ]]
        ][
            Button.a [
                Button.Color Color.IsDanger
                Button.IsFullWidth
                Button.OnClick (fun e -> OfficeInterop.DeleteAllCustomXml |> OfficeInteropMsg |> dispatch )
                Button.Props [Style []; Title "Be sure you know what you do. This cannot be undone!"]
            ] [
                Icon.icon [ ] [
                    Fa.i [Fa.Solid.ExclamationTriangle][]
                ]
                span [] [str "Delete All Custom Xml!"]
                Icon.icon [ ] [
                    Fa.i [Fa.Solid.ExclamationTriangle][]
                ]
            ]
        ]
    ]

let breadcrumbEle model dispatch =
    Breadcrumb.breadcrumb [Breadcrumb.HasArrowSeparator][
        Breadcrumb.item [][
            a [
                OnClick (fun e -> UpdatePageState (Some Routing.Route.Settings) |> dispatch)
            ][
                str (Routing.Route.Settings.toStringRdbl)
            ]
        ]
        Breadcrumb.item [ Breadcrumb.Item.IsActive true ][
            a [
                Style [Color model.SiteStyleState.ColorMode.Text]
                OnClick (fun e -> UpdatePageState (Some Routing.Route.SettingsXml) |> dispatch)
            ][
                str Routing.Route.SettingsXml.toStringRdbl
            ]
        ]
    ]

let showRawCustomXmlButton model dispatch =
    Field.div [][
        Button.a [
            Button.Color Color.IsInfo
            Button.IsFullWidth
            Button.OnClick (fun e -> OfficeInterop.GetSwateCustomXml |> OfficeInteropMsg |> dispatch )
            Button.Props [Title "Show Swate custom Xml"]
        ] [
            span [] [str "Load raw custom xml"]
        ]
    ]

let textAreaEle (model:Model) dispatch = 
    Columns.columns [Columns.IsMobile][
        Column.column [][
            Control.div [][
                Textarea.textarea [
                    Textarea.OnChange (fun e ->
                        SettingsXml.UpdateNextRawCustomXml (Some e.Value) |> SettingsXmlMsg |> dispatch
                    )
                    Textarea.DefaultValue (if model.SettingsXmlState.RawXml.IsSome then model.SettingsXmlState.RawXml.Value else "")
                    Textarea.ValueOrDefault model.SettingsXmlState.NextRawXml.Value
                ] [ ]
            ]
        ]
        Column.column [
            Column.Width (Screen.All,Column.IsNarrow)
        ][
            Field.div [][
                Button.a [
                    Button.Props [
                        Style [Width "40.5px"]
                        Title "Copy to Clipboard"
                    ]
                    Button.Color IsInfo
                    Button.OnClick (fun e ->
                        CustomComponents.ResponsiveFA.triggerResponsiveReturnEle "clipboard_customxmlSettings_rawXml"
                        let txt = model.SettingsXmlState.RawXml
                        let textArea = Browser.Dom.document.createElement "textarea"
                        textArea?value <- txt
                        textArea?style?top <- "0"
                        textArea?style?left <- "0"
                        textArea?style?position <- "fixed"

                        Browser.Dom.document.body.appendChild textArea |> ignore

                        textArea.focus()
                        /// Can't belive this actually worked
                        textArea?select()

                        let t = Browser.Dom.document.execCommand("copy")
                        Browser.Dom.document.body.removeChild(textArea) |> ignore
                        ()
                    )
                ][
                    CustomComponents.ResponsiveFA.responsiveReturnEle "clipboard_customxmlSettings_rawXml" Fa.Regular.Clipboard Fa.Solid.Check
                ]
            ]
            Field.div [][
                Button.a [
                    Button.IsStatic (model.SettingsXmlState.NextRawXml = model.SettingsXmlState.RawXml)
                    Button.Props [
                        Style [Width "40.5px"]
                        Title "Apply Changes"
                    ]
                    Button.Color IsWarning
                    Button.OnClick (fun e ->
                        let xmlEle = model.SettingsXmlState.NextRawXml.Value |> Fable.SimpleXml.SimpleXml.tryParseElementNonStrict
                        if xmlEle.IsSome then
                            let rmvWhiteSpace =
                                xmlEle.Value |> Fable.SimpleXml.Generator.ofXmlElement |> Fable.SimpleXml.Generator.serializeXml
                            let msg = OfficeInterop.Msg.UpdateSwateCustomXml rmvWhiteSpace |> OfficeInteropMsg
                            let modalBody = "Changes in this field could potentially invalidate your checklist and protocol xml. Please safe a copy before clicking 'Continue'."
                            let nM = {|ModalMessage = modalBody; NextMsg = msg|} |> Some
                            UpdateWarningModal nM |> dispatch
                        else
                            DevMsg.GenericError (System.Exception("Could not parse element to valid xml.")) |> Dev |> dispatch
                            
                    )
                ] [
                    Fa.i [
                        Fa.Solid.Pen
                    ] [] 
                ]
            ]
        ]
    ]

let showRawCustomXmlEle (model:Model) dispatch =
    mainFunctionContainer [
        Field.div [][
            Help.help [Help.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Justified)]][
                str "Here you can display all custom xml of your Swate table. This can help debug your Swate table and/or fix any problems occuring."
            ]
        ]

        Field.div [][
            Columns.columns [Columns.IsMobile][
                Column.column [][
                    showRawCustomXmlButton model dispatch
                ]
                if model.SettingsXmlState.RawXml.IsSome then
                    Column.column [Column.Width (Screen.All,Column.IsNarrow)][
                        Button.a [
                            Button.OnClick (fun e -> SettingsXml.UpdateRawCustomXml None |> SettingsXmlMsg |> dispatch)
                            Button.Color IsDanger
                            Button.Props [Title "Remove custom xml from the text area"]
                        ][
                            Fa.i [Fa.Solid.Times][]
                        ]
                    ]
            ]
        ]

        if model.SettingsXmlState.RawXml.IsSome then
            Field.div [][
                textAreaEle model dispatch
            ]
    ]


//// UP: Elements used to display raw custom xml
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
//// Down: Elements used to display validation/checklist xml


//let getValidationXmlButton (model:Model) dispatch =
//    Button.span [
//        Button.Color IsInfo
//        Button.IsFullWidth
//        Button.OnClick (fun e ->
//            GetAllValidationXmlParsedRequest |> SettingsXmlMsg |> dispatch
//        )
//    ][
//        str "Load checklist xml"
//    ]

//let removeValidationXmlButton (model:Model) dispatch =
//    Button.span [
//        Button.Color IsDanger
//        Button.OnClick (fun e ->
//            UpdateValidationXmls [||] |> SettingsXmlMsg |> dispatch
//        )
//    ][
//        Fa.i [Fa.Solid.Times][]
//    ]


//open OfficeInterop.Types.Xml

//let private tryFindInMap (tryFindMap:Map<string,string>) (toFind:Shared.AnnotationTable) =
//    let isValidKey = tryFindMap.ContainsKey toFind.Name
//    if isValidKey then
//        tryFindMap.[toFind.Name] = toFind.Worksheet
//    else false

//let applyChangesToTableValidationButton (model:Model) dispatch (tableValidation:ValidationTypes.TableValidation) isNextValidForWorkbook =
//    Button.a [
//        Button.Color IsWarning
//        Button.IsStatic (isNextValidForWorkbook |> not)
//        Button.OnClick (fun e ->
//            let prevat = tableValidation.AnnotationTable
//            let newat = model.SettingsXmlState.NextAnnotationTableForActiveValidation.Value
//            let prevXml = XmlTypes.ValidationType tableValidation
//            let newXml = XmlTypes.ValidationType {
//                tableValidation with
//                    AnnotationTable =
//                        AnnotationTable.create
//                            (if newat.Name = "" then prevat.Name else newat.Name)
//                            (if newat.Worksheet = "" then prevat.Worksheet else newat.Worksheet)
//            }
//            ReassignCustomXmlRequest (prevXml,newXml) |> SettingsXmlMsg |> dispatch
//        )
//    ][
//        text [if isNextValidForWorkbook then Style [Color "white"]] [str "Apply Changes"]
//    ]

//let removeTableValidationButton (model:Model) dispatch (tableValidation:ValidationTypes.TableValidation) =
//    Button.a [
//        Button.OnClick (fun e ->
//            let xmlType = XmlTypes.ValidationType tableValidation
//            let msg = RemoveCustomXmlRequest xmlType |> SettingsXmlMsg
//            let modalBody = "This function will remove the related checklist xml without chance of recovery. Please safe a copy before clicking 'Continue'."
//            let nM = {|ModalMessage = modalBody; NextMsg = msg|} |> Some
//            UpdateWarningModal nM |> dispatch
//        )
//        Button.Color IsDanger
//    ][
//        str "Remove"
//    ]

//let displaySingleTableValidationEle (model:Model) dispatch (tableValidation:ValidationTypes.TableValidation) tryFindMap =
//    let isValidForWorkBook = tryFindInMap tryFindMap tableValidation.AnnotationTable
//    let isActive = model.SettingsXmlState.ActiveSwateValidation.IsSome && model.SettingsXmlState.ActiveSwateValidation.Value = tableValidation
//    let isNextValidForWorkbook =
//        if isActive && model.SettingsXmlState.NextAnnotationTableForActiveValidation.IsSome then
//            let at = model.SettingsXmlState.NextAnnotationTableForActiveValidation.Value
//            match at.Name, at.Worksheet with
//            | "","" ->
//                let next = AnnotationTable.create tableValidation.AnnotationTable.Name tableValidation.AnnotationTable.Worksheet
//                tryFindInMap tryFindMap next
//            | "", newWorksheetName ->
//                let next = AnnotationTable.create tableValidation.AnnotationTable.Name newWorksheetName
//                tryFindInMap tryFindMap next
//            | newAnnotationTableName, "" ->
//                let next = AnnotationTable.create newAnnotationTableName tableValidation.AnnotationTable.Worksheet
//                tryFindInMap tryFindMap next
//            | newAnnotationTableName, newWorksheetName ->
//                let next = AnnotationTable.create newAnnotationTableName newWorksheetName
//                tryFindInMap tryFindMap next
//        else
//            false
//    Table.table [
//        Table.IsFullWidth
//        Table.IsBordered
//        Table.IsStriped
//    ][
//        thead [][
//            tr [
//                Style [Cursor "pointer"]
//                Class "hoverTableEle"
//                OnClick (fun e ->
//                    let next = if isActive then None else Some tableValidation
//                    UpdateActiveSwateValidation next |> SettingsXmlMsg |> dispatch
//                )
//            ][
//                th [
//                    Style [if not isValidForWorkBook then Color NFDIColors.Red.Base else Color NFDIColors.Mint.Base]
//                    Title ""
//                ][
//                    if isActive then
//                        Input.text [
//                            if isValidForWorkBook || isNextValidForWorkbook then Input.Color IsSuccess else Input.Color IsDanger
//                            Input.Props [
//                                OnClick (fun e -> e.stopPropagation())
//                            ]
//                            Input.DefaultValue tableValidation.AnnotationTable.Name
//                            Input.OnChange (fun e ->
//                                let nextAnnoT =
//                                    if model.SettingsXmlState.NextAnnotationTableForActiveValidation.IsSome then
//                                        { model.SettingsXmlState.NextAnnotationTableForActiveValidation.Value with Name = e.Value }
//                                    else AnnotationTable.create e.Value ""
//                                    |> Some
//                                UpdateNextAnnotationTableForActiveValidation nextAnnoT |> SettingsXmlMsg |> dispatch
//                            )
//                        ]
//                    else
//                        str tableValidation.AnnotationTable.Name
//                ]
//                th [
//                    Style [if not isValidForWorkBook then Color NFDIColors.Red.Base else Color NFDIColors.Mint.Base]
//                ][
//                    if isActive then
//                        Input.text [
//                            if isValidForWorkBook || isNextValidForWorkbook then Input.Color IsSuccess else Input.Color IsDanger
//                            Input.Props [
//                                OnClick (fun e -> e.stopPropagation())
//                            ]
//                            Input.DefaultValue tableValidation.AnnotationTable.Worksheet
//                            Input.OnChange (fun e ->
//                                let nextAnnoT =
//                                    if model.SettingsXmlState.NextAnnotationTableForActiveValidation.IsSome then
//                                        { model.SettingsXmlState.NextAnnotationTableForActiveValidation.Value with Worksheet = e.Value }
//                                    else AnnotationTable.create "" e.Value
//                                    |> Some
//                                UpdateNextAnnotationTableForActiveValidation nextAnnoT |> SettingsXmlMsg |> dispatch
//                            )
//                        ]
//                    else
//                        str tableValidation.AnnotationTable.Worksheet
//                ]
//                th [][str tableValidation.SwateVersion]
//                th [][str ( sprintf "%s %s" (tableValidation.DateTime.ToShortDateString()) (tableValidation.DateTime.ToShortTimeString()) )]
//            ]
//        ]
//        tbody [Style [
//            Transition "height 0.25s"
//            OverflowY OverflowOptions.Hidden
//            if not isActive then
//                Height "0px"
//                Display DisplayOptions.None
//        ]] [
//            for col in tableValidation.ColumnValidations do
//                let valFormat = if col.ValidationFormat.IsSome then col.ValidationFormat.Value.toReadableString else "-"
//                let importance = if col.Importance.IsSome then string col.Importance.Value else "-"
//                let colAdress = if col.ColumnAdress.IsSome then string col.ColumnAdress.Value else "-"
//                yield
//                    tr [][
//                        td [][str col.ColumnHeader]
//                        td [][str valFormat]
//                        td [][str importance]
//                        td [][str colAdress]
//                    ]
//            yield
//                tr [] [
//                    td [ColSpan 4][
//                        Level.level [Level.Level.IsMobile][
//                            Level.left [][
//                                Level.item [][
//                                    applyChangesToTableValidationButton model dispatch tableValidation isNextValidForWorkbook
//                                ]
//                            ]
//                            Level.right [][
//                                Level.item [][
//                                    removeTableValidationButton model dispatch tableValidation
//                                ]
//                            ]
//                        ]
//                    ]
//                ]
//        ]
//    ]

//let showValidationXmlEle (model:Model) dispatch =
//    let tryFindMap =
//        model.SettingsXmlState.FoundTables |> Array.map (fun x -> x.Name, x.Worksheet) |> Map.ofArray
        
//    div [
//        Style [
//            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
//            BorderRadius "15px 15px 0 0"
//            Padding "0.25rem 1rem"
//            MarginBottom "1rem"
//        ]
//    ][
//        Field.div [][
//            Help.help [Help.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Justified)]][
//                str "This block will display all checklist xml for this workbook. You can then remove single elements or assign
//                them to a new table-sheet combination. Should Swate find any information not related to an existing table-sheet
//                combination these will be marked in red."
//            ]
//        ]

//        Field.div [][
//            Columns.columns [Columns.IsMobile][
//                Column.column [][
//                    getValidationXmlButton model dispatch
//                ]
//                if model.SettingsXmlState.ValidationXmls |> Array.isEmpty |> not then
//                    Column.column [Column.Width (Screen.All,Column.IsNarrow)][
//                        removeValidationXmlButton model dispatch
//                    ]
//            ]
//        ]

//        if model.SettingsXmlState.ValidationXmls |> Array.isEmpty |> not then
//            Field.div [][
//                for validation in model.SettingsXmlState.ValidationXmls do
//                    yield
//                        displaySingleTableValidationEle model dispatch validation tryFindMap
//            ]
//    ]

//// UP: Elements used to display validation/checklist xml
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


let settingsXmlViewComponent (model:Model) dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle model dispatch

        Help.help [][str "The functions on this page allow direct manipulation of the Xml used to save additional information about your Swate table. Please use them with care."]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Display raw custom xml."]
        showRawCustomXmlEle model dispatch

        //Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Handle checklist xml."]
        //showValidationXmlEle model dispatch

        dangerZone model dispatch
    ]