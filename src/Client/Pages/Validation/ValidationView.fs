module Validation

open Fable.Core.JsInterop
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
    // This message gets its values from ExcelInteropMsg.GetTableRepresentation.
    // It is used to update ValidationState.TableRepresentation and to transform the new information to ValidationState.TableValidationScheme.
    | StoreTableRepresentationFromOfficeInterop (tableValidation:OfficeInterop.CustomXmlTypes.Validation.TableValidation, buildingBlocks:BuildingBlock []) ->
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

open Fable
open Fable.React
open Fable.React.Props
open Feliz
open Feliz.Bulma

let columnListElement ind (columnValidation:ColumnValidation) (model:Model) dispatch =
    let isActive =
        match model.ValidationState.DisplayedOptionsId with
        | Some id when id = ind ->
            true
        | _ ->
            false
    Standard.tr [
        // Remove validationTableEle when active to remove on-hover color change to really light grey.
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
    ] [
        td [] [str columnValidation.ColumnHeader.SwateColumnHeader]
        td [] [
            if columnValidation.Importance.IsSome then
                str (string columnValidation.Importance)
            else
                str "-"
        ]
        td [] [
            if columnValidation.ValidationFormat.IsSome then
                str columnValidation.ValidationFormat.Value.toReadableString
            else
                str "-"
        ]
        td [] [
            Bulma.icon [
                Html.i [prop.className "fa-solid fa-chevron-down"]
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
        Standard.input [
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
        ] [str contentType]
        label [
            Class "checkbox-checkmark";
            HtmlFor (sprintf "checkradio%i%s" id contentType)
        ] []
    ]


let findTerm (columnValidation:ColumnValidation) (buildingBlocks:BuildingBlock []) =
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
    div [] [
        Bulma.field.div [
            Bulma.field.hasAddons
            prop.children [
                for i in 1 .. 5 do
                    yield
                        Bulma.control.div [
                            Bulma.button.a [
                                Bulma.color.isWarning
                                Bulma.button.isOutlined
                                prop.style [style.padding 0; style.borderColor model.SiteStyleState.ColorMode.BodyForeground]
                                prop.onClick (fun e ->
                                    let nextColumnValidation = {
                                        columnValidation with
                                            Importance = i |> Some
                                    }
                                    let nextTableValidation =
                                        updateTableValidationByColValidation model nextColumnValidation
                                    UpdateTableValidationScheme nextTableValidation |> ValidationMsg |> dispatch
                                    )
                                Html.span [
                                    //Fa.Size Fa.FaLarge
                                    if columnValidation.Importance.IsSome && columnValidation.Importance.Value >= i then
                                        prop.className "fa-solid fa-star"
                                    else
                                        prop.className "fa-regular fa-star"
                                    //Fa.Props [Style [Color NFDIColors.Yellow.Base]]
                                ] |> prop.children
                            ]
                        ]
                yield
                    Bulma.button.a [
                        Bulma.color.isDanger
                        Bulma.button.isOutlined
                        prop.onClick (fun _ ->
                            let nextColumnValidation = {
                                columnValidation with
                                    Importance = None
                            }
                            let nextTableValidation =
                                updateTableValidationByColValidation model nextColumnValidation
                            UpdateTableValidationScheme nextTableValidation |> ValidationMsg |> dispatch
                            )
                        Html.span [ prop.className "fa-solid fa-delete-left" ]
                        |> prop.children
                    ]
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
    Standard.tr [] [
        td [
            ColSpan 4
            Style [
                Padding "0";
                if isVisible then BorderBottom (sprintf "2px solid %s" NFDIColors.Mint.Base)
            ]
        ] [
            Bulma.box [
                prop.style [
                        if isVisible then style.display.block else style.display.none
                        style.width (length.perc 100)
                        style.borderRadius 0
                        style.backgroundColor model.SiteStyleState.ColorMode.BodyForeground
                        style.color model.SiteStyleState.ColorMode.Text
                    ]
                Bulma.columns [
                    Bulma.column [
                        b [] [str "Content Type"]

                        Bulma.help [prop.className "mb-1"; prop.text "Select the specific type of content for the selected column."]
                    
                        yield! checkradioList ind columnValidation model dispatch

                    ]
                    Bulma.column [
                        b [] [str "Importance"]

                        Bulma.help [str "Define how important it is to fill in the column correctly."]

                        sliderElements ind columnValidation model dispatch
                    ]
                ]
                |> prop.children
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
    ] [
        Bulma.field.div [
            prop.style [style.width(length.perc 100)]
            // annotationTable name - DateTime of saving
            prop.children [
                div [
                    Id "TableRepresentationInfoHeader"
                    OnTransitionEnd (fun e ->
                        let header = Browser.Dom.document.getElementById("TableRepresentationInfoHeader")
                        header?style?opacity <- 1
                        header?style?transition <- "unset"
                    )
                ] [
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
                Bulma.table [
                    Bulma.table.isHoverable
                    Bulma.table.isFullWidth
                    prop.children [
                        thead [ ] [
                            Standard.tr [ ] [
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
                ]

                // Show warning if no validation format was found
                if model.ValidationState.TableValidationScheme.SwateVersion = "" then
                    Bulma.label """No checklist for this table found! Hit "Add checklist to workbook" to add a checklist for the active annotation table."""

                // Submit new checklist scheme. This will write custom xml into the workbook.
                Bulma.button.a [
                    Bulma.color.isSuccess
                    Bulma.button.isFullWidth
                    prop.onClick (fun e ->
                        let header = Browser.Dom.document.getElementById("TableRepresentationInfoHeader")
                        header?style?transition <- "0.3s ease"
                        header?style?opacity <- 0
                        OfficeInterop.WriteTableValidationToXml (model.ValidationState.TableValidationScheme, model.PersistentStorageState.AppVersion) |> OfficeInteropMsg |> dispatch
                    )
                    prop.custom ("data-tooltip","Write checklist info to excel worksheet.")
                    prop.className "has-tooltip-right has-tooltip-multiline"
                    prop.text "Add checklist to workbook"
                ]
            ]
        ]
    ]

let validationComponent model dispatch =
    form [
        OnSubmit (fun e -> e.preventDefault())
        // https://keycode.info/
        OnKeyDown (fun k -> if k.key = "Enter" then k.preventDefault())
    ] [
        Bulma.label "Checklist Editor"

        Bulma.help "Display a table representation and add information to later validate values in the table according to their respective column."

        Bulma.button.a [
            Bulma.color.isInfo
            Bulma.button.isFullWidth
            prop.onClick (fun e -> OfficeInterop.GetTableValidationXml |> OfficeInteropMsg |> dispatch )
            prop.className "has-tooltip-right has-tooltip-multiline"
            prop.style [style.margin(length.rem 1; length.px 0)]
            prop.custom ("data-tooltip","Get checklist info for currently shown annotation table.")
            prop.text "Update table representation"
        ]

        Bulma.label [
            Html.span """Adjust current Swate table checklist. """
            span [
                Class "has-tooltip-right has-tooltip-multiline"
                Props.Custom ("data-tooltip", """When hitting "Add checklist to workbook" this information will be saved as part of the workbook.""")
                Style [Color NFDIColors.LightBlue.Base; MarginLeft ".5rem"]
            ] [
                Html.i [ prop.className "fa-solid fa-info-circle" ]
            ]
        ]

        validationElements model dispatch
            
    ]