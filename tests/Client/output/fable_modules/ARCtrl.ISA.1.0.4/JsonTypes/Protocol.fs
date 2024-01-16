namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Update

type Protocol =
    {       
        ID : URI option
        Name :          string option
        ProtocolType :  OntologyAnnotation option
        Description :   string option
        Uri :           URI option
        Version :       string option
        Parameters :    ProtocolParameter list option
        Components :    Component list option
        Comments :      Comment list option
    }

    static member make id name protocolType description uri version parameters components comments : Protocol= 
        {       
            ID              = id
            Name            = name
            ProtocolType    = protocolType
            Description     = description
            Uri             = uri
            Version         = version
            Parameters      = parameters
            Components      = components
            Comments        = comments
        }

    static member create(?Id,?Name,?ProtocolType,?Description,?Uri,?Version,?Parameters,?Components,?Comments) : Protocol =
        Protocol.make Id Name ProtocolType Description Uri Version Parameters Components Comments

    static member empty = 
        Protocol.create()


    /// If a protocol with the given identfier exists in the list, returns it
    static member tryGetByName (name : string) (protocols : Protocol list) =
        List.tryFind (fun (p:Protocol) -> p.Name = Some name) protocols

    /// If a protocol with the given name exists in the list exists, returns true
    static member existsByName (name : string) (protocols : Protocol list) =
        List.exists (fun (p:Protocol) -> p.Name = Some name) protocols

    /// Adds the given protocol to the protocols  
    static member add (protocols : Protocol list) (protocol : Protocol) =
        List.append protocols [protocol]

    /// Updates all protocols for which the predicate returns true with the given protocol values
    static member updateBy (predicate : Protocol -> bool) (updateOption : UpdateOptions) (protocol : Protocol) (protocols : Protocol list) =
        if List.exists predicate protocols then
            List.map (fun p -> if predicate p then updateOption.updateRecordType p protocol else p) protocols
        else 
            protocols

    /// Updates all protocols with the same name as the given protocol with its values
    static member updateByName (updateOption:UpdateOptions) (protocol : Protocol) (protocols : Protocol list) =
        Protocol.updateBy (fun p -> p.Name = protocol.Name) updateOption protocol protocols

    /// If a protocol with the given name exists in the list, removes it
    static member removeByName (name : string) (protocols : Protocol list) = 
        List.filter (fun (p:Protocol) -> p.Name = Some name |> not) protocols

    // Comments

    /// Returns comments of a protocol
    static member getComments (protocol : Protocol) =
        protocol.Comments

    /// Applies function f on comments of a protocol
    static member mapComments (f : Comment list -> Comment list) (protocol : Protocol) =
        { protocol with 
            Comments = Option.map f protocol.Comments}

    /// Replaces comments of a protocol by given comment list
    static member setComments (protocol : Protocol) (comments : Comment list) =
        { protocol with
            Comments = Some comments }

    // Protocol Type

    /// Returns protocol type of a protocol
    static member getProtocolType (protocol : Protocol) =
        protocol.ProtocolType

    /// Applies function f on protocol type of a protocol
    static member mapProtocolType (f : OntologyAnnotation -> OntologyAnnotation) (protocol : Protocol) =
        { protocol with 
            ProtocolType = Option.map f protocol.ProtocolType}

    /// Replaces protocol type of a protocol by given protocol type
    static member setProtocolType (protocol : Protocol) (protocolType : OntologyAnnotation) =
        { protocol with
            ProtocolType = Some protocolType }

    // Protocol Version

    /// Returns protocol version of a protocol
    static member getVersion (protocol : Protocol) =
        protocol.Version

    /// Applies function f on protocol version of a protocol
    static member mapVersion (f : string -> string) (protocol : Protocol) =
        {protocol with 
            Version = Option.map f protocol.Version}

    /// Replaces protocol version of a protocol by given protocol version
    static member setVersion (protocol : Protocol) (version : string) =
        { protocol with
            Version = Some version }

    /// Protocol Name

    /// Returns protocol name of a protocol
    static member getName (protocol : Protocol) =
        protocol.Name

    /// Applies function f on protocol name of a protocol
    static member mapName (f : string -> string) (protocol : Protocol) =
        {protocol with 
            Name = Option.map f protocol.Name}

    /// Replaces protocol name of a protocol by given protocol name
    static member setName (protocol : Protocol) (name : string) =
        { protocol with
            Name = Some name }
    
    // Protocol Description

    /// Returns protocol description of a protocol
    static member getDescription (protocol : Protocol) =
        protocol.Description

    /// Applies function f on protocol description of a protocol
    static member mapDescription (f : string -> string) (protocol : Protocol) =
        {protocol with 
            Description = Option.map f protocol.Description}

    /// Replaces protocol description of a protocol by given protocol description
    static member setDescription (protocol : Protocol) (description : string) =
        { protocol with
            Description = Some description }
    
    // Protocol URI
    
    /// Returns protocol URI of a protocol
    static member getUri (protocol : Protocol) =
        protocol.Uri

    /// Applies function f on protocol URI of a protocol
    static member mapUri (f : URI -> URI) (protocol : Protocol) =
        {protocol with 
            Uri = Option.map f protocol.Uri}

    /// Replaces protocol URI of a protocol by given protocol URI
    static member setUri (protocol : Protocol) (uri : URI) =
        { protocol with
            Uri = Some uri }



    // Components

    /// Returns components of a protocol
    static member getComponents (protocol : Protocol) =
        protocol.Components

    /// Applies function f on components of a protocol
    static member mapComponents (f : Component list -> Component list) (protocol : Protocol) =
        { protocol with 
            Components = Option.map f protocol.Components}

    /// Replaces components of a protocol by given component list
    static member setComponents (protocol : Protocol) (components : Component list) =
        { protocol with
            Components = Some components }

    static member addComponent (comp : Component) (protocol : Protocol) =
        protocol.Components 
        |> Option.defaultValue []
        |> fun cs -> List.append cs [comp]
        |> Protocol.setComponents protocol
    

    // Protocol Parameters
    
    /// Returns protocol parameters of a protocol
    static member getParameters (protocol : Protocol) =
        protocol.Parameters
   

    /// Applies function f on protocol parameters of a protocol
    static member mapParameters (f : ProtocolParameter list -> ProtocolParameter list) (protocol : Protocol) =
        { protocol with 
            Parameters = Option.map f protocol.Parameters}
    
    /// Replaces protocol parameters of a protocol by given protocol parameter list
    static member setParameters (protocol : Protocol) (parameters : ProtocolParameter list) =
        { protocol with
            Parameters = Some parameters }

    static member addParameter (parameter : ProtocolParameter) (protocol : Protocol) =
        protocol.Parameters 
        |> Option.defaultValue []
        |> fun ps -> List.append ps [parameter]
        |> Protocol.setParameters protocol