namespace Shared

module URLs =

    module Data =

        module SelectorFormat =

            [<Literal>]
            let csv = @"https://datatracker.ietf.org/doc/html/rfc7111"

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
    let SwateWiki = @"https://nfdi4plants.org/nfdi4plants.knowledgebase/docs/SwateManual/index.html"

    [<LiteralAttribute>]
    let SwateRepo = @"https://github.com/nfdi4plants/Swate"

    [<LiteralAttribute>]
    let CSBTwitterUrl = @"https://twitter.com/cs_biology"

    [<LiteralAttribute>]
    let NFDITwitterUrl = @"https://twitter.com/nfdi4plants"

    [<Literal>]
    let NFDIGitHubUrl = @"https://github.com/nfdi4plants"

    [<LiteralAttribute>]
    let CSBWebsiteUrl = @"https://csb.bio.uni-kl.de/"

    [<LiteralAttribute>]
    let NfdiWebsite = @"https://nfdi4plants.org"

    [<LiteralAttribute>]
    let OntobeeOntologyPrefix = @"https://ontobee.org/ontology/"

