module DocsAnnotationAPIvs1

open Shared
open Giraffe
open Saturn
open Shared
open Shared.DbDomain

open Fable.Remoting.Server
open Fable.Remoting.Giraffe

type ParameterType =
| ParamInteger
| ParamFloat
| ParamString
| ParamBoolean
| ParamUnit
| ParamDateTime
| ParamArray of ParameterType
| ParamOption of ParameterType
| ParamRecordType of Parameter []

    member this.toString =
        match this with
        | ParamInteger              -> "Integer"
        | ParamFloat                -> "Float"
        | ParamString               -> "String"
        | ParamBoolean              -> "Boolean"
        | ParamUnit                 -> "Unit"
        | ParamDateTime             -> "DateTime"
        | ParamArray param          -> sprintf "[ %s ]" param.toString
        | ParamOption param         -> sprintf "%s option" param.toString
        | ParamRecordType paramArr  -> Parameter.arrToString paramArr true

and Parameter = {
    Name : string
    Type : ParameterType
    Desc : string
}
    with
        /// isEnd defines if the single Parameter will be closed with a ',' or not. If isEnd = true then no comma, else comma.
        static member singleToString (param:Parameter) isEnd =
            sprintf
                "
                <div style=\"color:#153b57\">//%s</div>
                <div><span style=\"width: 200px; display: inline-block; color: #155724\">%s</span> : <span style=\"color: #571520\">%s</span>%s</div>
                "
                param.Desc
                param.Name
                param.Type.toString
                (if isEnd then "" else ",")

        member this.toString =
            Parameter.singleToString this true

        /// isRecordType defines if the Parameter array will be closed with a '[]' or '{}'. If isRecordType = true then '{}', else '[]'.
        static member arrToString (paramArr:Parameter []) isRecordType =
            let endInd = paramArr.Length-1
            let singleStrings =
                paramArr
                |> Array.mapi (fun i x ->
                    let isEnd = i = endInd
                    Parameter.singleToString x isEnd
                )
            String.concat "" singleStrings
            |> fun x ->
                if isRecordType then
                    sprintf
                        "
                        {<br>
                        <div style=\"margin-left: 1rem\">%s</div>
                        }"
                        x
                else
                    sprintf
                        "
                        [<br>
                        <div style=\"margin-left: 1rem\">%s</div>
                        ]"
                        x

        static member create name paramType desc =
            {
                Name = name
                Type = paramType
                Desc = desc
            }

module PredefinedParams =

    let dbDomainTerm =
        let dbdomaniTermParamArr = [|
            Parameter.create "ID" ParamInteger ""
            Parameter.create "OntologyId" ParamInteger ""
            Parameter.create "Accession" ParamString ""
            Parameter.create "Name" ParamString ""
            Parameter.create "Definition" ParamString ""
            Parameter.create "XRefValueType" (ParamOption ParamString) ""
            Parameter.create "IsObsolete" ParamBoolean ""
        |]
        dbdomaniTermParamArr
        |> ParamRecordType

    let dbDomainOntology =
        let dbdomaniOntologyParamArr = [|
            Parameter.create "ID" ParamInteger ""
            Parameter.create "Name" ParamString ""
            Parameter.create "CurrentVersion" ParamString ""
            Parameter.create "Definition" ParamString ""
            Parameter.create "DateCreated" ParamDateTime ""
            Parameter.create "UserID" ParamString ""
        |]
        dbdomaniOntologyParamArr
        |> ParamRecordType

    let unitOntology:DbDomain.Ontology = {
        ID = 1L
        Name = "uo"
        CurrentVersion = "releases/2020-03-10"
        Definition = "Unit Ontology"
        DateCreated = System.DateTime(2014,9,4) //"2014-09-04 00:00:00.000000"
        UserID = "gkoutos"
    }


let createDocumentationDescription functionDesc usageDesc (paramArr:Parameter [] option) resultDesc (resultType:Parameter) =
    let prepParams =
        if paramArr.IsSome then Parameter.arrToString paramArr.Value false else "No parameters are passed."
        
    let prepResultParam =
        resultType.toString
    sprintf
        "
            <div><b>Function</b></div>
            <div>%s</div>
            <br>
            <div><b>Usage</b></div>
            <div>%s</div>
            <br>
            <div><b>Parameters</b></div>
            <div>%s</div>
            <br>
            <div><b>Result</b></div>
            <div>%s</div>
            <br>
            <div><b>ResultType</b></div>
            <div>%s</div>
        "
        functionDesc
        usageDesc
        prepParams
        resultDesc
        prepResultParam

let annotatorDocsv1 = Docs.createFor<IAnnotatorAPIv1>()

