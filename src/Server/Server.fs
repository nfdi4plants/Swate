open System.IO
open System.Threading.Tasks

open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Saturn
open Shared
open Shared.TermTypes

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration

let serviceApi = {
    getAppVersion = fun () -> async { return System.AssemblyVersionInformation.AssemblyVersion }
}

let isaDotNetCommonAPIv1 : IISADotNetCommonAPIv1 =
    let assayFromByteArray (byteArray: byte []) =
        let ms = new MemoryStream(byteArray)
        let jsonStr =
            ISADotNet.XLSX.AssayFile.AssayFile.fromStream ms
        jsonStr
    let investigationFromByteArray (byteArray: byte []) =
        let ms = new MemoryStream(byteArray)
        let jsonStr =
            ISADotNet.XLSX.Investigation.fromStream ms
        jsonStr
    let customXmlFromByteArray (byteArray: byte []) =
        let ms = new MemoryStream(byteArray)
        let jsonStr =
            ISADotNet.XLSX.AssayFile.SwateTable.SwateTable.readSwateTablesFromStream ms
            |> Array.ofSeq
            |> Array.map (fun x -> ISADotNet.JsonExtensions.toString x)
        jsonStr
    {
        /// This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Assay.
        toAssayJSON = fun byteArray -> async {
            let assayJsonString = assayFromByteArray byteArray |> fun (_,_,_,assay) -> ISADotNet.Json.Assay.toString assay
            return assayJsonString
        }
        /// This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Assay with it's customXml.
        toAssayJSONWithCustomXml = fun byteArray -> async {
            failwith "toAssayJSONWithCustomXml is not yet implemented"
            //let swateCustomXmlArr = customXmlFromByteArray byteArray
            //let assay =
            //    assayFromByteArray byteArray
            //    |> fun (_,_,_,assay) ->
            //        let prevCommentList = Option.defaultValue [] assay.Comments
            //        let nextCommentList =
            //            if swateCustomXmlArr |> Array.isEmpty then
                            
            //        ISADotNet.API.Assay.setComments assay 
            return ""
        }
        /// This functions takes an ISA-XLSX file as byte [] and converts it to a ISA-JSON Investigation.
        toInvestigationJSON = fun byteArray -> async {
            let investigationJson = investigationFromByteArray byteArray |> ISADotNet.Json.Investigation.toString
            return investigationJson
        }
        toProcessSeqJSON = fun byteArray -> async {
            let assay = assayFromByteArray byteArray 
            let processJSon = assay |> fun (_,_,_,assay) -> Option.defaultValue "" (Option.map ISADotNet.Json.ProcessSequence.toString assay.ProcessSequence) 
            return processJSon
        }
        toSimplifiedRowMajorJSON = fun byteArray -> async {
            let assay = assayFromByteArray byteArray 
            let processJSon = assay |> fun (_,_,_,assay) -> assay |> (ISADotNet.Json.AssayCommonAPI.RowWiseAssay.fromAssay >> ISADotNet.Json.AssayCommonAPI.RowWiseAssay.toString) 
            return processJSon
        }
        testPostNumber = fun num -> async {
            let res = $"Hey you just sent us a number. Is this your number {num}?"
            return res
        }
        getTestNumber = fun () -> async {
            return "42"
        }
    }

