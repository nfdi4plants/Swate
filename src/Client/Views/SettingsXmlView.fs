module SettingsXmlView

open Fulma
open Fable
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
//open Fable.Core.JS
open Fable.Core.JsInterop

open Shared

open Model
open Messages

let dangerZone model dispatch =
    div [][
        Label.label [][str "Dangerzone"]
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
                Button.OnClick (fun e -> DeleteAllCustomXml |> ExcelInterop |> dispatch )
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

let breadcrumbEle dispatch =
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
            Button.OnClick (fun e -> GetSwateCustomXml |> ExcelInterop |> dispatch )
            Button.Props [Title "Show record type data of Swate custom Xml"]
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
                        UpdateNextRawCustomXml e.Value |> SettingsXmlMsg |> dispatch
                    )
                    Textarea.DefaultValue model.SettingsXmlState.RawXml
                ][ ]
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
                    Button.IsStatic (model.SettingsXmlState.NextRawXml = "")
                    Button.Props [
                        Style [Width "40.5px"]
                        Title "Apply Changes"
                    ]
                    Button.Color IsWarning
                    Button.OnClick (fun e ->
                        let rmvWhiteSpace =
                            let xmlEle = model.SettingsXmlState.NextRawXml |> Fable.SimpleXml.SimpleXml.parseElementNonStrict
                            xmlEle
                            |> OfficeInterop.HelperFunctions.xmlElementToXmlString
                        let msg = ExcelInteropMsg.UpdateSwateCustomXml rmvWhiteSpace |> ExcelInterop
                        let modalBody = "Changes in this field could potentially invalidate your checklist and protocol xml. Please safe a copy before clicking 'Continue'."
                        let nM = {|ModalMessage = modalBody; NextMsg = msg|} |> Some
                        UpdateWarningModal nM |> dispatch
                    )
                ][
                    Fa.i [
                        Fa.Solid.Pen
                    ] [] 
                ]
            ]
        ]
    ]

let showRawCustomXmlEle (model:Model) dispatch =
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ][
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
                if model.SettingsXmlState.RawXml <> "" then
                    Column.column [Column.Width (Screen.All,Column.IsNarrow)][
                        Button.a [
                            Button.OnClick (fun e -> UpdateRawCustomXml "" |> SettingsXmlMsg |> dispatch)
                            Button.Color IsDanger
                            Button.Props [Title "Remove custom xml from the text area"]
                        ][
                            Fa.i [Fa.Solid.Times][]
                        ]
                    ]
            ]
        ]

        if model.SettingsXmlState.RawXml <> "" then
            Field.div [][
                textAreaEle model dispatch
            ]
    ]


// UP: Elements used to display raw custom xml
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Down: Elements used to display validation/checklist xml


let getValidationXmlButton (model:Model) dispatch =
    Button.span [
        Button.Color IsInfo
        Button.IsFullWidth
        Button.OnClick (fun e ->
            GetAllValidationXmlParsedRequest |> SettingsXmlMsg |> dispatch
        )
    ][
        str "Load checklist xml"
    ]

let removeValidationXmlButton (model:Model) dispatch =
    Button.span [
        Button.Color IsDanger
        Button.OnClick (fun e ->
            UpdateValidationXmls [||] |> SettingsXmlMsg |> dispatch
        )
    ][
        Fa.i [Fa.Solid.Times][]
    ]


open OfficeInterop.Types.Xml

let private tryFindInMap (tryFindMap:Map<string,string>) (toFind:Shared.AnnotationTable) =
    let isValidKey = tryFindMap.ContainsKey toFind.Name
    if isValidKey then
        tryFindMap.[toFind.Name] = toFind.Worksheet
    else false

