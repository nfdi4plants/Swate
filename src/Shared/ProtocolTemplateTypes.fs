namespace Shared

module TemplateTypes =

    open System


    module Metadata =

        [<Literal>]
        let TemplateMetadataWorksheetName = "SwateTemplateMetadata" 

        type MetadataField = {
            /// Will be used to create rowKey
            Key             : string
            ExtendedNameKey : string
            Description     : string option
            List            : bool
            Children        : MetadataField list
        } with
            static member create(key,?extKey,?desc,?islist,?children) = {
                Key             = key
                ExtendedNameKey = if extKey.IsSome then extKey.Value else ""
                Description     = desc
                List            = if islist.IsSome then islist.Value else false
                Children        = if children.IsSome then children.Value else []
            }

            static member createParentKey parentKey (newKey:string) =
                let nk = newKey.Replace("#","")
                $"{parentKey} {nk}".Trim()

            /// Loop through all children to create ExtendedNameKey for all MetaDataField types
            member this.extendedNameKeys =
                let rec extendName (parentKey:string) (metadata:MetadataField) =
                    let nextParentKey = MetadataField.createParentKey parentKey metadata.Key
                    { metadata with
                        ExtendedNameKey = nextParentKey
                        Children        = if metadata.Children.IsEmpty |> not then metadata.Children |> List.map (extendName nextParentKey) else metadata.Children
                    }
                extendName "" this

            static member createWithExtendedKeys(key,?extKey,?desc,?islist,?children) =
                {
                    Key             = key
                    ExtendedNameKey = if extKey.IsSome then extKey.Value else ""
                    Description     = desc
                    List            = if islist.IsSome then islist.Value else false
                    Children        = if children.IsSome then children.Value else []
                }.extendedNameKeys


        module RowKeys =
            [<Literal>]
            let DescriptionKey  = "Description"
            [<Literal>]
            let TemplateIdKey   = "Id"

        open RowKeys

        // annotation value
        let private tsr                     = MetadataField.create("Term Source REF")
        let private tan                     = MetadataField.create("Term Accession Number")
        let private annotationValue         = MetadataField.create("#")
        // 
        let private id                      = MetadataField.create(TemplateIdKey, desc ="The unique identifier of this template. It will be auto generated.")
        let private name                    = MetadataField.create("Name", desc="The name of the Swate template.")
        let private version                 = MetadataField.create("Version", desc="The current version of this template in SemVer notation.")
        let private description             = MetadataField.create(DescriptionKey, desc ="The description of this template. Use few sentences for succinctness.")
        //let private docslink                = MetadataField.create("Docslink", desc="The URL to the documentation page.")
        let private organisation            = MetadataField.create("Organisation", desc="""The name of the template associated organisation. "DataPLANT" will trigger the "DataPLANT" batch of honor for the template.""")
        let private table                   = MetadataField.create("Table", desc="The name of the Swate annotation table in the workbook of the template's excel file.")
        // er
        let private er                      = MetadataField.create("ER",desc="A list of all ERs (endpoint repositories) targeted with this template. ERs are realized as Terms.",islist=true, children = [annotationValue; tan; tsr])
        // tags
        let private tags                    = MetadataField.create("Tags",desc="A list of all tags associated with this template. Tags are realized as Terms.", islist=true, children = [annotationValue; tan; tsr] )
        // person
        let private lastName                = MetadataField.create("Last Name")
        let private firstName               = MetadataField.create("First Name")
        let private midIntiials             = MetadataField.create("Mid Initials")
        let private email                   = MetadataField.create("Email")
        let private phone                   = MetadataField.create("Phone")
        let private fax                     = MetadataField.create("Fax")
        let private address                 = MetadataField.create("Address")
        let private affiliation             = MetadataField.create("Affiliation")
        let private orcid                   = MetadataField.create("ORCID")
        //let private roles                   = MetadataField.create("Role", children = [annotationValue; tan; tsr])
        let private roleAnnotationValue     = MetadataField.create("Role")
        let private roleTAN                 = MetadataField.create("Role Term Accession Number")
        let private roleTSR                 = MetadataField.create("Role Term Source REF")
        let private authors                 = MetadataField.create("Authors",desc="The author(s) of this template.", islist = true, children = [lastName; firstName; midIntiials; email; phone; fax; address; affiliation; orcid; roleAnnotationValue; roleTAN; roleTSR])
        // entry
        let root = MetadataField.createWithExtendedKeys("",children=[id;name;version;description;(*docslink;*)organisation;table;er;tags;authors]) 

    type Template = {
        Id                      : string
        Name                    : string
        Description             : string
        Organisation            : string
        Version                 : string
        Authors                 : string
        /// endpoint repository tags
        Er_Tags                 : string []
        Tags                    : string []
        TemplateBuildingBlocks  : OfficeInteropTypes.InsertBuildingBlock list
        LastUpdated             : System.DateTime
        Used                    : int
        // WIP
        Rating                  : int  
    } with
        static member create id name describtion organisation version lastUpdated author ertags tags templateBuildingBlocks used rating  = {
            Id                      = id
            Name                    = name
            Description             = describtion
            Organisation            = organisation
            Version                 = version
            Authors                 = author
            Er_Tags                 = ertags
            Tags                    = tags
            TemplateBuildingBlocks  = templateBuildingBlocks
            LastUpdated             = lastUpdated 
            Used                    = used
            // WIP                  
            Rating                  = rating
    }