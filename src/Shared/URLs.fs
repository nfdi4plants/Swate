namespace Shared

module URLs =

    [<LiteralAttribute>]
    let TermAccessionBaseUrl = @"http://purl.obolibrary.org/obo/"

    /// accession string needs to have format: PO:0007131
    let termAccessionUrlOfAccessionStr (accessionStr:string) =
        let replaced = accessionStr.Replace(":","_")
        TermAccessionBaseUrl + replaced

    [<RequireQualifiedAccessAttribute>]
    module Docs =

        type FileType =
        | Html
        | Yaml
         with
            member this.toStr =
                match this with
                | Html -> ".html"
                | Yaml -> ".yaml"

        
        let private Base = "/docs"

        let OntologyApi (filetype: FileType)= Base + "/IOntologyAPIv2" + filetype.toStr
        

    type Helpdesk =
        static member Url = @"https://support.nfdi4plants.org"

        static member UrlSwateTopic = Helpdesk.Url + "/?topic=Tools_Swate"

        static member UrlOntologyTopic = Helpdesk.Url + "/?topic=Metadata_OntologyUpdate"

        static member UrlTemplateTopic = Helpdesk.Url + "/?topic=Metadata_SwateTemplate"

    [<LiteralAttribute>]
    let AnnotationPrinciplesUrl = @"https://nfdi4plants.github.io/AnnotationPrinciples/"

    [<LiteralAttribute>]
    let SwateWiki = @"https://nfdi4plants.org/nfdi4plants.knowledgebase/docs/implementation/SwateManual/index.html"

    [<LiteralAttribute>]
    let SwateRepo = @"https://github.com/nfdi4plants/Swate"

    [<LiteralAttribute>]
    let CSBTwitterUrl = @"https://twitter.com/cs_biology"

    [<LiteralAttribute>]
    let NFDITwitterUrl = @"https://twitter.com/nfdi4plants"

    [<LiteralAttribute>]
    let CSBWebsiteUrl = @"https://csb.bio.uni-kl.de/"

    let NfdiWebsite = @"https://nfdi4plants.org"