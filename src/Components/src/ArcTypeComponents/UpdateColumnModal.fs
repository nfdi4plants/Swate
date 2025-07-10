namespace Swate.Components

open System.Text.RegularExpressions
open Feliz
open ARCtrl


type UpdateColumnModal =

    [<ReactComponent>]
    static member private UpdateForm(cellValues: string[], setPreview, regex: string, setRegex: string -> unit) =
        let replacement, setReplacement = React.useState ("")

        let updateCells (replacement: string) (regex: string) =
            if regex <> "" then
                try
                    let regex = Regex(regex)

                    cellValues
                    |> Array.mapi (fun i c ->
                        let m = regex.Match(c)

                        match m.Success with
                        | true ->
                            let replaced = c.Replace(m.Value, replacement)
                            replaced
                        | false -> c)
                    |> setPreview
                with _ ->
                    ()
            else
                ()

        Html.div [
            prop.className "swt:flex gap-2"
            prop.children [
                Html.div [
                    prop.className "swt:fieldset"
                    prop.children [
                        Html.legend [ prop.className "swt:fieldset-legend"; prop.text "Regex" ]
                        Html.input [
                            prop.autoFocus true
                            prop.className "swt:input swt:input-xs swt:sm:input-sm swt:md:input-md"
                            prop.valueOrDefault regex
                            prop.onChange (fun (ev: Browser.Types.Event) ->
                                let target = ev.target :?> Browser.Types.HTMLInputElement
                                let value = target.value
                                setRegex value
                                updateCells replacement value)
                        ]
                        Html.legend [ prop.className "swt:fieldset-legend"; prop.text "Replacement" ]
                        Html.input [
                            prop.className "swt:input swt:input-xs swt:sm:input-sm swt:md:input-md"
                            prop.valueOrDefault replacement
                            prop.onChange (fun (ev: Browser.Types.Event) ->
                                let target = ev.target :?> Browser.Types.HTMLInputElement
                                let value = target.value
                                setReplacement value
                                updateCells value regex)
                        ]
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member UpdateColumnModal(columnIndex: int, arcTable:ArcTable, setColumn, rmv: unit -> unit, ?debug) =

        let column = arcTable.GetColumn(columnIndex)

        let getCellStrings () =
            column.Cells |> Array.map (fun c -> c.ToString())

        let preview, setPreview = React.useState (getCellStrings)

        /// This state is only used for update logic
        let regex, setRegex = React.useState ("")

        let debug = defaultArg debug false

        let submit =
            fun () ->
                preview
                |> Array.map (fun x -> CompositeCell.FreeText x)
                |> fun x -> CompositeColumn.create (column.Header, x)
                |> fun column -> setColumn column

        let content = ComponentHelper.PreviewTable(column, preview, regex)

        let footer =
            Html.div [
                if debug then
                    prop.testId "Update Column"
                prop.className "swt:justify-end swt:flex swt:gap-2"
                prop.style [ style.marginLeft length.auto ]
                prop.children [
                    Html.button [
                        prop.className "swt:btn swt:btn-outline"
                        prop.text "Cancel"
                        prop.onClick (fun _ -> rmv())
                    ]
                    Html.button [
                        prop.className "swt:btn swt:btn-primary"
                        prop.style [ style.marginLeft length.auto ]
                        prop.text "Submit"
                        prop.onClick (fun _ ->
                            submit ()
                            rmv())
                    ]
                ]
            ]

        Html.div [
            prop.className "swt:flex swt:flex-col swt:h-full swt:gap-4 swt:min-h-[500px]"
            prop.children [
                Html.div [
                    prop.className "swt:border-b swt:pb-2 swt:mb-2"
                    prop.children [
                        UpdateColumnModal.UpdateForm(getCellStrings (), setPreview, regex, setRegex)
                    ]
                ]
                Html.div [
                    prop.className "swt:flex-grow swt:overflow-y-auto swt:h-[200px]"
                    prop.children [
                        content
                    ]
                ]
                Html.div [
                    prop.className "swt:border-t swt:pt-2 swt:mt-2"
                    prop.children [
                        footer
                    ]
                ]
            ]
        ]
