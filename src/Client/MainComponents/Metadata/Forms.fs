namespace MainComponents.Metadata

open Feliz
open Feliz.Bulma

open Spreadsheet
open Messages
open Browser.Types
open Fable.Core.JsInterop
open ARCtrl.ISA
open Shared


type FormComponents =

    [<ReactComponent>]
    static member TextInput (input: string, label: string, setter: string -> unit, ?placeholder: string) =
        let loading, setLoading = React.useState(false)
        let state, setState = React.useState(input)
        React.useEffect((fun () -> setState input), dependencies=[|box input|])
        Bulma.field.div [
            if label <> "" then Bulma.label label
            Bulma.control.div [
                if loading then Bulma.control.isLoading
                prop.children [
                    Bulma.input.text [
                        if placeholder.IsSome then prop.placeholder placeholder.Value
                        prop.valueOrDefault state
                        prop.onChange(fun (e: string) ->
                            setState e
                            debouncel "t-field" 1000 setLoading setter e
                        )
                    ]
                ]
            ]
        ]

    [<ReactComponent>]
    static member OntologyAnnotationInput (input: OntologyAnnotation, label: string, setter: OntologyAnnotation -> unit) =
        Bulma.field.div [ 
            Bulma.label label
            Html.div [
                prop.classes ["is-flex-direction-row"; "is-flex"; "is-justify-content-space-between"; "is-flex-wrap-wrap"]
                prop.children [
                    FormComponents.TextInput(
                        input.NameText,
                        $"Term Name",
                        fun s -> OntologyAnnotation.fromString(s, ?tsr=input.TermSourceREF, ?tan=input.TermAccessionNumber) |> setter
                    )
                    FormComponents.TextInput(
                        input.TermSourceREFString,
                        $"TSR",
                        fun s -> 
                            let s2 = s |> fun s -> if s = "" then None else Some s
                            OntologyAnnotation.fromString(input.NameText, ?tsr=s2, ?tan=input.TermAccessionNumber) |> setter
                    )
                    FormComponents.TextInput(
                        input.TermAccessionShort,
                        $"TAN",
                        fun s -> 
                            let s2 = s |> fun s -> if s = "" then None else Some s
                            OntologyAnnotation.fromString(input.NameText, ?tsr=input.TermSourceREF, ?tan=s2) |> setter
                    )
                ]
            ]
        ]

    static member PersonInput (persons: Person [], label: string, setter: Person [] -> unit) =
        let createPersonTextInput (inputOpt: string option) (index: int) (innerSetter:(string option -> Person)) = 
            FormComponents.TextInput(Option.defaultValue "" inputOpt, "", 
                fun s -> 
                    let s = if s = "" then None else Some s
                    persons.[index] <- innerSetter s
                    setter persons
            )
        Bulma.field.div [
            Bulma.label label
            //yield! persons
            //|> Array.mapi (fun i person ->
            
            //)
            //Html.th "First Name"
            //Html.th "Mid Initilias"
            //Html.th "Last Name"
            //Html.th "ORCID"
            //Html.th []
            ////Html.th "Affiliation"
            ////Html.th "Email"
            ////Html.th "Address"
            ////Html.th "Phone"
            ////Html.th "Fax"
        ]