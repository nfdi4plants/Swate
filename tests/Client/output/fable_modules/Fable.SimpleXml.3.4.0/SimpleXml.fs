namespace Fable.SimpleXml

open Fable.Parsimmon

[<RequireQualifiedAccess>]
module SimpleXml =
    let children (node: XmlElement) = node.Children
    let isTextNode (node: XmlElement) = node.IsTextNode
    let attributes (node: XmlElement) = node.Attributes
    let content (node: XmlElement) = node.Content
    /// Tries to parse the input XML as a single element.
    let tryParseElement (input: string) =
        Parsimmon.parse input Parser.xmlElement

    /// Tries to parse the input XML as a single element. Throws an error if parsing fails
    let parseElement (input: string) =
        match tryParseElement input with
        | Some xml -> xml
        | None -> failwithf "Could not parse XML input as an element: %s" input

    /// Tries to parse the input XML as a document, where the document possibly contains a declaration element `<?xml ... ?>`
    let tryParseDocument (input: string) =
        Parsimmon.parse input Parser.xmlDocument

    /// Tries to parse the input XML as a document, where the document possibly contains a declaration element `<?xml ... ?>`. Throws an error if parsing fails
    let parseDocument (input: string) =
        match tryParseDocument input with
        | Some document -> document
        | None ->  failwithf "Could not parse XML input as a document: %s" input

    /// Recursively find elements that match a predicate
    let findElementsBy (pred: XmlElement -> bool) (node: XmlElement) =
        let rec findElements (root: XmlElement) =
            [
                if pred root then yield root
                for child in root.Children do
                    yield! findElements child
            ]

        findElements node

    /// Recursively find elements that have a given tag name
    let findElementsByName (tagName: string) (node: XmlElement) =
        findElementsBy (fun el -> el.Name = tagName) node

    /// Recursively find elements that the exact given set of attributes
    let findElementsByExactAttributes (attrs: Map<string, string>) (node: XmlElement)=
        findElementsBy (fun el -> el.Attributes = attrs) node

    let private mapIsSubsetOf (x: Map<string, string>) (y: Map<string, string>) =
        Map.forall (fun key _ -> Map.containsKey key y) x

    /// Recursively find elements that have a subset of the given attributes
    let findElementsByAttributes (attrs: Map<string, string>) (node: XmlElement) =
        findElementsBy (fun el -> mapIsSubsetOf attrs el.Attributes) node

    /// Recursively find an element that has a subset of the given attributes
    let tryFindElementByAttributes (attrs: Map<string, string>) (node: XmlElement) =
        node
        |> findElementsByAttributes attrs
        |> List.tryHead

    /// Recursively find an element that has a subset of the given attributes
    let findElementByAttributes (attrs: Map<string, string>) (node: XmlElement) =
        node
        |> tryFindElementByAttributes attrs
        |> function
            | Some element -> element
            | None -> failwith "Could not find an element with the given attributes"

    let rec excludeWhere pred node =
        { node with
            Children =
                node.Children
                |> List.filter (pred >> not)
                |> List.map (excludeWhere pred) }

    /// Just like `parseElement` but recursively excludes the extra verbose text nodes (leaving Content intact)
    let parseElementNonStrict =
        parseElement >> excludeWhere isTextNode

    /// Just like `tryParseElement` but recursively excludes the extra verbose text nodes (leaving Content intact)
    let tryParseElementNonStrict =
        tryParseElement >> Option.map (excludeWhere isTextNode)

        /// Just like `parseDocument` but recursively excludes the extra verbose text nodes (leaving Content intact)
    let parseDocumentNonStrict input =
        let document = parseDocument input
        { document with Root = excludeWhere isTextNode document.Root }

    /// Just like `tryParseDocument` but recursively excludes the extra verbose text nodes (leaving Content intact)
    let tryParseDocumentNonStrict input =
        match tryParseDocument input with
        | Some document -> Some { document with Root = excludeWhere isTextNode document.Root }
        | None -> None

    /// Recursively find elements that contain a given attribute
    let findElementsByAttribute key value node =
        node
        |> findElementsBy (fun el ->
            match Map.tryFind key el.Attributes with
            | Some attributeValue when value = attributeValue -> true
            | _ -> false)

    /// Tries to parse a list of XML elements that does not contain a root element
    let tryParseManyElements (input: string) =
        input
        |> sprintf "<dummyRoot>%s</dummyRoot>"
        |> tryParseElement
        |> Option.map children

    /// Tries to parse a list of XML elements that does not contain a root element. Throws an error if parsing fails.
    let parseManyElements (input: string) =
        input
        |> sprintf "<dummyRoot>%s</dummyRoot>"
        |> tryParseElement
        |> function
            | Some element -> element.Children
            | None -> failwithf "Could not parse input '%s' as a list of XML elements" input

    /// Recursively try to find an element that has a given tag name and return the first matching element, if any.
    let tryFindElementByName tagName root =
        findElementsByName tagName root
        |> List.tryHead

    /// Recursively try to find an element that has a given tag name and return the first matching element, if any.
    let findElementByName tagName root =
        match tryFindElementByName tagName root with
        | Some node -> node
        | None -> failwithf "Could not find element with name '%s' inside '%s'" tagName root.Name
