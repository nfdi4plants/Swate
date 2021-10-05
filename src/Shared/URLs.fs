namespace Shared

module URLs =

    [<LiteralAttribute>]
    let TermAccessionBaseUrl = @"http://purl.obolibrary.org/obo/"

    /// accession string needs to have format: PO:0007131
    let termAccessionUrlOfAccessionStr (accessionStr:string) =
        let replaced = accessionStr.Replace(":","_")
        TermAccessionBaseUrl + replaced

    [<LiteralAttribute>]
    let Nfdi4psoOntologyUrl = @"https://github.com/nfdi4plants/nfdi4plants_ontology/issues/new/choose"

    [<LiteralAttribute>]
    let AnnotationPrinciplesUrl = @"https://nfdi4plants.github.io/AnnotationPrinciples/"

    [<LiteralAttribute>]
    let DocsFeatureUrl = @"https://github.com/nfdi4plants/Swate/wiki"

    [<LiteralAttribute>]
    let DocsApiUrl = @"/api/IAnnotatorAPIv1/docs"

    /// This will only be needed as long there is no documentation on where to find all api docs.
    /// As soon as that link exists it will replace DocsApiUrl and DocsApiUrl2
    [<LiteralAttribute>]
    let DocsApiUrl2 = @"/api/IServiceAPIv1/docs"

    [<LiteralAttribute>]
    let CSBTwitterUrl = @"https://twitter.com/cs_biology"

    [<LiteralAttribute>]
    let NFDITwitterUrl = @"https://twitter.com/nfdi4plants"

    [<LiteralAttribute>]
    let CSBWebsiteUrl = @"https://csb.bio.uni-kl.de/"