namespace Swate.Components.Theme

open Feliz
open Fable.Core
open Browser
open Fable.React
open Swate.Components
open Swate.Components.Primitives

[<Erase; Mangle(false)>]
type ThemeProvider =

    [<ReactComponent(true)>]
    static member ThemeProvider
        (children: ReactElement, ?dataAttribute: string, ?localStorageKey: string, ?enforceTheme: Theme)
        =
        let localStorageKey = defaultArg localStorageKey "swate-theme-ctx"
        let dataAttribute = defaultArg dataAttribute "data-theme"
        let (theme, setTheme) = React.useLocalStorage (localStorageKey, Theme.Auto)

        React.useLayoutEffect (
            (fun () ->
                match theme with
                | Theme.Auto -> document.documentElement.removeAttribute (dataAttribute)
                | themeName -> document.documentElement.setAttribute (dataAttribute, unbox themeName)

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