namespace Fable.SimpleXml

module Generator =

    type XAttribute =
        | String of string * string
        | Int of string * int
        | Number of string * float
        | Boolean of string * bool

    type Tag = Tag of string

    type Prefix = Prefix of string

    type XNode =
        | Text of string
        | Comment of string
        | Leaf of (Tag * XAttribute list)
        | XNodeList of (Tag * XAttribute list * XNode list)
        | Namespace of (Prefix * XNode)

    type attr() =
        static member value(name, value) = XAttribute.String(name, value)
        static member value(name, value) = XAttribute.Int(name, value)
        static member value(name, value) = XAttribute.Number(name, value)
        static member value(name, value) = XAttribute.Boolean(name, value)

    let leaf name attrs = XNode.Leaf (Tag(name), attrs)
    let node name attrs values = XNode.XNodeList (Tag(name), attrs, values)
    let text value = XNode.Text value
    let comment value = XNode.Comment value
    let namespaceXml prefix node =
        match node with
        | XNode.Leaf _ | XNodeList _ -> Namespace(Prefix(prefix), node)
        | otherNodes -> failwith (sprintf "Cannot use 'namespaceXml' with '%A'. A xml namespace prefix can only be used with 'node' or 'leaf'" otherNodes)

    let serializeAttr = function
        | XAttribute.String (key, value) -> sprintf "%s=\"%s\"" key value
        | XAttribute.Int (key, value) -> sprintf "%s=\"%d\"" key value
        | XAttribute.Number (key, value) -> sprintf "%s=\"%f\"" key value
        | XAttribute.Boolean (key, value) -> sprintf "%s=\"%s\"" key (string value)

    let rec serializeXml = function
        | XNode.Text text -> text
        | XNode.Comment text -> sprintf "<!--%s-->" text
        //
        | XNode.Leaf (Tag(tag), [ ]) -> sprintf "<%s />" tag
        | XNode.Namespace (Prefix(prefix), XNode.Leaf (Tag(tag), [ ]) ) -> sprintf "<%s:%s />" prefix tag
        //
        | XNode.Leaf (Tag(tag), attributes) ->
            attributes
            |> List.map serializeAttr
            |> String.concat " "
            |> sprintf "<%s %s />" tag
        | XNode.Namespace (Prefix(prefix), XNode.Leaf (Tag(tag), attributes)) ->
            attributes
            |> List.map serializeAttr
            |> String.concat " "
            |> sprintf "<%s:%s %s />" prefix tag

        //
        | XNode.XNodeList (Tag(tag), [ ], children) ->
            let childNodes =
                children
                |> List.map serializeXml
                |> String.concat ""

            sprintf "<%s>%s</%s>" tag childNodes tag
        | XNode.Namespace (Prefix(prefix), XNode.XNodeList (Tag(tag), [ ], children)) ->
            let childNodes =
                children
                |> List.map serializeXml
                |> String.concat ""

            sprintf "<%s:%s>%s</%s:%s>" prefix tag childNodes prefix tag

        //
        | XNode.XNodeList (Tag(tag), attributes, children) ->
            let attributes =
                attributes
                |> List.map serializeAttr
                |> String.concat " "

            let childNodes =
                children
                |> List.map serializeXml
                |> String.concat ""

            sprintf "<%s %s>%s</%s>" tag attributes childNodes tag
        | XNode.Namespace (Prefix(prefix), XNode.XNodeList (Tag(tag), attributes, children)) ->
            let attributes =
                attributes
                |> List.map serializeAttr
                |> String.concat " "

            let childNodes =
                children
                |> List.map serializeXml
                |> String.concat ""

            sprintf "<%s:%s %s>%s</%s:%s>" prefix tag attributes childNodes prefix tag

        | Namespace (prefix, anyNode) ->
            failwith (sprintf "A 'Namespace' prefix cannot be applied to a '%A'." anyNode)

    ///
    let ofXmlElement (root:XmlElement) =
        let rec createChildren (child:XmlElement) =
            let createLeafOrNode child =
                match child.SelfClosing with
                | true ->
                    leaf child.Name [
                        for cAttr in child.Attributes do
                            yield attr.value(cAttr.Key,cAttr.Value)
                    ]
                | false ->
                    node child.Name [
                        for cAttr in child.Attributes do
                            yield attr.value(cAttr.Key,cAttr.Value)
                    ] [
                        for grandChild in child.Children do
                            yield createChildren grandChild
                        yield
                            text child.Content
                    ]
            match child.IsTextNode, child.IsComment, child.Namespace with
            | true, _, _ ->
                text child.Content
            | false, true, _ ->
                comment child.Content
            | false, _, Some prefix ->
                namespaceXml prefix (createLeafOrNode child)
            | false, _, None ->
                createLeafOrNode child
        node root.Name [
            for rAttr in root.Attributes do
                yield attr.value(rAttr.Key,rAttr.Value)
        ] [
            for child in root.Children do
                yield createChildren child
            yield
                text root.Content
        ]

    let ofXmlElements (rootElements:seq<XmlElement>) =
        seq [
            for rootElement in rootElements do
                yield ofXmlElement rootElement
        ]
