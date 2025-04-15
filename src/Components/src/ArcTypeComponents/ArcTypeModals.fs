namespace Swate.Components

open System
open ARCtrl
open Feliz
open Feliz.DaisyUI
open Swate.Components
open Swate.Components.Shared
open Fable.Core
open Browser.Types

module ArcTypeModalsUtil =
    let inputKeydownHandler = fun (e: KeyboardEvent) submit cancel ->
        match e.code with
        | kbdEventCode.enter ->
            if e.ctrlKey || e.metaKey then
                e.preventDefault()
                e.stopPropagation()
                submit()
        | kbdEventCode.escape ->
            e.preventDefault()
            e.stopPropagation()
            cancel()
        | _ -> ()

type InputField =
    static member Input(v: string, setter: string -> unit, label: string, rmv, submit, ?autofocus: bool) =
        let autofocus = defaultArg autofocus false
        Html.div [
            prop.className "flex flex-col gap-2"
            prop.children [
                Html.label [
                    prop.className "label"
                    prop.text label
                ]
                Html.input [
                    prop.className "input input-bordered"
                    prop.autoFocus autofocus
                    prop.valueOrDefault v
                    prop.onChange (fun (input: string) -> setter input)
                    prop.onKeyDown (fun (e: KeyboardEvent) ->
                        ArcTypeModalsUtil.inputKeydownHandler e submit rmv
                    )
                ]
            ]
        ]

    static member TermCombi(v: Term option, setter: Term option -> unit, label: string, rmv, submit, ?autofocus: bool) =
        let autofocus = defaultArg autofocus false
        Html.div [
            prop.className "flex flex-col gap-2"
            prop.children [
                Html.label [
                    prop.className "label"
                    prop.text label
                ]
                TermSearch.TermSearch(
                    setter,
                    term = v,
                    classNames = Swate.Components.TermSearchStyle(U2.Case1 "border-current"),
                    advancedSearch = U2.Case2 true,
                    showDetails = true,
                    autoFocus = autofocus,
                    portalModals = Browser.Dom.document.body,
                    onKeyDown = fun (e: KeyboardEvent) ->
                        ArcTypeModalsUtil.inputKeydownHandler e submit rmv
                )
            ]
        ]

type FooterButtons =
    static member Cancel(rmv: unit -> unit) =
        Daisy.button.button [
            button.outline
            prop.text "Cancel"
            prop.onClick(fun e -> rmv ())
        ]

    static member Submit(submitOnClick: unit -> unit) =
        Daisy.button.button [
            button.primary
            prop.text "Submit"
            prop.className "ml-auto"
            prop.onClick(fun e ->
                submitOnClick()
            )
        ]

[<Mangle(false); Erase>]
type CompositeCellModal =

    /// pr is required to make indicators on termsearch not overflow
    /// pl is required to make the input ouline when focused not cut of
    static member BaseModalContentClassOverride = "overflow-y-auto space-y-2 pl-1 pr-4"

    static member TermModal(oa: OntologyAnnotation, setOa: OntologyAnnotation -> unit, rmv) =
        let initTerm = Term.fromOntologyAnnotation oa
        let tempTerm, setTempTerm = React.useState(initTerm)

        let submit = fun () ->
            tempTerm
            |> Term.toOntologyAnnotation
            |> setOa
            rmv()

        BaseModal.BaseModal(
            (fun _ -> rmv()),
            header = Html.div "Term",
            content = React.fragment [
                InputField.TermCombi(
                    Some tempTerm,
                    (fun t ->
                        t
                        |> Option.defaultValue (Term())
                        |> setTempTerm
                    ),
                    "Term Name",
                    rmv,
                    submit,
                    autofocus = true
                )
                InputField.Input(
                    (tempTerm.source |> Option.defaultValue ""),
                    (fun input ->
                        tempTerm.source <- Option.whereNot System.String.IsNullOrWhiteSpace input
                        setTempTerm(tempTerm)
                    ),
                    "Source",
                    rmv,
                    submit
                )
                InputField.Input(
                    (tempTerm.id |> Option.defaultValue ""),
                    (fun input ->
                        tempTerm.id <- Option.whereNot System.String.IsNullOrWhiteSpace input
                        setTempTerm(tempTerm)
                    ),
                    "Accession Number",
                    rmv,
                    submit
                )
            ],
            footer = React.fragment [
                FooterButtons.Cancel(rmv)
                FooterButtons.Submit(submit)
            ],
            contentClassInfo = CompositeCellModal.BaseModalContentClassOverride
        )

    static member UnitizedModal(v, oa: OntologyAnnotation, rmv) =
        BaseModal.BaseModal(
            (fun _ -> rmv())
        )

    static member FreeTextModal(v, rmv) =
        BaseModal.BaseModal(
            (fun _ -> rmv())
        )

    static member DataModal(v, rmv) =
        BaseModal.BaseModal(
            (fun _ -> rmv())
        )

    [<ReactComponent>]
    static member CompositeCellModal(compositeCell: CompositeCell, setCell, rmv: unit -> unit) =
        match compositeCell with
        | CompositeCell.Term oa ->
            let setOa = fun oa -> setCell(CompositeCell.Term oa)
            CompositeCellModal.TermModal(oa, setOa, rmv)
        | CompositeCell.Unitized (v, oa) ->
            CompositeCellModal.UnitizedModal(v, oa, rmv)
        | CompositeCell.FreeText v ->
            CompositeCellModal.FreeTextModal(v, rmv)
        | CompositeCell.Data v ->
            CompositeCellModal.DataModal(v, rmv)