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
            isLocalStorageClear: string -> unit -> bool
        ) =
        let ref = React.useElementRef ()
        let markedNodes, setMarkedNodes = React.useState (ResizeArray())

        React.useEffect (
            (fun () ->
                if ref.current.IsSome then
                    // https://developer.mozilla.org/en-US/docs/Web/API/Document/createTreeWalker
                    let treewalker = Browser.Dom.document.createTreeWalker (ref.current.Value, 0x4) // SHOW_TEXT
                    let mutable currentNode = treewalker.nextNode ()
                    let nodes = ResizeArray()

                    while isNullOrUndefined currentNode |> not do
                        nodes.Add currentNode
                        currentNode <- treewalker.nextNode ()

                    setMarkedNodes nodes
            ),
            [| box htmlString |]
        )

        React.useEffect (
            (fun () ->
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
            prop.custom ("data-theme", "light")
            prop.dangerouslySetInnerHTML htmlString
            prop.className
                "swt:prose swt:p-2 swt:pb-18 swt:rounded-lg swt:max-w-full swt:bg-base-300 swt:min-w-0 swt:[&_pre]:min-w-0 swt:box-border swt:[&_pre]:box-border swt:[&_code]:box-border swt:[&_pre]:whitespace-pre-wrap swt:[&_code]:whitespace-pre-wrap swt:[&_pre]:wrap-break-word swt:[&_code]:wrap-break-word"
            prop.id elementID
            prop.ref ref
        ]
