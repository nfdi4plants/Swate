namespace Swate.Components

open System
open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Dom
open Browser.Types

type Dropdown =

    [<ReactComponent>]
    static member Main
        (isOpen, setIsOpen, toggle: ReactElement, recentARCs: ReactElement[], actionBar: ReactElement, ?potMaxWidth: int) =

        let ref = React.useElementRef ()
        React.useListener.onClickAway (ref, fun _ -> setIsOpen false)

        let maxWidth = defaultArg potMaxWidth 48

        Html.div [
            prop.ref ref
            prop.className [
                "swt:dropdown"
                if isOpen then
                    "swt:dropdown-open"
            ]
            prop.children [
                toggle
                if isOpen then
                    Html.ul [
                        prop.tabIndex 0
                        prop.className
                            "swt:dropdown-content swt:min-w-48 swt:menu swt:bg-base-200 swt:rounded-box swt:z-99 swt:p-2 swt:gap-2 swt:shadow-sm swt:top-110%"
                        prop.style [ style.maxWidth maxWidth ]
                        prop.children [ Html.div recentARCs; actionBar ]
                    ]
            ]
        ]
