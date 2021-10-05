module Validation

open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Fable.Core.JsInterop
open Browser
open Browser.MediaQueryList
open Browser.MediaQueryListExtensions
open Elmish

open Shared

open ExcelColors
open Model
open Messages

open CustomComponents

open OfficeInterop.CustomXmlTypes.Validation
open Shared.OfficeInteropTypes
open Validation

let update (validationMsg:Validation.Msg) (currentState: Validation.Model) : Validation.Model * Cmd<Messages.Msg> =
    match validationMsg with
    /// This message gets its values from ExcelInteropMsg.GetTableRepresentation.
    /// It is used to update ValidationState.TableRepresentation and to transform the new information to ValidationState.TableValidationScheme.
    | StoreTableRepresentationFromOfficeInterop (tableValidation:OfficeInterop.CustomXmlTypes.Validation.TableValidation, buildingBlocks:BuildingBlockTypes.BuildingBlock []) ->
        let nextState = {
            currentState with
                ActiveTableBuildingBlocks = buildingBlocks
                TableValidationScheme = tableValidation
        }
        nextState, Cmd.none

    | UpdateDisplayedOptionsId intOpt ->
        let nextState = {
            currentState with
                DisplayedOptionsId = intOpt
        }
        nextState, Cmd.none
    | UpdateTableValidationScheme tableValidation ->
        let nextState = {
            currentState with
                TableValidationScheme   = tableValidation
        }
        nextState, Cmd.none

open Messages

let columnListElement ind (columnValidation:ColumnValidation) (model:Model) dispatch =
    let isActive =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            true
        | _ ->
            false
    tr [
        /// Remove validationTableEle when active to remove on-hover color change to really light grey.
        if isActive then
            Class "nonSelectText"
        else
            Class "nonSelectText hoverTableEle"
        Style [
            Cursor "pointer"
            UserSelect UserSelectOptions.None
            if isActive then
                BackgroundColor NFDIColors.Mint.Darker10
            if isActive then
                Color "white"
            else
                Color model.SiteStyleState.ColorMode.Text
        ]
        OnClick (fun e ->
            e.preventDefault()
            if isActive then
                UpdateDisplayedOptionsId None |> ValidationMsg |> dispatch
            else
                UpdateDisplayedOptionsId (Some ind) |> ValidationMsg |> dispatch
        )
    ][
        td [][str columnValidation.ColumnHeader.SwateColumnHeader]
        td [][
            if columnValidation.Importance.IsSome then
                str (string columnValidation.Importance)
            else
                str "-"
        ]
        td [][
            if columnValidation.ValidationFormat.IsSome then
                str columnValidation.ValidationFormat.Value.toReadableString
            else
                str "-"
        ]
        td [][
            Icon.icon [][
                Fa.i [Fa.Solid.ChevronDown][]
            ]
        ]
    ]

let updateTableValidationByColValidation (model:Model) (updatedColValidation:ColumnValidation) =
    {
        model.ValidationState.TableValidationScheme with
            ColumnValidations =
                model.ValidationState.TableValidationScheme.ColumnValidations
                |> List.filter (fun x -> x.ColumnHeader <> updatedColValidation.ColumnHeader)
                |> fun filteredList -> updatedColValidation::filteredList
                |> List.sortBy (fun colVal -> colVal.ColumnAdress)
    }

let checkradioElement (id:int) (contentTypeOpt:ContentType option) (columnValidation:ColumnValidation) (model:Model) dispatch =
    let contentType = if contentTypeOpt.IsSome then contentTypeOpt.Value.toReadableString else "None"
    let isDisabled = (contentType = "Ontology [None]" || contentType = "Unit [None]")
    div [Style [Position PositionOptions.Relative]] [
        input [
            Type "checkbox";
            Class "checkbox-input"
            Id (sprintf "checkradio%i%s" id contentType)
            Name (sprintf "ContentType%i" id)
            Disabled isDisabled
            OnChange (fun e ->
                let nextColumnValidation = {
                    columnValidation with
                        ValidationFormat = contentTypeOpt
                }
                let nextTableValidation =
                    updateTableValidationByColValidation model nextColumnValidation
                UpdateTableValidationScheme nextTableValidation |> ValidationMsg |> dispatch
            )
            Checked (contentTypeOpt = columnValidation.ValidationFormat)

        ]
        label [
            Class "checkbox-label"
            HtmlFor (sprintf "checkradio%i%s" id contentType)
        ][str contentType]
        label [
            Class "checkbox-checkmark";
            HtmlFor (sprintf "checkradio%i%s" id contentType)
        ][]
    ]