let applyChangesToTableValidationButton (model:Model) dispatch (tableValidation:ValidationTypes.TableValidation) isNextValidForWorkbook =
    Button.a [
        Button.Color IsWarning
        Button.IsStatic (isNextValidForWorkbook |> not)
        Button.OnClick (fun e ->
            let prevat = tableValidation.AnnotationTable
            let newat = model.SettingsXmlState.NextAnnotationTableForActiveValidation.Value
            let prevXml = XmlTypes.ValidationType tableValidation
            let newXml = XmlTypes.ValidationType {
                tableValidation with
                    AnnotationTable =
                        AnnotationTable.create
                            (if newat.Name = "" then prevat.Name else newat.Name)
                            (if newat.Worksheet = "" then prevat.Worksheet else newat.Worksheet)
            }
            ReassignCustomXmlRequest (prevXml,newXml) |> SettingsXmlMsg |> dispatch
        )
    ][
        text [if isNextValidForWorkbook then Style [Color "white"]] [str "Apply Changes"]
    ]

let removeTableValidationButton (model:Model) dispatch (tableValidation:ValidationTypes.TableValidation) =
    Button.a [
        Button.OnClick (fun e ->
            let xmlType = XmlTypes.ValidationType tableValidation
            let msg = RemoveCustomXmlRequest xmlType |> SettingsXmlMsg
            let modalBody = "This function will remove the related checklist xml without chance of recovery. Please safe a copy before clicking 'Continue'."
            let nM = {|ModalMessage = modalBody; NextMsg = msg|} |> Some
            UpdateWarningModal nM |> dispatch
        )
        Button.Color IsDanger
    ][
        str "Remove"
    ]

