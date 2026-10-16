namespace Swate.Components.Composite.ThemeSelector

open Feliz
open Fable.Core
open Browser
open Fable.React
open Swate.Components
open Swate.Components.Primitive
open Swate.Components.Composite.ThemeSelector.Types

[<Erase; Mangle(false)>]
type ThemeProvider =

    [<ReactComponent(true)>]
    static member ThemeProvider
        (children: ReactElement, ?dataAttribute: string, ?localStorageKey: string, ?enforceTheme: Theme)
        =
        let localStorageKey = defaultArg localStorageKey "swate-theme-ctx"
        let dataAttribute = defaultArg dataAttribute "data-theme"
        let colorModeAttribute = "data-color-mode"
        let (theme, setTheme) = React.useLocalStorage (localStorageKey, Theme.Auto)

        React.useLayoutEffect (
            (fun () ->
                let root = document.documentElement

                match theme with
                | Theme.Auto ->
                    root.removeAttribute (dataAttribute)
                    // Without the attribute, the markdown preview follows the OS color scheme.
                    root.removeAttribute (colorModeAttribute)
                | themeName ->
                    root.setAttribute (dataAttribute, unbox themeName)

                    // The markdown preview picks its syntax colors from data-color-mode. The
                    // daisyUI theme that was just applied decides whether that is light or dark.
                    let colorMode =
                        if JsBindings.Dom.computedColorScheme root = "dark" then
                            "dark"
                        else
                            "light"

                    root.setAttribute (colorModeAttribute, colorMode)

                ()

            ),
            [| box theme |]
        )

        React.useLayoutEffect (
            (fun () ->
                match enforceTheme with
                | Some enforcedTheme when enforcedTheme <> theme -> setTheme enforcedTheme
                | _ -> ()
            ),
            [| box enforceTheme |]
        )

        Context.ThemeCtx.Provider({ state = theme; setState = setTheme }, children)
