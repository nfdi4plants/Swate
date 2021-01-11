module DocsAnnotationAPIvs1

open Shared
open Giraffe
open Saturn
open Shared
open Shared.DbDomain

open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open DocsFunctions

let annotatorDocsv1 = Docs.createFor<IAnnotatorAPIv1>()

let annotatorApiDocsv1 =
    Remoting.documentation (sprintf "Annotation API v1") [

        ///////////////////////////////////////////////////////////// Development /////////////////////////////////////////////////////////////
        ////////
        annotatorDocsv1.route <@ fun api -> api.getTestNumber @>
        |> annotatorDocsv1.alias "Get Test Number (<code>getTestNumber</code>)"
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
                    "This is used during development to test documentation for fsharp."
                    ""
                    (Some [|
                        Parameter.create "TestParam" (ParamDateTime) "This Param will often change during development"
                    |])
                    "The result will contain the TestParam of some sort."
                    (Parameter.create "TestValue" ParamString "")
            )
        |> annotatorDocsv1.example <@ fun api -> api.getTestString (System.DateTime(2020,11,17)) @>

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
                    (Parameter.create "Ontology" PredefinedParams.OntologyType "A database Ontology entry. This one is not from the database and is currently <b>not</b> created. <code>ID</code> is a set value for this version.")
            )
        |> annotatorDocsv1.example <@ fun api -> api.testOntologyInsert ("TO","releases/testdate","Test Ontology",PredefinedParams.Examples.test,"UserTestId") @>

        ////////
        annotatorDocsv1.route <@ fun api -> api.getAllOntologies @>
        |> annotatorDocsv1.alias "Get All Ontologies (<code>getAllOntologies</code>)"
        |> annotatorDocsv1.description (
            let dbDomainOntologyArr =
                PredefinedParams.OntologyType
                |> ParamArray
            createDocumentationDescription
                "This function is used to get all ontologies in the database."
                "<code>getAllOntologies</code> is executed during app initialization and is needed to allow filtering for a specific ontology in advanced term search."
                None
                "This function returns an array of all Database.Ontology entries in the form of <code>DbDomain.Ontology []</code>."
                (Parameter.create "Ontology []" dbDomainOntologyArr "Array of database Ontology entries.")
        )
        |> annotatorDocsv1.example <@ fun api -> api.getAllOntologies () @>

        ///////////////////////////////////////////////////////////// Term related requests /////////////////////////////////////////////////////////////
        ////////
        annotatorDocsv1.route <@ fun api -> api.getTermSuggestions @>
        |> annotatorDocsv1.alias "Get Terms (<code>getTermSuggestions</code>)"
        |> annotatorDocsv1.description
            (
                let dbDomainTermArr =
                    PredefinedParams.TermType
                    |> ParamArray
                createDocumentationDescription
                    "This function is used to search the Database for Terms by <b>querystring</b>'. It returns <b>n</b> results."
                    "<code>getTermSuggestions</code> is used to search Terms without parent ontology and AddBuildingBlock terms.
                    From release 0.1.4 onwards this function will try to parse the <b>querystring</b> to a <code>Term.Accession</code> value.
                    If successful it will search the database for <code>Term.Accession</code> = <b>querystring</b>, allowing a search by accession number."
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
                (Parameter.create "Term []" (PredefinedParams.TermType |> ParamArray) "Array of database Term entries.")
        )
        |> annotatorDocsv1.example <@ fun api -> api.getTermSuggestionsByParentTerm (5,"micrOTOF-Q","instrument model") @>

        ////////
        annotatorDocsv1.route <@ fun api -> api.getTermsForAdvancedSearch @>
        |> annotatorDocsv1.alias "Get Terms Advanced Search (<code>getTermsForAdvancedSearch</code>)"
        |> annotatorDocsv1.description
            (
                createDocumentationDescription
                    "<code>getTermsForAdvancedSearch</code>) can be used to search for specific Terms that might not show up in the top 5 results of the normal term search."
                    "Should a user not be able to find his term of interest, the term suggestions dropdown below the searchfield allows the user to switch to advanced term search."
                    (Some [|
                        Parameter.create "OntologyOption" (ParamOption PredefinedParams.OntologyType) "This parameter can be used to search only in a specific ontology."
                        Parameter.create "searchName" ParamString "This parameter is used to search the <b>Term.Name</b> column for hits."
                        Parameter.create "mustContainName" ParamString "This parameter is used to limit search results to only those with <b>Term.Name</b> containing the subtext in this parameter."
                        Parameter.create "searchDefinition" ParamString "This parameter is used to search the <b>Term.Definition</b> column for hits."
                        Parameter.create "mustContainDefinition" ParamString "This parameter is used to limit search results to only those with <b>Term.Definition</b> containing the subtext in this parameter."
                        Parameter.create "keepObsolete" ParamBoolean "This parameter decides if <b>Term</b>s flagged as obsolete will be filtered out."
                    |])
                    "This function returns an array of matching Database.Term entries in the form of <code>DbDomain.Term []</code>."
                    (Parameter.create "Term []" (PredefinedParams.TermType |> ParamArray) "Array of database Term entries.")
            )
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (None                                 , "unit volume", ""        ,""                 ,""     ,true) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (PredefinedParams.Examples.unitOntologyExmp |> Some, "unit volume", ""        ,""                 ,""     ,true) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (PredefinedParams.Examples.unitOntologyExmp |> Some, "unit volume", "volume"  ,""                 ,""     ,true) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (PredefinedParams.Examples.unitOntologyExmp |> Some, "unit volume", "volume"  ,"mass per volume"  ,""     ,true) @>
        |> annotatorDocsv1.example <@ fun api -> api.getTermsForAdvancedSearch (PredefinedParams.Examples.unitOntologyExmp |> Some, "unit volume", "volume"  ,"mass per volume"  ,"mass" ,true) @>

        ////////
        annotatorDocsv1.route <@ fun api -> api.getUnitTermSuggestions @>
        |> annotatorDocsv1.alias "Get Unit Terms (<code>getUnitTermSuggestions</code>)"
        |> annotatorDocsv1.description (
            let dbDomainTermArr =
                PredefinedParams.TermType
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

        annotatorDocsv1.route <@ fun api -> api.getTermsByNames @>
        |> annotatorDocsv1.alias "Get Unit Terms (<code>getTermsByNames</code>)"
        |> annotatorDocsv1.description (
            createDocumentationDescription
                "This function uses the given <code>SearchTermI []</code> to try and find one search result for each member of <code>SearchTermI []</code> for the
                <code>SearchTermI.SearchString</code> on Term.Name in the database.
                The search result is then passed into the same SearchTerm type as <code>SearchTermI.TermOpt</code> and returned to the client."
                "<code>getTermsByNames</code> is used to find Terms in the database that fit the entries in the excel annotation table. The necessary information (col-, row-index
                is stored in the SearchTermI type. One array element for each distinct value found in the table."
                (Some [|
                    Parameter.create "SearchTermI []" (PredefinedParams.SearchTermType |> ParameterType.ParamArray) ""
                |])
                "Returns the parameter array posted to it with the search result in <code>SearchTerm.TermOpt</code>."
                (Parameter.create "SearchTermI []" (PredefinedParams.SearchTermType |> ParameterType.ParamArray) "")
        )
        |> annotatorDocsv1.example <@ fun api -> api.getTermsByNames ([|PredefinedParams.Examples.searchTermExmp|]) @>
    ]

