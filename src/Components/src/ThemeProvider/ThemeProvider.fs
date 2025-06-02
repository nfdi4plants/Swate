namespace Swate.Components

open Feliz
open Fable.Core
open Browser
open Fable.React

[<Erase; Mangle(false)>]
type ThemeProvider =

    [<ReactComponent(true)>]
    static member ThemeProvider
        (
            reactContext: IContext<Context<Theme>>,
            children: ReactElement,
            ?dataAttribute: string,
            ?localStorageKey: string
        ) =
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

        React.contextProvider (reactContext, { data = theme; setData = setTheme }, children)