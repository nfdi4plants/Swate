namespace Swate.Components.Composite.MarkdownText

open System
open Fable.Core
open Fable.Core.JsInterop
open Feliz

open Swate.Components.Composite.MarkdownText.JsBindings

[<RequireQualifiedAccess>]
module Preview =

    let mutable private MermaidInitialized = false

    let private ensureMermaidInitialized () =
        if not MermaidInitialized then
            Mermaid.instance.initialize (createObj [ "startOnLoad" ==> false; "securityLevel" ==> "strict" ])

            MermaidInitialized <- true

    let private tryGetClassName (props: obj) =
        let className: obj = props?className

        if isNullOrUndefined className then
            None
        else
            Some(string className)

    let private tryGetCode (props: obj) =
        let node: obj = props?node

        if isNullOrUndefined node then
            let children: obj = props?children

            if isNullOrUndefined children then
                None
            else
                match children with
                | :? string as text -> Some text
                | _ ->
                    try
                        children |> unbox<obj[]> |> Array.map string |> String.concat "" |> Some
                    with _ ->
                        Some(string children)
        else
            let children: obj = node?children

            if isNullOrUndefined children then
                None
            else
                Some(ReactMDEditor.getCodeString children)

    [<ReactComponent>]
    let private CodeRenderer (props: obj) =
        let className = tryGetClassName props |> Option.defaultValue ""
        let code = tryGetCode props |> Option.defaultValue ""

        let isMermaid =
            className.StartsWith("language-mermaid", StringComparison.OrdinalIgnoreCase)

        // Mermaid uses the id in querySelector, so keep a non-digit prefix to avoid invalid selectors.
        let renderId =
            React.useRef (
                let rawId = Guid.NewGuid().ToString "N"
                sprintf "mermaid-%s" rawId
            )

        let svg, setSvg = React.useState (None: string option)
        let renderError, setRenderError = React.useState (None: string option)

        React.useEffect (
            (fun () ->
                if isMermaid then
                    if String.IsNullOrWhiteSpace code then
                        setSvg None
                        setRenderError (Some "Mermaid code block is empty.")
                    else
                        ensureMermaidInitialized ()

                        Mermaid.instance.render (renderId.current, code)
                        |> Promise.map (fun result ->
                            setRenderError None
                            setSvg (Some result.svg)
                        )
                        |> Promise.catch (fun err ->
                            setSvg None
                            setRenderError (Some $"Mermaid render error: {string err}")
                        )
                        |> Promise.start
            ),
            [| box isMermaid; box code |]
        )

        if isMermaid then
            match renderError, svg with
            | Some message, _ ->
                Html.pre [
                    prop.className "swt:p-3 swt:rounded-box swt:bg-base-200 swt:text-error swt:text-sm"
                    prop.text message
                ]
            | None, Some renderedSvg ->
                Html.div [
                    prop.className "swt:my-2"
                    prop.dangerouslySetInnerHTML renderedSvg
                ]
            | None, None ->
                Html.pre [
                    prop.className "swt:p-3 swt:rounded-box swt:bg-base-200 swt:text-sm"
                    prop.text "Rendering Mermaid diagram..."
                ]
        else
            Html.code [
                if className <> "" then
                    prop.className className
                prop.text code
            ]

    let components = createObj [ "code" ==> CodeRenderer ]

    let rehypePlugins: obj[] = [| ReactMDEditor.rehypeSanitize |]