let annotatorApi cString = {
    //Development
    getTestNumber = fun () -> async { return 42 }

    //Ontology related requests
    testOntologyInsert = fun (name,version,created,user) ->
        async {
            /// Don't allow users to access this part!! At least for now
            //let createdEntry = OntologyDB.insertOntology cString name version definition created user
            let onto =
                DbDomain.createOntology 
                    name
                    version
                    created
                    user
            printfn "created pseudo ontology entry: \t%A. No actual db insert has happened." onto
            return onto
        }

    getAllOntologies = fun () ->
        async {
            let results = OntologyDB.getAllOntologies cString ()
            return results
        }

    // Term related requests
    getTermSuggestions = fun (max:int,typedSoFar:string) ->
        async {
            let searchRes =
                match typedSoFar with
                | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                    OntologyDB.getTermByAccession cString foundAccession
                | _ ->
                    let like = OntologyDB.getTermSuggestions cString (typedSoFar)
                    let searchSet = typedSoFar |> Suggestion.createBigrams
                    like
                    |> Array.sortByDescending (fun sugg ->
                            Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
                    |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)
            return searchRes
        }

    getTermSuggestionsByParentTerm = fun (max:int,typedSoFar:string,parentTerm:TermMinimal) ->
        async {

            let searchRes =
                match typedSoFar with
                | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                    OntologyDB.getTermByAccession cString foundAccession
                | _ ->
                    let like =
                        if parentTerm.TermAccession = ""
                        then
                            OntologyDB.getTermSuggestionsByParentTerm cString (typedSoFar,parentTerm.Name)
                        else
                            OntologyDB.getTermSuggestionsByParentTermAndAccession cString (typedSoFar,parentTerm.Name,parentTerm.TermAccession)
                    let searchSet = typedSoFar |> Suggestion.createBigrams
                    like
                    |> Array.sortByDescending (fun sugg ->
                            Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
                    
                    |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)
            return searchRes
        }

    getAllTermsByParentTerm = fun (parentTerm:TermMinimal) ->
        async {
            let searchRes =
                OntologyDB.getAllTermsByParentTermOntologyInfo cString parentTerm

            return searchRes  
        }

    getTermSuggestionsByChildTerm = fun (max:int,typedSoFar:string,childTerm:TermMinimal) ->
        async {

            let searchRes =
                match typedSoFar with
                | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                    OntologyDB.getTermByAccession cString foundAccession
                | _ ->
                    let like =
                        if childTerm.TermAccession = ""
                        then
                            OntologyDB.getTermSuggestionsByChildTerm cString (typedSoFar,childTerm.Name)
                        else
                            OntologyDB.getTermSuggestionsByChildTermAndAccession cString (typedSoFar,childTerm.Name,childTerm.TermAccession)
                    let searchSet = typedSoFar |> Suggestion.createBigrams
                    like
                    |> Array.sortByDescending (fun sugg ->
                            Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
                    
                    |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)

            return searchRes
        }

    getAllTermsByChildTerm = fun (childTerm:TermMinimal) ->
        async {
            let searchRes =
                OntologyDB.getAllTermsByChildTermOntologyInfo cString childTerm

            return searchRes  
        }

    getTermsForAdvancedSearch = fun (ontOpt,searchName,mustContainName,searchDefinition,mustContainDefinition,keepObsolete) ->
        async {
            let result =
                let searchSet = searchName + mustContainName + searchDefinition + mustContainDefinition|> Suggestion.createBigrams
                OntologyDB.getAdvancedTermSearchResults cString ontOpt searchName mustContainName searchDefinition mustContainDefinition keepObsolete
                |> Array.sortByDescending (fun sugg ->
                    Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
            return result
        }

    getUnitTermSuggestions = fun (max:int,typedSoFar:string, unit:UnitSearchRequest) ->
        async {
            let searchRes =
                match typedSoFar with
                | Regex.Aux.Regex Regex.Pattern.TermAccessionPattern foundAccession ->
                    OntologyDB.getTermByAccession cString foundAccession
                | _ ->
                    let like = OntologyDB.getUnitTermSuggestions cString (typedSoFar)
                    let searchSet = typedSoFar |> Suggestion.createBigrams
                    like
                    |> Array.sortByDescending (fun sugg ->
                            Suggestion.sorensenDice (Suggestion.createBigrams sugg.Name) searchSet
                    )
                
                    |> fun x -> x |> Array.take (if x.Length > max then max else x.Length)

            return (searchRes, unit)
        }

    getTermsByNames = fun (queryArr) ->
        async {
            let result =
                queryArr |> Array.map (fun searchTerm ->
                    {searchTerm with
                        SearchResultTerm =
                            // check if search string is empty. This case should delete TAN- and TSR- values in table
                            if searchTerm.Term.Name = "" then None
                            // check if term accession was found. If so search also by this as it is unique
                            elif searchTerm.Term.TermAccession <> "" then
                                let searchRes = OntologyDB.getTermByNameAndAccession cString (searchTerm.Term.Name,searchTerm.Term.TermAccession)
                                if Array.isEmpty searchRes then
                                    None
                                else
                                    searchRes |> Array.head |> Some
                            // check if parent term was found and try find term via parent term
                            elif searchTerm.ParentTerm.IsSome then
                                let searchRes = OntologyDB.getTermByParentTermOntologyInfo cString (searchTerm.Term.Name,searchTerm.ParentTerm.Value)
                                if Array.isEmpty searchRes then
                                    // if no term can be found by is_a directed search do standard search by name
                                    // no need to search for name and accession, as accession is the clearly defines a term and is checked in the if branch above.
                                    let searchRes' = OntologyDB.getTermByName cString searchTerm.Term.Name
                                    if Array.isEmpty searchRes' then None else searchRes' |> Array.head |> Some
                                else
                                    searchRes |> Array.head |> Some
                            // if term is a unit it should be contained inside the unit ontology, if not it is most likely free text input.
                            elif searchTerm.IsUnit then
                                let searchRes = OntologyDB.getTermByNameAndOntology cString (searchTerm.Term.Name,"uo")
                                if Array.isEmpty searchRes then
                                    None
                                else
                                    searchRes |> Array.head |> Some
                            // if none of the above apply we do a standard term search
                            else
                                let searchRes = OntologyDB.getTermByName cString searchTerm.Term.Name
                                if Array.isEmpty searchRes then None else searchRes |> Array.head |> Some
                    }
                )
            return result
        }

    getAllProtocolsWithoutXml = fun () -> async {
        let protocols = ProtocolDB.getAllProtocols cString
        return protocols
    }

    getProtocolByName = fun prot -> async { return ProtocolDB.getProtocolByName cString prot }

    getProtocolsByName = fun (names) -> async {
        let prot = names |> Array.map (fun x -> ProtocolDB.getProtocolByName cString x)
        //let protsWithXml = protsWithoutXml |> Array.map (ProtocolDB.getXmlByProtocol cString)
        return prot
    }

    increaseTimesUsed = fun templateName -> async {
        ProtocolDB.increaseTimesUsed cString templateName
        return ()
    }
}

let createIAnnotatorApiv1 cString =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue (annotatorApi cString)
    |> Remoting.withDocs Shared.URLs.DocsApiUrl DocsAnnotationAPIvs1.annotatorApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    ) 
    |> Remoting.buildHttpHandler

let createIServiceAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue serviceApi
    |> Remoting.withDocs Shared.URLs.DocsApiUrl2 DocsServiceAPIvs1.serviceApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    )
    |> Remoting.buildHttpHandler

let createISADotNetCommonAPIv1 =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue isaDotNetCommonAPIv1
    |> Remoting.withDocs "/api/IISADotNetCommonAPIv1/docs" DocsISADotNetAPIvs1.isaDotNetCommonApiDocsv1
    |> Remoting.withDiagnosticsLogger(printfn "%A")
    |> Remoting.withErrorHandler(
        (fun x y -> Propagate (sprintf "[SERVER SIDE ERROR]: %A @ %A" x y))
    )
    |> Remoting.buildHttpHandler


/// due to a bug in Fable.Remoting this does currently not work as inteded and is ignored. (https://github.com/Zaid-Ajaj/Fable.Remoting/issues/198)
let mainApiController = router {

    //
    forward @"/IAnnotatorAPIv1" (fun next ctx ->
        let cString = 
            let settings = ctx.GetService<IConfiguration>()
            settings.["Swate:ConnectionString"]
        createIAnnotatorApiv1 cString next ctx
    )

    //
    forward @"/IServiceAPIv1" (fun next ctx ->
        createIServiceAPIv1 next ctx
    )
}

let topLevelRouter = router {
    get "/test/test1" (htmlString "<h1>Hi this is test response 1</h1>")
    //forward "/api" mainApiController
    forward @"" (fun next ctx ->
        let cString = 
            let settings = ctx.GetService<IConfiguration>()
            settings.["Swate:ConnectionString"]
        createIAnnotatorApiv1 cString next ctx
    )

    //
    forward @"" (fun next ctx ->
        createIServiceAPIv1 next ctx
    )

    
    forward @"" (fun next ctx ->
        createISADotNetCommonAPIv1 next ctx
    )
}

let app = application {
    url "http://localhost:5000/"
    use_router topLevelRouter
    memory_cache
    //logging 
    use_static "public"
    use_gzip
    logging (fun (builder: ILoggingBuilder) -> builder.SetMinimumLevel(LogLevel.Debug) |> ignore)
}

app
    .ConfigureAppConfiguration(
        System.Action<Microsoft.Extensions.Hosting.HostBuilderContext,IConfigurationBuilder> ( fun ctx config ->
            config.AddUserSecrets("6de80bdf-2a05-4cf7-a1a8-d08581dfa887") |> ignore
            config.AddJsonFile("production.json",true,true)  |> ignore
            config.AddJsonFile("dev.json",true,true)  |> ignore
        )
)
|> run

