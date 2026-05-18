module Renderer.Components.InitState

open Feliz

open Fable.Core

open Swate.Components
open Swate.Components.Primitive.BaseModal
open Swate.Components.Primitive.CardGrid

module private InitStateHelper =
    let openARC =
        fun () -> promise {
            let! r = Api.ipcArcVaultApi.openARC ()

            match r with
            | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
            | Ok _ -> ()
        }

    let createARC =
        fun identifier -> promise {
            let! r = Api.ipcArcVaultApi.createARC identifier

            match r with
            | Error e -> console.error (Fable.Core.JS.JSON.stringify e.Message)
            | Ok _ -> ()
        }

open InitStateHelper

[<ReactComponent>]
let CreateNewArcModalContent (close: unit -> unit) =

    let isValid, setIsValid = React.useState (true)
    let temp, setTemp = React.useState ("")

    let handleSubmit =
        fun () ->
            if isValid then
                console.log ("Starting Create ARC:", temp)
                createARC temp |> Promise.start

            close ()

    React.Fragment [
        Html.fieldSet [
            prop.className "swt:fieldset"
            prop.children [
                Html.legend [
                    prop.className "swt:fieldset-legend"
                    prop.text "ARC Identifier"
                ]
                Html.label [
                    prop.className "swt:input swt:w-full"
                    prop.children [
                        Html.input [
                            prop.type'.text
                            prop.required true
                            prop.onKeyDown (key.enter, fun _ -> handleSubmit ())
                            prop.onChange (fun (v: string) ->
                                if System.String.IsNullOrEmpty v then
                                    setIsValid true
                                else
                                    let isValid = ARCtrl.Helper.Identifier.tryCheckValidCharacters v
                                    setIsValid isValid

                                setTemp v
                            )
                        ]
                    ]
                ]
                Html.p [
                    prop.hidden (isValid)
                    prop.className "swt:text-error"
                    prop.text
                        "New identifier contains forbidden characters! Allowed characters are: letters, digits, underscore (_), dash (-) and whitespace ( )."
                ]
            ]
        ]
        Html.button [
            prop.className "swt:btn swt:mt-4"
            prop.disabled (not isValid)
            prop.onClick (fun _ -> handleSubmit ())
            prop.text "Create new ARC"
        ]
    ]

[<ReactComponent>]
let InitState () =

    let modalIsOpen, setModalIsOpen = React.useState (false)
    let pageStateCtx = Renderer.Context.PageStateContext.usePageStateCtx ()

    React.Fragment [
        BaseModal.BaseModal(modalIsOpen, setModalIsOpen, CreateNewArcModalContent(fun () -> setModalIsOpen false))
        CardGrid.CardGrid(
            React.Fragment [
                CardGrid.CardGridButton(
                    Html.i [
                        prop.className "swt:iconify swt:fluent--folder-open-24-filled"
                    ],
                    "Open ARC",
                    "Open a locally existing ARC!",
                    (openARC >> Promise.start)
                )
                CardGrid.CardGridButton(
                    Html.i [
                        prop.className "swt:iconify swt:fluent--folder-open-24-filled"
                    ],
                    "New ARC",
                    "Create a new ARC!",
                    (fun _ -> setModalIsOpen (not modalIsOpen))
                )
                CardGrid.CardGridButton(
                    Html.i [
                        prop.className "swt:iconify swt:fluent--cloud-beaker-24-filled swt:size-5"
                    ],
                    "Download ARC",
                    "Download an existing ARC from DataHub!",
                    (fun _ -> pageStateCtx.setState (Some Renderer.Types.PageState.DataHubBrowser))
                )
            ],
            gridClassName = "swt:grid swt:grid-cols-2"
        )
    ]