let findTerm (columnValidation:ColumnValidation) (buildingBlocks:BuildingBlockTypes.BuildingBlock []) =
    buildingBlocks
    |> Array.find (fun x -> x.MainColumn.Header = columnValidation.ColumnHeader)

let checkradioList (ind:int) colVal model dispatch =
    let term = findTerm colVal model.ValidationState.ActiveTableBuildingBlocks
    let unitContent =
        if term.hasUnit then ContentType.UnitTerm term.Unit.Value.Cells.[0].Unit.Value.Name |> Some else ContentType.UnitTerm "None" |> Some

    let ontologyContent = 
        if term.hasTerm then ContentType.OntologyTerm term.MainColumn.Header.tryGetOntologyTerm.Value |> Some else ContentType.OntologyTerm "None" |> Some

    [
        checkradioElement ind   None                        colVal model dispatch
        
        checkradioElement ind   (Some ContentType.Number)   colVal model dispatch
        checkradioElement ind   (Some ContentType.Int)      colVal model dispatch
        checkradioElement ind   (Some ContentType.Text)     colVal model dispatch
        checkradioElement ind   (Some ContentType.Url)      colVal model dispatch

        checkradioElement ind   ontologyContent             colVal model dispatch
        checkradioElement ind   unitContent                 colVal model dispatch
    ]


let sliderElements id columnValidation model dispatch =
    div [][
        Field.div [Field.HasAddons][
            for i in 1 .. 5 do
                yield
                    Control.div [][
                        Button.a [
                            Button.Color IsWarning
                            Button.IsOutlined
                            Button.Props [Style [Padding "0rem"; BorderColor model.SiteStyleState.ColorMode.BodyForeground]]
                            Button.OnClick (fun e ->
                                let nextColumnValidation = {
                                    columnValidation with
                                        Importance = i |> Some
                                }
                                let nextTableValidation =
                                    updateTableValidationByColValidation model nextColumnValidation
                                UpdateTableValidationScheme nextTableValidation |> ValidationMsg |> dispatch
                                )
                        ][
                            Fa.span [
                                Fa.Size Fa.FaLarge
                                if columnValidation.Importance.IsSome && columnValidation.Importance.Value >= i then
                                    Fa.Solid.Star
                                else
                                    Fa.Regular.Star
                                //Fa.Props [Style [Color NFDIColors.Yellow.Base]]
                            ][]
                        ]
                    ]
            yield
                Button.a [
                    Button.Color IsDanger
                    Button.Props [Style [BorderColor model.SiteStyleState.ColorMode.BodyForeground]]
                    Button.IsOutlined
                    Button.OnClick (fun e ->
                        let nextColumnValidation = {
                            columnValidation with
                                Importance = None
                        }
                        let nextTableValidation =
                            updateTableValidationByColValidation model nextColumnValidation
                        UpdateTableValidationScheme nextTableValidation |> ValidationMsg |> dispatch
                        )
                ][
                    Fa.span [
                        Fa.Size Fa.FaLarge
                        Fa.Solid.Backspace
                    ][]
                ]
        ]
    ]

let optionsElement ind (columnValidation:ColumnValidation) (model:Model) dispatch =
    let isVisible =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            true
        | _ ->
            false
    tr [][
        td [
            ColSpan 4
            Style [
                Padding "0";
                if isVisible then BorderBottom (sprintf "2px solid %s" NFDIColors.Mint.Base)
            ]
        ][
            Box.box' [
                Props [
                    Style [
                        Display (if isVisible then DisplayOptions.Block else DisplayOptions.None)
                        Width "100%"
                        BorderRadius "0px"
                        BackgroundColor model.SiteStyleState.ColorMode.BodyForeground
                        Color model.SiteStyleState.ColorMode.Text
                    ]
                ]
            ][
                Columns.columns [][
                    Column.column [][
                        b [][str "Content Type"]

                        Help.help [Help.Props [Style [MarginBottom "1rem"]]][str "Select the specific type of content for the selected column."]
                    
                        yield! checkradioList ind columnValidation model dispatch

                    ]
                    Column.column [][
                        b [][str "Importance"]

                        Help.help [][str "Define how important it is to fill in the column correctly."]

                        sliderElements ind columnValidation model dispatch
                    ]
                ]
            ]
        ]
    ]

