namespace ARCtrl.Template

open ARCtrl.ISA
open Fable.Core

[<AttachMembers>]
type Organisation =
| [<CompiledName "DataPLANT">] DataPLANT
| Other of string
    static member ofString (str:string) = 
        match str.ToLower() with
        | "dataplant" -> DataPLANT
        | _ -> Other str

    override this.ToString() =
        match this with
        | DataPLANT -> "DataPLANT"
        | Other anyElse -> anyElse

    member this.IsOfficial() = this = DataPLANT

[<AttachMembers>]
type Template(id: System.Guid, table: ArcTable, ?name: string, ?description, ?organisation: Organisation, ?version: string, ?authors: Person [], 
    ?repos: OntologyAnnotation [], ?tags: OntologyAnnotation [], ?lastUpdated: System.DateTime) =

    let name = defaultArg name ""
    let description = defaultArg description ""
    let organisation = defaultArg organisation (Other "Custom Organisation")
    let version = defaultArg version "0.0.0"
    let authors = defaultArg authors [||]
    let repos = defaultArg repos [||]
    let tags = defaultArg tags [||]
    let lastUpdated = defaultArg lastUpdated (System.DateTime.Now.ToUniversalTime())

    member val Id : System.Guid = id with get, set
    member val Table : ArcTable = table with get, set
    member val Name : string = name with get, set
    member val Description : string = description with get, set
    member val Organisation : Organisation = organisation with get, set
    member val Version : string = version with get, set
    member val Authors : Person [] = authors with get, set
    member val EndpointRepositories : OntologyAnnotation [] = repos with get, set
    member val Tags : OntologyAnnotation [] = tags with get, set
    member val LastUpdated : System.DateTime = lastUpdated with get, set

    static member make id table name description organisation version authors repos tags lastUpdated =
        Template(id, table, name, description, organisation, version, authors, repos, tags, lastUpdated)

    static member create(id, table, ?name, ?description, ?organisation, ?version, ?authors, ?repos, ?tags, ?lastUpdated) =
        Template(id, table, ?name=name, ?description = description, ?organisation=organisation, ?version=version, ?authors=authors, ?repos=repos, ?tags=tags, ?lastUpdated=lastUpdated)

    static member init(templateName: string) =
        let guid = System.Guid.NewGuid()
        let table = ArcTable.init(templateName)
        Template(guid, table, templateName)

    member this.SemVer
        with get() = ARCtrl.SemVer.SemVer.tryOfString this.Version

    /// <summary>
    /// Use this function to check if this Template and the input Template refer to the same object.
    ///
    /// If true, updating one will update the other due to mutability.
    /// </summary>
    /// <param name="other">The other Template to test for reference.</param>
    member this.ReferenceEquals (other: Template) = System.Object.ReferenceEquals(this,other)

    member this.StructurallyEquals (other: Template) =
        (this.Id = other.Id)
        && (this.Table = other.Table)
        && (this.Name = other.Name)
        && (this.Organisation = other.Organisation)
        && (this.Version = other.Version)
        && (this.Authors = other.Authors)
        && (this.EndpointRepositories = other.EndpointRepositories)
        && (this.Tags = other.Tags)
        && (this.LastUpdated = other.LastUpdated)

    // custom check
    override this.Equals other =
        match other with
        | :? Template as template -> 
            this.StructurallyEquals(template)
        | _ -> false

    override this.GetHashCode() =
        this.Id.GetHashCode()
        + this.Table.GetHashCode()
        + this.Name.GetHashCode()
        + this.Organisation.GetHashCode()
        + this.Version.GetHashCode()
        + this.Authors.GetHashCode()
        + this.EndpointRepositories.GetHashCode()
        + this.Tags.GetHashCode()
        + this.LastUpdated.GetHashCode()
        