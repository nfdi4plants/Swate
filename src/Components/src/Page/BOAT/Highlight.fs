namespace Components

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Fable.SimpleJson

[<EmitConstructor; Global>]
type Range() =
    member this.setStart: Browser.Types.Node -> int -> unit = jsNative
    member this.setEnd: Browser.Types.Node -> int -> unit = jsNative

[<Global>]
type Highlight([<ParamSeq>] ranges: ResizeArray<Range>) =
    member this.Log() = failwith "This should never be matched"

type IHighlights =
    abstract member clear: unit -> unit
    abstract member set: name: string -> Highlight -> unit

[<Global>]
type CSS =
    static member highlights: IHighlights = jsNative

type PaperWithMarker =

    [<ReactComponent>]
    static member Main
        (
            htmlString: string,
            markedKeys: string[],
            markedTerms: string[],
            markedValues: string[],
            elementID: string,
            isLocalStorageClear
        ) =
        let ref = React.useElementRef ()
        let markedNodes, setMarkedNodes = React.useState (ResizeArray())
        let APIwarningModalState, setwarningModal = React.useState (false)

        let setLocalFile (id: string) (nextFlag: bool) =
            let JSONString = Json.stringify nextFlag
            Browser.WebStorage.localStorage.setItem (id, JSONString)

        let initialWarning (id: string) =
            if isLocalStorageClear id () = true then
                false
            else
                Json.parseAs<bool> (Browser.WebStorage.localStorage.getItem id)

        let hasClosed, setHasClosed = React.useState (initialWarning "warningModal")
        let warningFlag, setWarningFlag = React.useState (true)

        React.useEffectOnce (fun _ ->
            if ref.current.IsSome then
                // https://developer.mozilla.org/en-US/docs/Web/API/Document/createTreeWalker
                let treewalker = Browser.Dom.document.createTreeWalker (ref.current.Value, 0x4) // SHOW_TEXT
                let mutable currentNode = treewalker.nextNode ()
                let nodes = ResizeArray()

                while isNullOrUndefined currentNode |> not do
                    nodes.Add currentNode
                    currentNode <- treewalker.nextNode ()

                setMarkedNodes nodes
        )

        React.useEffect (
            (fun () ->
                if CSS.highlights.Equals(null) then
                    setwarningModal true
                else
                    CSS.highlights.clear ()
                    //keys
                    let rangesKey =
                        markedNodes
                        |> Array.ofSeq
                        |> Array.map (fun n -> {|
                            Node = n
                            Text = n.textContent.ToLower()
                        |})
                        |> Array.collect (fun n ->
                            let indices: ResizeArray<int * int> = ResizeArray()

                            for phrase0 in markedKeys do
                                let phrase = phrase0.Trim().ToLower()
                                let index = n.Text.IndexOf(phrase)

                                if index > -1 then
                                    indices.Add(index, index + phrase.Length)

                            [|
                                for startIndex, endIndex in indices do
                                    let range = new Range()
                                    range.setStart n.Node startIndex
                                    range.setEnd n.Node endIndex
                                    range
                            |]
                        )
                        |> ResizeArray

                    let highlightKeys = new Highlight(rangesKey)
                    CSS.highlights.set "keyColor" highlightKeys
                    // terms
                    let rangesTerms =
                        markedNodes
                        |> Array.ofSeq
                        |> Array.map (fun n -> {|
                            Node = n
                            Text = n.textContent.ToLower()
                        |})
                        |> Array.collect (fun n ->
                            let indices: ResizeArray<int * int> = ResizeArray()

                            for phrase0 in markedTerms do
                                let phrase = phrase0.Trim().ToLower()
                                let index = n.Text.IndexOf(phrase)

                                if index > -1 then
                                    indices.Add(index, index + phrase.Length)

                            [|
                                for startIndex, endIndex in indices do
                                    let range = new Range()
                                    range.setStart n.Node startIndex
                                    range.setEnd n.Node endIndex
                                    range
                            |]
                        )
                        |> ResizeArray

                    let highlightValues = new Highlight(rangesTerms)
                    CSS.highlights.set "termColor" highlightValues
                    // values
                    let rangesValue =
                        markedNodes
                        |> Array.ofSeq
                        |> Array.map (fun n -> {|
                            Node = n
                            Text = n.textContent.ToLower()
                        |})
                        |> Array.collect (fun n ->
                            let indices: ResizeArray<int * int> = ResizeArray()

                            for phrase0 in markedValues do
                                let phrase = phrase0.Trim().ToLower()
                                let index = n.Text.IndexOf(phrase)

                                if index > -1 then
                                    indices.Add(index, index + phrase.Length)

                            [|
                                for startIndex, endIndex in indices do
                                    let range = new Range()
                                    range.setStart n.Node startIndex
                                    range.setEnd n.Node endIndex
                                    range
                            |]
                        )
                        |> ResizeArray

                    let highlightKeys = new Highlight(rangesValue)
                    CSS.highlights.set "valueColor" highlightKeys
            )
        )

        Html.div [
            prop.className "min-w-0"
            prop.children [
                Html.dialog [
                    prop.className [
                        "swt:modal"
                        if APIwarningModalState = true && hasClosed = false then
                            "swt:modal-open"
                    ]
                    prop.children [
                        Html.div [
                            prop.className "swt:modal-box"
                            prop.children [
                            Html.div [
                                Html.p "Text highlighting is not compatible with your browser."
                                Html.a [
                                    prop.text "View compatible browsers"
                                    prop.href
                                        "https://developer.mozilla.org/en-US/docs/Web/API/CSS_Custom_Highlight_API#browser_compatibility"
                                    prop.target.blank
                                    prop.className "swt:underline swt:text-info"
                                ]
                            ]
                            Html.div [
                                prop.className "swt:flex swt:items-center swt:mt-5"
                                prop.children [
                                    Html.p "Don't show this again"
                                    Html.input [
                                        prop.id "warningModal"
                                        prop.type'.checkbox
                                        // prop.checked hasClosed
                                        prop.className "swt:checkbox swt:checkbox-sm swt:ml-2"
                                        prop.onClick (fun _ ->
                                            setWarningFlag (not warningFlag)
                                            setLocalFile "warningModal" warningFlag
                                        )
                                    ]
                                ]
                            ]
                            Html.button [
                                prop.className "swt:btn swt:mt-5"
                                prop.text "Got it"
                                prop.onClick (fun _ -> setHasClosed (not hasClosed))
                            ]
                            ]
                        ]
                    ]
                ]
                Html.div [
                    prop.custom ("data-theme", "light")
                    prop.dangerouslySetInnerHTML htmlString
                    //prop.style [
                    //    style.custom ("whitespace", "pre-wrap")
                    //    style.custom ("word-wrap", "break-word")
                    //]
                    prop.className
                        "swt:prose swt:p-2 swt:rounded-lg swt:max-w-full swt:bg-base-300 swt:min-w-0 swt:[&_pre]:min-w-0 swt:box-border swt:[&_pre]:box-border swt:[&_code]:box-border swt:[&_pre]:whitespace-pre-wrap swt:[&_code]:whitespace-pre-wrap swt:[&_pre]:break-words swt:[&_code]:break-words"
                    prop.id elementID
                    prop.ref ref
                ]
            ]
        ]