let validationElements (model:Model) dispatch =
    div [
        Style [
            BorderLeft (sprintf "5px solid %s" NFDIColors.Mint.Base)
            //BorderRadius "15px 15px 0 0"
            Padding "0.25rem 1rem"
            MarginBottom "1rem"
        ]
    ][
        Field.div [Field.Props [Style [
            Width "100%"
        ]]] [
            // annotationTable name - DateTime of saving
            div [
                Id "TableRepresentationInfoHeader"
                OnTransitionEnd (fun e ->
                    let header = Browser.Dom.document.getElementById("TableRepresentationInfoHeader")
                    header?style?opacity <- 1
                    header?style?transition <- "unset"
                )
            ][
                str model.ValidationState.TableValidationScheme.AnnotationTable
                str " - "
                str ( model.ValidationState.TableValidationScheme.DateTime.ToString("yyyy-MM-dd HH:mm") )
                str " - "
                str (
                    sprintf "Swate %s" (
                        if model.ValidationState.TableValidationScheme.SwateVersion = "" then
                            model.PersistentStorageState.AppVersion
                        else
                            model.ValidationState.TableValidationScheme.SwateVersion
                    )
                )
            ]
            Table.table [
                Table.Props [Style [BackgroundColor model.SiteStyleState.ColorMode.BodyBackground]]
                Table.IsHoverable; Table.IsFullWidth
            ] [
                thead [ ] [
                    tr [ ] [
                        th [ Style [Color model.SiteStyleState.ColorMode.Text] ] [ str "Column Header" ]
                        th [ Style [Color model.SiteStyleState.ColorMode.Text] ] [ str "Importance" ]
                        th [ Style [Color model.SiteStyleState.ColorMode.Text] ] [ str "Content Type" ]
                        th [ Style [Color model.SiteStyleState.ColorMode.Text] ] [ ]
                    ]
                ]
                tbody [ ] [
                    for i in 0 .. model.ValidationState.TableValidationScheme.ColumnValidations.Length-1 do
                        let colVal = model.ValidationState.TableValidationScheme.ColumnValidations.[i]
                        yield! [
                            columnListElement i colVal model dispatch
                            optionsElement i colVal model dispatch
                        ]
                ]
            ]

            /// Show warning if no validation format was found
            if model.ValidationState.TableValidationScheme.SwateVersion = "" then
                Label.label [Label.Size Size.IsSmall; Label.Props [Style [Color NFDIColors.Red.Lighter10]]][
                    str """No checklist for this table found! Hit "Add checklist to workbook" to add a checklist for the active annotation table."""
                ]

            // Submit new checklist scheme. This will write custom xml into the workbook.
            Button.a [
                Button.Color Color.IsSuccess
                Button.IsFullWidth
                Button.OnClick (fun e ->
                    let header = Browser.Dom.document.getElementById("TableRepresentationInfoHeader")
                    header?style?transition <- "0.3s ease"
                    header?style?opacity <- 0
                    OfficeInterop.WriteTableValidationToXml (model.ValidationState.TableValidationScheme, model.PersistentStorageState.AppVersion) |> OfficeInteropMsg |> dispatch
                )
                Button.Props [Tooltip.dataTooltip "Write checklist info to excel worksheet."]
                Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            ] [
                str "Add checklist to workbook"
            ]
        ]
    ]

let validationComponent model dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [ str "Checklist Editor"]

        Help.help [][
            str "Display a table representation and add information to later validate values in the table according to their respective column."
        ]

        Button.a [
            Button.Color Color.IsInfo
            Button.IsFullWidth
            Button.OnClick (fun e -> OfficeInterop.GetTableValidationXml |> OfficeInteropMsg |> dispatch )
            Button.CustomClass (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
            Button.Props [Style [Margin "1rem 0"]; Tooltip.dataTooltip "Get checklist info for currently shown annotation table."]
        ] [
            str "Update table representation"
        ]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [
            str """Adjust current Swate table checklist. """
            span [
                Class (Tooltip.ClassName + " " + Tooltip.IsTooltipBottom + " " + Tooltip.IsMultiline)
                Tooltip.dataTooltip """When hitting "Add checklist to workbook" this information will be saved as part of the workbook."""
                Style [Color NFDIColors.LightBlue.Base; MarginLeft ".5rem"]
            ][
                Fa.i [ Fa.Solid.InfoCircle ][]
            ]
        ]

        validationElements model dispatch
            
    ]