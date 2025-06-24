namespace Swate.Components

open System
open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components
open Swate.Components.Shared
open Fable.Core


type EditConfig =

    static member ConvertCellType (tHeaders: ReactElement[], tBody: ReactElement[], targetType: CompositeCellDiscriminate) =
        Html.div [
            Html.div [
                prop.children [
                    Html.div [
                        prop.className "swt:flex swt:flex-col swt:gap-2"
                        prop.children [
                            Html.small $"Transform the existing cell type into {targetType} and adapt the values as depicted on submit."
                            Html.div [
                                prop.className "swt:overflow-x-auto swt:border swt:border-base-content/5"
                                prop.children [
                                    Html.table [
                                        prop.className "swt:table swt:table-xs"
                                        prop.children [
                                            Html.thead [
                                                Html.tr (
                                                    tHeaders
                                                )
                                            ]
                                            Html.tbody (
                                                [
                                                    Html.tr (
                                                        tBody
                                                    )
                                                ]
                                            )
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

[<Mangle(false); Erase>]
type CompositeCellEditModal =

    /// pr is required to make indicators on termsearch not overflow
    /// pl is required to make the input ouline when focused not cut of
    static member BaseModalContentClassOverride =
        "swt:overflow-y-auto swt:overflow-x-hidden swt:space-y-2 swt:pl-1 swt:pr-4 swt:py-1"

    static member TransformTermUnit
        (cell: CompositeCell, header: CompositeHeader, setUnitized: OntologyAnnotation -> unit, rmv)
        =

        let oa = cell.AsTerm
        let term = Term.fromOntologyAnnotation oa

        let submit =
            fun () ->
                term |> Term.toOntologyAnnotation |> setUnitized
                rmv ()

        let termHeader = header.ToTerm()
        let tHeaders =
            [|
                Html.th (header.ToString())
                Html.th ("Unit")
                Html.th ($"Term Source REF: {termHeader.TermSourceREF}")
                Html.th ($"Term Accession Number {termHeader.TermAccessionNumber}")
            |]
        let tBody =
            [|
                Html.td ($"{oa.Name}")
                Html.td ($"{oa.Name}")
                Html.td ($"{oa.TermSourceREF}")
                Html.td ($"{oa.TermAccessionNumber}")
            |]

        BaseModal.BaseModal(
            (fun _ -> rmv ()),
            header = Html.div "Term to Unit",
            content =
                React.fragment [
                    EditConfig.ConvertCellType(tHeaders, tBody, CompositeCellDiscriminate.Unitized)
                ],
            footer = React.fragment [ FooterButtons.Cancel(rmv); FooterButtons.Submit(submit) ],
            contentClassInfo = CompositeCellEditModal.BaseModalContentClassOverride
        )

    static member UnitToTerm
        (cell: CompositeCell, header: CompositeHeader, setTerm: OntologyAnnotation -> unit, rmv)
        =

        let _, oa = cell.AsUnitized
        let term = Term.fromOntologyAnnotation oa

        let submit =
            fun () ->
                term |> Term.toOntologyAnnotation |> setTerm
                rmv ()

        let termHeader = header.ToTerm()
        let tHeaders =
            [|
                Html.th (header.ToString())
                Html.th ($"Term Source REF: {termHeader.TermSourceREF}")
                Html.th ($"Term Accession Number {termHeader.TermAccessionNumber}")
            |]
        let tBody =
            [|
                Html.td ($"{oa.Name}")
                Html.td ($"{oa.TermSourceREF}")
                Html.td ($"{oa.TermAccessionNumber}")
            |]

        BaseModal.BaseModal(
            (fun _ -> rmv ()),
            header = Html.div "Unit to Term",
            content =
                React.fragment [
                    EditConfig.ConvertCellType(tHeaders, tBody, CompositeCellDiscriminate.Term)
                ],
            footer = React.fragment [ FooterButtons.Cancel(rmv); FooterButtons.Submit(submit) ],
            contentClassInfo = CompositeCellEditModal.BaseModalContentClassOverride
        )

    static member DataToFreeText
        (cell: CompositeCell, header: CompositeHeader, setText: string -> unit, rmv)
        =

        let data = cell.AsData
        let text = defaultArg data.Name ""

        let submit =
            fun () ->
                text |> setText
                rmv ()

        let dataHeader = header.TryIOType()
        if dataHeader.IsNone then failwith "No data column available!"
        let tHeaders =
            [|
                Html.th (header.ToString())
            |]
        let tBody =
            [|
                Html.td ($"{text}")
            |]

        BaseModal.BaseModal(
            (fun _ -> rmv ()),
            header = Html.div "Data to Text",
            content =
                React.fragment [
                    EditConfig.ConvertCellType(tHeaders, tBody, CompositeCellDiscriminate.Text)
                ],
            footer = React.fragment [ FooterButtons.Cancel(rmv); FooterButtons.Submit(submit) ],
            contentClassInfo = CompositeCellEditModal.BaseModalContentClassOverride
        )

    static member FreeTextToData
        (cell: CompositeCell, header: CompositeHeader, setData: Data -> unit, rmv)
        =

        let text = cell.AsFreeText
        let data = Data.create(Name = text)
        let submit =
            fun () ->
                data |> setData
                rmv ()

        let dataHeader = header.TryIOType()
        if dataHeader.IsNone then failwith "No data column available!"
        let tHeaders =
            [|
                Html.th (header.ToString())
                Html.th ("Selector")
                Html.th ("Format")
                Html.th ("Selector Format")
            |]
        let tBody =
            [|
                Html.td ($"{data.Name}")
                Html.td ($"{data.Selector}")
                Html.td ($"{data.Format}")
                Html.td ($"{data.SelectorFormat}")
            |]

        BaseModal.BaseModal(
            (fun _ -> rmv ()),
            header = Html.div "Text to Data",
            content =
                React.fragment [
                    EditConfig.ConvertCellType(tHeaders, tBody, CompositeCellDiscriminate.Data)
                ],
            footer = React.fragment [ FooterButtons.Cancel(rmv); FooterButtons.Submit(submit) ],
            contentClassInfo = CompositeCellEditModal.BaseModalContentClassOverride
        )

    [<ReactComponent>]
    static member CompositeCellTransformModal
        (
            compositeCell: CompositeCell,
            header: CompositeHeader,
            setCell: CompositeCell -> unit,
            rmv: unit -> unit,
            ?relevantCompositeHeader: CompositeHeader
        ) =

        match compositeCell with
        | CompositeCell.Term _ ->
            let setUnit = fun term -> setCell (CompositeCell.Unitized ("", term))
            CompositeCellEditModal.TransformTermUnit(compositeCell, header, setUnit, rmv)
        | CompositeCell.Unitized _ ->
            let setTerm = fun unit -> setCell (CompositeCell.Term unit)
            CompositeCellEditModal.UnitToTerm(compositeCell, header, setTerm, rmv)
        | CompositeCell.Data _ ->
            let setText = fun text -> setCell (CompositeCell.FreeText text)
            CompositeCellEditModal.DataToFreeText(compositeCell, header, setText, rmv)
        | CompositeCell.FreeText text ->
            if header.IsDataColumn then
                let setData = fun (data: Data) -> setCell (CompositeCell.Data data)
                CompositeCellEditModal.FreeTextToData(compositeCell, header, setData, rmv)
            else
                Html.none