let displaySingleTableValidationEle (model:Model) dispatch (tableValidation:ValidationTypes.TableValidation) tryFindMap =
    let isValidForWorkBook = tryFindInMap tryFindMap tableValidation.AnnotationTable
    let isActive = model.SettingsXmlState.ActiveSwateValidation.IsSome && model.SettingsXmlState.ActiveSwateValidation.Value = tableValidation
    let isNextValidForWorkbook =
        if isActive && model.SettingsXmlState.NextAnnotationTableForActiveValidation.IsSome then
            let at = model.SettingsXmlState.NextAnnotationTableForActiveValidation.Value
            match at.Name, at.Worksheet with
            | "","" ->
                let next = AnnotationTable.create tableValidation.AnnotationTable.Name tableValidation.AnnotationTable.Worksheet
                tryFindInMap tryFindMap next
            | "", newWorksheetName ->
                let next = AnnotationTable.create tableValidation.AnnotationTable.Name newWorksheetName
                tryFindInMap tryFindMap next
            | newAnnotationTableName, "" ->
                let next = AnnotationTable.create newAnnotationTableName tableValidation.AnnotationTable.Worksheet
                tryFindInMap tryFindMap next
            | newAnnotationTableName, newWorksheetName ->
                let next = AnnotationTable.create newAnnotationTableName newWorksheetName
                tryFindInMap tryFindMap next
        else
            false
    Table.table [
        Table.IsFullWidth
        Table.IsBordered
        Table.IsStriped
    ][
        thead [][
            tr [
                Style [Cursor "pointer"]
                Class "hoverTableEle"
                OnClick (fun e ->
                    let next = if isActive then None else Some tableValidation
                    UpdateActiveSwateValidation next |> SettingsXmlMsg |> dispatch
                )
            ][
                th [
                    Style [if not isValidForWorkBook then Color NFDIColors.Red.Base else Color NFDIColors.Mint.Base]
                    Title ""
                ][
                    if isActive then
                        Input.text [
                            if isValidForWorkBook || isNextValidForWorkbook then Input.Color IsSuccess else Input.Color IsDanger
                            Input.Props [
                                OnClick (fun e -> e.stopPropagation())
                            ]
                            Input.DefaultValue tableValidation.AnnotationTable.Name
                            Input.OnChange (fun e ->
                                let nextAnnoT =
                                    if model.SettingsXmlState.NextAnnotationTableForActiveValidation.IsSome then
                                        { model.SettingsXmlState.NextAnnotationTableForActiveValidation.Value with Name = e.Value }
                                    else AnnotationTable.create e.Value ""
                                    |> Some
                                UpdateNextAnnotationTableForActiveValidation nextAnnoT |> SettingsXmlMsg |> dispatch
                            )
                        ]
                    else
                        str tableValidation.AnnotationTable.Name
                ]
                th [
                    Style [if not isValidForWorkBook then Color NFDIColors.Red.Base else Color NFDIColors.Mint.Base]
                ][
                    if isActive then
                        Input.text [
                            if isValidForWorkBook || isNextValidForWorkbook then Input.Color IsSuccess else Input.Color IsDanger
                            Input.Props [
                                OnClick (fun e -> e.stopPropagation())
                            ]
                            Input.DefaultValue tableValidation.AnnotationTable.Worksheet
                            Input.OnChange (fun e ->
                                let nextAnnoT =
                                    if model.SettingsXmlState.NextAnnotationTableForActiveValidation.IsSome then
                                        { model.SettingsXmlState.NextAnnotationTableForActiveValidation.Value with Worksheet = e.Value }
                                    else AnnotationTable.create "" e.Value
                                    |> Some
                                UpdateNextAnnotationTableForActiveValidation nextAnnoT |> SettingsXmlMsg |> dispatch
                            )
                        ]
                    else
                        str tableValidation.AnnotationTable.Worksheet
                ]
                th [][str tableValidation.SwateVersion]
                th [][str ( sprintf "%s %s" (tableValidation.DateTime.ToShortDateString()) (tableValidation.DateTime.ToShortTimeString()) )]
            ]
        ]
        tbody [Style [
            Transition "height 0.25s"
            OverflowY OverflowOptions.Hidden
            if not isActive then
                Height "0px"
                Display DisplayOptions.None
        ]] [
            for col in tableValidation.ColumnValidations do
                let valFormat = if col.ValidationFormat.IsSome then col.ValidationFormat.Value.toReadableString else "-"
                let importance = if col.Importance.IsSome then string col.Importance.Value else "-"
                let colAdress = if col.ColumnAdress.IsSome then string col.ColumnAdress.Value else "-"
                yield
                    tr [][
                        td [][str col.ColumnHeader]
                        td [][str valFormat]
                        td [][str importance]
                        td [][str colAdress]
                    ]
            yield
                tr [] [
                    td [ColSpan 4][
                        Level.level [Level.Level.IsMobile][
                            Level.left [][
                                Level.item [][
                                    applyChangesToTableValidationButton model dispatch tableValidation isNextValidForWorkbook
                                ]
                            ]
                            Level.right [][
                                Level.item [][
                                    removeTableValidationButton model dispatch tableValidation
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

let showValidationXmlEle (model:Model) dispatch =
    let tryFindMap =
        model.SettingsXmlState.FoundTables |> Array.map (fun x -> x.Name, x.Worksheet) |> Map.ofArray
        
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            //BorderRadius "15px 15px 0 0"
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ][
        Field.div [][
            Help.help [Help.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Justified)]][
                str "This block will display all checklist xml for this workbook. You can then remove single elements or assign
                them to a new table-sheet combination. Should Swate find any information not related to an existing table-sheet
                combination these will be marked in red."
            ]
        ]

        Field.div [][
            Columns.columns [Columns.IsMobile][
                Column.column [][
                    getValidationXmlButton model dispatch
                ]
                if model.SettingsXmlState.ValidationXmls |> Array.isEmpty |> not then
                    Column.column [Column.Width (Screen.All,Column.IsNarrow)][
                        removeValidationXmlButton model dispatch
                    ]
            ]
        ]

        if model.SettingsXmlState.ValidationXmls |> Array.isEmpty |> not then
            Field.div [][
                for validation in model.SettingsXmlState.ValidationXmls do
                    yield
                        displaySingleTableValidationEle model dispatch validation tryFindMap
            ]
    ]

// UP: Elements used to display validation/checklist xml
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Down: Elements used to display protocol(-group) xml

let getProtocolGroupXmlButton (model:Model) dispatch =
    Button.span [
        Button.Color IsInfo
        Button.IsFullWidth
        Button.OnClick (fun e ->
            GetAllProtocolGroupXmlParsedRequest |> SettingsXmlMsg |> dispatch
        )
    ][
        str "Load protocol group xml"
    ]

let removeProtocolGroupXmlButton (model:Model) dispatch =
    Button.span [
        Button.Color IsDanger
        Button.OnClick (fun e ->
            UpdateProtocolGroupXmls [||] |> SettingsXmlMsg |> dispatch
        )
    ][
        Fa.i [Fa.Solid.Times][]
    ]

let applyChangesToProtocolGroupButton (model:Model) dispatch (protGroup:GroupTypes.ProtocolGroup) isNextValidForWorkbook =
    Button.a [
        Button.Color IsWarning
        Button.IsStatic (isNextValidForWorkbook |> not)
        Button.OnClick (fun e ->
            let prevat = protGroup.AnnotationTable
            let newat = model.SettingsXmlState.NextAnnotationTableForActiveProtGroup.Value
            let prevXml = XmlTypes.GroupType protGroup
            let newName = if newat.Name = "" then prevat.Name else newat.Name
            let newWorksheet = if newat.Worksheet = "" then prevat.Worksheet else newat.Worksheet
            let newXml = XmlTypes.GroupType {
                protGroup with
                    AnnotationTable = AnnotationTable.create newName newWorksheet
                    Protocols = protGroup.Protocols |> List.map (fun x -> {x with AnnotationTable = AnnotationTable.create newName newWorksheet})
            }
            ReassignCustomXmlRequest (prevXml,newXml) |> SettingsXmlMsg |> dispatch
        )
    ][
        text [if isNextValidForWorkbook then Style [Color "white"]] [str "Apply Changes"]
    ]

let removeProtocolGroupButton (model:Model) dispatch (protGroup:GroupTypes.ProtocolGroup) =
    Button.a [
        Button.OnClick (fun e ->
            let xmlType = XmlTypes.GroupType protGroup
            let msg = RemoveCustomXmlRequest xmlType |> SettingsXmlMsg
            let modalBody = "This function will remove the related protocol xml without chance of recovery. Please safe a copy before clicking 'Continue'."
            let nM = {|ModalMessage = modalBody; NextMsg = msg|} |> Some
            UpdateWarningModal nM |> dispatch
        )
        Button.Color IsDanger
    ][
        str "Remove"
    ]

let ativeRowBorder = BorderLeft (sprintf "3px solid %s" NFDIColors.LightBlue.Base)

let protocolChildList (protocol:GroupTypes.Protocol) isActive model dispatch =
    [
        for bb in protocol.SpannedBuildingBlocks do
            yield
                tr [if not isActive then Style [Display DisplayOptions.None]][
                    td [ColSpan 2; Style [ativeRowBorder] ] [ str bb.ColumnName ]
                    td [ColSpan 2 ]                         [ str bb.TermAccession ]
                    td [ ]                                  [ ]
                ]
        yield
            tr [if not isActive then Style [Display DisplayOptions.None]][
                td [ColSpan 5; Style [ativeRowBorder] ][
                    Level.level [Level.Level.IsMobile][
                        Level.left [][
                            Level.item [][]
                        ]
                        Level.right [][
                            Level.item [][
                                Button.a [
                                    Button.Color IsDanger
                                    Button.OnClick (fun e ->
                                        let xml = XmlTypes.ProtocolType protocol
                                        let msg = RemoveCustomXmlRequest xml |> SettingsXmlMsg
                                        let modalBody = "This function will remove the related protocol xml without chance of recovery. Please safe a copy before clicking 'Continue'."
                                        let nM = {|ModalMessage = modalBody; NextMsg = msg|} |> Some
                                        UpdateWarningModal nM |> dispatch
                                    )
                                ][
                                    str "Remove"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
    ]

let displaySingleProtocolGroupEle model dispatch (protocolGroup:GroupTypes.ProtocolGroup) tryFindMap =
    let isValidForWorkBook = tryFindInMap tryFindMap protocolGroup.AnnotationTable
    let isActive = model.SettingsXmlState.ActiveProtocolGroup.IsSome && model.SettingsXmlState.ActiveProtocolGroup.Value = protocolGroup
    let isNextValidForWorkbook =
        if isActive && model.SettingsXmlState.NextAnnotationTableForActiveProtGroup.IsSome then
            let at = model.SettingsXmlState.NextAnnotationTableForActiveProtGroup.Value
            match at.Name, at.Worksheet with
            | "","" ->
                let next = AnnotationTable.create protocolGroup.AnnotationTable.Name protocolGroup.AnnotationTable.Worksheet
                tryFindInMap tryFindMap next
            | "", newWorksheetName ->
                let next = AnnotationTable.create protocolGroup.AnnotationTable.Name newWorksheetName
                tryFindInMap tryFindMap next
            | newAnnotationTableName, "" ->
                let next = AnnotationTable.create newAnnotationTableName protocolGroup.AnnotationTable.Worksheet
                tryFindInMap tryFindMap next
            | newAnnotationTableName, newWorksheetName ->
                let next = AnnotationTable.create newAnnotationTableName newWorksheetName
                tryFindInMap tryFindMap next
        else
            false

    Table.table [
        Table.IsFullWidth
        Table.IsBordered
        Table.IsStriped
    ][
        thead [][
            tr [
                Style [Cursor "pointer"]
                Class "hoverTableEle"
                OnClick (fun e ->
                    let next = if isActive then None else Some protocolGroup
                    UpdateActiveProtocolGroup next |> SettingsXmlMsg |> dispatch
                )
            ][
                th [
                    Style [if not isValidForWorkBook then Color NFDIColors.Red.Base else Color NFDIColors.Mint.Base]
                    Title ""
                ][
                    if isActive && model.SettingsXmlState.ActiveProtocol.IsNone then
                        Input.text [
                            if isValidForWorkBook || isNextValidForWorkbook then Input.Color IsSuccess else Input.Color IsDanger
                            Input.Props [
                                OnClick (fun e -> e.stopPropagation())
                            ]
                            Input.DefaultValue protocolGroup.AnnotationTable.Name
                            Input.OnChange (fun e ->
                                let nextAnnoT =
                                    if model.SettingsXmlState.NextAnnotationTableForActiveProtGroup.IsSome then
                                        { model.SettingsXmlState.NextAnnotationTableForActiveProtGroup.Value with Name = e.Value }
                                    else AnnotationTable.create e.Value ""
                                    |> Some
                                UpdateNextAnnotationTableForActiveProtGroup nextAnnoT |> SettingsXmlMsg |> dispatch
                            )
                        ]
                    else
                        str protocolGroup.AnnotationTable.Name
                ]
                th [
                    Style [if not isValidForWorkBook then Color NFDIColors.Red.Base else Color NFDIColors.Mint.Base]
                ][
                    if isActive && model.SettingsXmlState.ActiveProtocol.IsNone then
                        Input.text [
                            if isValidForWorkBook || isNextValidForWorkbook then Input.Color IsSuccess else Input.Color IsDanger
                            Input.Props [
                                OnClick (fun e -> e.stopPropagation())
                            ]
                            Input.DefaultValue protocolGroup.AnnotationTable.Worksheet
                            Input.OnChange (fun e ->
                                let nextAnnoT =
                                    if model.SettingsXmlState.NextAnnotationTableForActiveProtGroup.IsSome then
                                        { model.SettingsXmlState.NextAnnotationTableForActiveProtGroup.Value with Worksheet = e.Value }
                                    else AnnotationTable.create "" e.Value
                                    |> Some
                                UpdateNextAnnotationTableForActiveProtGroup nextAnnoT |> SettingsXmlMsg |> dispatch
                            )
                        ]
                    else
                        str protocolGroup.AnnotationTable.Worksheet
                ]
                th [][str protocolGroup.SwateVersion]
                th [ColSpan 2] []
            ]
        ]
        tbody [Style [
            if not isActive then
                Display DisplayOptions.None
        ]] [
            for protocol in protocolGroup.Protocols do
                let isActiveProt = model.SettingsXmlState.ActiveProtocol.IsSome && model.SettingsXmlState.ActiveProtocol.Value = protocol
                yield
                    tr [][
                        td [if isActiveProt then Style [ativeRowBorder] ][str protocol.Id]
                        td [][str protocol.AnnotationTable.Name]
                        td [][str protocol.AnnotationTable.Worksheet]
                        td [][str protocol.SwateVersion]
                        td [
                            Style [
                                Cursor "Pointer"; Padding "0"
                            ]
                        ][
                            Button.a [
                                Button.Color IsInfo
                                Button.OnClick (fun e ->
                                    let nextProtocol = if isActiveProt then None else Some protocol
                                    UpdateActiveProtocol nextProtocol |> SettingsXmlMsg |> dispatch
                                )
                                Button.IsOutlined
                                Button.IsFullWidth
                                Button.Props [Style [BorderRadius "0"]]
                            ][
                                Fa.i [
                                    Fa.Props [Style [Transition "transform 0.4s"]]
                                    if isActiveProt then Fa.Rotate180
                                    Fa.Solid.AngleDown
                                ][]
                            ]
                        ]
                    ]
                yield! protocolChildList protocol isActiveProt model dispatch
            if model.SettingsXmlState.ActiveProtocol.IsNone then
                yield
                    tr [] [
                        td [ColSpan 5][
                            Level.level [Level.Level.IsMobile][
                                Level.left [][
                                    Level.item [][
                                        applyChangesToProtocolGroupButton model dispatch protocolGroup isNextValidForWorkbook
                                    ]
                                ]
                                Level.right [][
                                    Level.item [][
                                        removeProtocolGroupButton model dispatch protocolGroup
                                    ]
                                ]
                            ]
                        ]
                    ]
        ]
    ]

let showProtocolGroupXmlEle (model:Model) dispatch =
    let tryFindMap =
        model.SettingsXmlState.FoundTables |> Array.map (fun x -> x.Name, x.Worksheet) |> Map.ofArray
        
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ][
        Field.div [][
            Help.help [Help.Modifiers [Modifier.TextAlignment (Screen.All,TextAlignment.Justified)]][
                str "This block will display all protocol xml for this workbook. You can then remove single elements or assign
                them to a new table-sheet combination. Should Swate find any information not related to an existing table-sheet
                combination these will be marked in red."
            ]
        ]

        Field.div [][
            Columns.columns [Columns.IsMobile][
                Column.column [][
                    getProtocolGroupXmlButton model dispatch
                ]
                if model.SettingsXmlState.ProtocolGroupXmls |> Array.isEmpty |> not then
                    Column.column [Column.Width (Screen.All,Column.IsNarrow)][
                        removeProtocolGroupXmlButton model dispatch
                    ]
            ]
        ]

        if model.SettingsXmlState.ProtocolGroupXmls |> Array.isEmpty |> not then
            Field.div [][
                for protGroup in model.SettingsXmlState.ProtocolGroupXmls do
                    yield
                        displaySingleProtocolGroupEle model dispatch protGroup tryFindMap
            ]
    ]

let settingsXmlViewComponent (model:Model) dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        breadcrumbEle dispatch

        Help.help [][str "The functions on this page allow more or less direct manipulation of the Xml used to save additional information about your Swate table. Please use them with care."]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Display raw custom xml."]
        showRawCustomXmlEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Handle checklist xml."]
        showValidationXmlEle model dispatch

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Handle protocol group xml."]
        showProtocolGroupXmlEle model dispatch

        dangerZone model dispatch
    ]