let annotatorApiDocsv1 =
    Remoting.documentation (sprintf "Annotation API v1") [

        ///////////////////////////////////////////////////////////// Development /////////////////////////////////////////////////////////////
        ////////
        annotatorDocsv1.route <@ fun api -> api.getTestNumber @>
        |> annotatorDocsv1.alias "Get Test Number (<code>getTestNumber</code>"
        |> annotatorDocsv1.description
            (
                createDocumentationDescription
                    "This is used during development to check connection between client and server."
                    ""
                    None
                    "Returns a single integer with a fixed value."
                    (Parameter.create "TestValue" ParamInteger "A single fixed integer value to test connectivity.")
            )

        ////////
        annotatorDocsv1.route <@ fun api -> api.getTestString @>
        |> annotatorDocsv1.alias "Get Test String (<code>getTestString</code>)"
        |> annotatorDocsv1.description
            (
                createDocumentationDescription
                    "This is used during development to check documentation for fsharp option types."
                    ""
                    (Some [|
                        Parameter.create "StringOption" (ParamOption ParamString) ""
                    |])
                    "Returns a single string with a fixed value."
                    (Parameter.create "TestValue" ParamString "A single fixed integer value to test connectivity.")
            )
        |> annotatorDocsv1.example <@ fun api -> api.getTestString (None) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTestString (Some "Hallo ich bin der TestString!") @>

        ///////////////////////////////////////////////////////////// Ontology related requests /////////////////////////////////////////////////////////////
        ////////
        annotatorDocsv1.route <@ fun api -> api.testOntologyInsert @>
        |> annotatorDocsv1.alias "Test Insertion of Ontology into Database (<code>testOntologyInsert</code>)"
        |> annotatorDocsv1.description
            (
                createDocumentationDescription
                    "This is a preview function for a future feature. Right now it only returns an <code>DbDomain.Ontology</code> that <b>would</b> be created."
                    "This is currently not used"
                    (Some [|
                        Parameter.create "OntologyName" ParamString ""
                        Parameter.create "OntologyVersion" ParamString ""
                        Parameter.create "OntologyDefinition" ParamString ""
                        Parameter.create "Created" ParamString "DateTime at which the ontology was created."
                        Parameter.create "User" ParamString "Id of user who posted the ontology."
                    |])
                    "Creates a <code>DbDomain.Ontology</code> from the given params and returns it."
                    (Parameter.create "Ontology" PredefinedParams.dbDomainOntology "A database Ontology entry. This one is not from the database and is currently <b>not</b> created. <code>ID</code> is a set value for this version.")
            )
        |> annotatorDocsv1.example <@ fun api -> api.testOntologyInsert ("TO","releases/testdate","Test Ontology",System.DateTime(2020,11,17),"UserTestId") @>

        ////////
        annotatorDocsv1.route <@ fun api -> api.getAllOntologies @>
        |> annotatorDocsv1.alias "Get All Ontologies (<code>getAllOntologies</code>)"
        |> annotatorDocsv1.description (
            let dbDomainOntologyArr =
                PredefinedParams.dbDomainOntology
                |> ParamArray
            createDocumentationDescription
                "This function is used to get all ontologies in the database."
                "<code>getAllOntologies</code> is executed during app initialization and is needed to allow filtering for a specific ontology in advanced term search."
                None
                "This function returns an array of all Database.Ontology entries in the form of <code>DbDomain.Ontology []</code>."
                (Parameter.create "Ontology []" dbDomainOntologyArr "Array of database Ontology entries.")
        )


        ///////////////////////////////////////////////////////////// Term related requests /////////////////////////////////////////////////////////////
        ////////
        annotatorDocsv1.route <@ fun api -> api.getTermSuggestions @>
        |> annotatorDocsv1.alias "Get Terms (<code>getTermSuggestions</code>)"
        |> annotatorDocsv1.description
            (
                let dbDomainTermArr =
                    PredefinedParams.dbDomainTerm
                    |> ParamArray
                createDocumentationDescription
                    "This function is used to search the Database for Terms by <b>querystring</b>'. It returns <b>n</b> results."
                    "<code>getTermSuggestions</code> is used to search Terms without parent ontology, unit terms and AddBuildingBlock terms."
                    (Some [|
                        (Parameter.create "n" ParamInteger "This parameter sets the number of returned results."                        )
                        (Parameter.create "queryString" ParamString "This parameter is used to search the Term.Name Column for hits."   )
                    |])
                    "This function returns an array of matching Database.Term entries in the form of <code>DbDomain.Term []</code>."
                    (Parameter.create "Term []" dbDomainTermArr "Array of database Term entries.")
            )
        |> annotatorDocsv1.example <@ fun api -> api.getTermSuggestions (5,"micrOTOF-Q") @>

        ////////
        annotatorDocsv1.route <@ fun api -> api.getTermSuggestionsByParentTerm @>
        |> annotatorDocsv1.alias "Get Terms By Parent Ontology (<code>getTermSuggestionsByParentTerm</code>)"
        |> annotatorDocsv1.description (
            createDocumentationDescription
                "This is a <code>getTermSuggestions</code> variant, used to reduce the number of possible hits searching only data that is in a \"is_a\" relation to the parent ontology (written at the top of the column)."
                "If a column with a parent ontology is selected, the app will add this to the 'TermSearch' field in a static button. This can be toggled but a small slider below. If this alternative term search is active then <code>getTermSuggestionsByParentTerm</code> is executed."
                (Some [|
                    Parameter.create "n" ParamInteger "This parameter sets the number of returned results."
                    Parameter.create "queryString" ParamString "This parameter is used to search the Term.Name column for hits."
                    Parameter.create "parentOntology" ParamString "This parameter is used to search <b>only</b> is_a relationships by the parentOntology (Term.Name)."
                |])
                "This function returns an array of matching Database.Term entries in the form of <code>DbDomain.Term []</code>."
                (Parameter.create "Term []" (PredefinedParams.dbDomainTerm |> ParamArray) "Array of database Term entries.")
        )
        |> annotatorDocsv1.example <@ fun api -> api.getTermSuggestionsByParentTerm (5,"micrOTOF-Q","instrument model") @>

        ////////
        annotatorDocsv1.route <@ fun api -> api.getTermsForAdvancedSearch @>
        |> annotatorDocsv1.alias "Get Terms Advanced Search (<code>getTermsForAdvancedSearch</code>)"
        |> annotatorDocsv1.description
            (
                createDocumentationDescription
                    "This is a <code>getTermSuggestions</code> advanced search variant, which can be used to search for specific Terms that might not show up in the top 5 results of the normal term search."
                    "Should a user not be able to find his term of interest, the term suggestions dropdown below the searchfield allows the user to switch to advanced term search."
                    (Some [|
                        Parameter.create "OntologyOption" (ParamOption PredefinedParams.dbDomainOntology) "This parameter can be used to search only in a specific ontology."
                        Parameter.create "searchName" ParamString "This parameter is used to search the <b>Term.Name</b> column for hits."
                        Parameter.create "mustContainName" ParamString "This parameter is used to limit search results to only those with <b>Term.Name</b> containing the subtext in this parameter."
                        Parameter.create "searchDefinition" ParamString "This parameter is used to search the <b>Term.Definition</b> column for hits."
                        Parameter.create "mustContainDefinition" ParamString "This parameter is used to limit search results to only those with <b>Term.Definition</b> containing the subtext in this parameter."
                        Parameter.create "keepObsolete" ParamBoolean "This parameter decides if <b>Term</b>s flagged as obsolete will be filtered out."
                    |])
                    "This function returns an array of matching Database.Term entries in the form of <code>DbDomain.Term []</code>."
                    (Parameter.create "Term []" (PredefinedParams.dbDomainTerm |> ParamArray) "Array of database Term entries.")
            )
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (None                                 , "unit volume", ""        ,""                 ,""     ,true) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (PredefinedParams.unitOntology |> Some, "unit volume", ""        ,""                 ,""     ,true) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (PredefinedParams.unitOntology |> Some, "unit volume", "volume"  ,""                 ,""     ,true) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (PredefinedParams.unitOntology |> Some, "unit volume", "volume"  ,"mass per volume"  ,""     ,true) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (PredefinedParams.unitOntology |> Some, "unit volume", "volume"  ,"mass per volume"  ,"mass" ,true) @>

        ////////
        annotatorDocsv1.route <@ fun api -> api.getUnitTermSuggestions @>
        |> annotatorDocsv1.alias "Get Unit Terms (<code>getUnitTermSuggestions</code>)"
        |> annotatorDocsv1.description (
            let dbDomainTermArr =
                PredefinedParams.dbDomainTerm
                |> ParamArray
            createDocumentationDescription
                "This is a <code>getTermSuggestions</code> variant used specifically to find unit ontology terms."
                "<code>getUnitTermSuggestions</code> is used to find a unit restriction during AddBuildingBlock."
                (Some [|
                    Parameter.create "n" ParamInteger "This parameter sets the number of returned results."
                    Parameter.create "queryString" ParamString "This parameter is used to search the Term.Name Column for hits."
                |])
                "This function returns an array of matching Database.Term entries in the form of <code>DbDomain.Term []</code>. The matching results are restricted to Term.FK_OntologyID = 1 (Unit Ontology)."
                (Parameter.create "Term []" dbDomainTermArr "Array of database Term entries.")
        )
        |> annotatorDocsv1.example <@ fun api -> api.getUnitTermSuggestions (5,"light") @>
        |> annotatorDocsv1.example <@ fun api -> api.getUnitTermSuggestions (5,"temp") @>
    ]

