namespace Swate.Components

open Feliz
open Feliz.DaisyUI
open Fable.Core
open Fable.React
open Feliz
open Feliz.DaisyUI
open Swate.Components

//Modal that is the base of all error modals
[<Mangle(false); Erase>]
type ErrorBaseModal =

    ///<summary>This modal is used to display errors from for example api communication</summary>
    static member ErrorBaseModal
        (
            rmv,
            error: string,
            ?width: int,
            ?height: int,
            ?debug: string
        ) =

        Html.div [
            if debug.IsSome then
                prop.testId ("errorModal_" + debug.Value)
            prop.className "swt:modal swt:modal-open"
            prop.onClick rmv
            prop.children [
                Html.div [
                    prop.onClick (fun ev -> ev.stopPropagation())
                    prop.style [
                        if width.IsSome then 
                            style.width (length.percent width.Value)
                        else
                            style.minWidth 200
                        if height.IsSome then
                            style.maxHeight (length.percent height.Value)
                        style.overflow.auto
                    ]
                    prop.children [
                        Html.div [
                            prop.className "swt:alert swt:alert-error"
                            prop.children [
                                Svg.svg [
                                    svg.className "swt:w-6 swt:h-6 swt:stroke-current"
                                    svg.viewBox (0, 0, 24, 24)
                                    svg.fill "none"
                                    svg.children [
                                        Svg.path [
                                            svg.d "M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z"
                                            svg.strokeLineCap "round"
                                            svg.strokeLineJoin "round"
                                            svg.strokeWidth 2
                                        ]
                                    ]
                                ]

                                Html.div [
                                    Html.h3 [
                                        prop.className "swt:font-bold"
                                        prop.text "An error occured!"
                                    ]
                                    Html.div [
                                        yield! 
                                            error.Split('\n')
                                            |> Array.collect (fun line -> [| Html.text line; Html.br [] |])
                                    ]
                                ]

                                Html.button [
                                    prop.className "swt:btn swt:bg-neutral-content swt:btn-outline swt:ml-auto"
                                    prop.text "Ok"
                                    prop.onClick rmv
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]