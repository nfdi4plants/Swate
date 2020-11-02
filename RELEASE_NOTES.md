### 0.0.2-alpha - Work In Progress
* Release of Minimal POC milestone. Rough feature set:
    * Bug fixes:
	    * Responsive design should now render immediatly upon window size change
	    * Add-in should not reload after navigating to a new tab for the first time.

### 0.0.2-alpha - 2020-10-29
* First step on the way to the Minimal POC milestone. Rough feature set:
    * Add fulltext searches for advanced and simple search queries.
    * Upgrade simple search to use a "is_a directed search". This means the search used subterms to already chosen building blocks as default field of search.
    * AddBuildingBlock automatically adds 2 additional hidden terms in which "Term Source REF" and "Term Accession Number" are automatically inserted.
    * For Developers: We added a docker-compose file to generate a local docker mysql database with adminer for an easier developing process. This feature is initialized as part of ```dotnet fake build -t OfficeDebug```

### 0.0.1-alpha - 2020-07-27
* First open alpha release of Swate. Rough feature set:
    * Create annotation tables from existing data
    * Create annotation columns that are conform with our [Annotation Principles draft](https://nfdi4plants.github.io/AnnotationPrinciples/)
    * Autocomplete search for ontology terms. You can use these to either fill cells or annotate column headers
    * Automatic cell formatting for columns that have a unit annotation
    * File picker: open a dialog box to select local files and use their names in the annotation table.