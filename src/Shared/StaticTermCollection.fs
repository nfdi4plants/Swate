module Shared.TermCollection

open ARCtrl

/// <summary>
/// https://github.com/nfdi4plants/nfdi4plants_ontology/issues/85
/// </summary>
let Published = OntologyAnnotation("published","EFO","EFO:0001796")

/// <summary>
/// https://github.com/nfdi4plants/Swate/issues/409#issuecomment-2176134201
/// </summary>
let PublicationStatus = OntologyAnnotation("publication status","EFO","EFO:0001742")

/// <summary>
/// https://github.com/nfdi4plants/Swate/issues/409#issuecomment-2176134201
/// </summary>
let PersonRoleWithinExperiment = OntologyAnnotation("person role within the experiment","AGRO","AGRO:00000378")