### 0.1.3+5abee29 (Released 2020-12-2)
* Additions:
    * latest commit #5abee29
    * [[#13f3639](https://github.com/nfdi4plants/Swate/commit/13f3639c7181292ccc3764e2b31d5ae91f1f4dcf)] Create issue templates
    * [[#5abee29](https://github.com/nfdi4plants/Swate/commit/5abee298f349005b609ebc21eaf70a0e76e5c5d8)] Add Unit Column when selecting a unit for a term (Issue #48).
    * [[#5abee29](https://github.com/nfdi4plants/Swate/commit/5abee298f349005b609ebc21eaf70a0e76e5c5d8)] Add option to fill hidden cols according to main column (Issue #67).
    * [[#5abee29](https://github.com/nfdi4plants/Swate/commit/5abee298f349005b609ebc21eaf70a0e76e5c5d8)] Add input assist to delete hidden col cells onChange of main col (Issue #68).

### 0.1.2+af67a92 (Released 2020-11-26)
* Additions:
    * latest commit #af67a92
    * [[#af67a92](https://github.com/nfdi4plants/Swate/commit/af67a924a0ec5593573e1f7a5a830f0beb7cf0cd)] Replace footer placeholder.
    * [[#6a423b3](https://github.com/nfdi4plants/Swate/commit/6a423b385b9b1590bd0bb97cb76afc6dedd4873d)] Add button to create a new annotation table.
    * [[#9a3ea60](https://github.com/nfdi4plants/Swate/commit/9a3ea60476baccdf49d0bd7c4839b00b6b52627f)] Add automated Versioning and release note creation (Issue #44).
    * [[#40000ef5](https://github.com/nfdi4plants/Swate/commit/ffd82de928528179f05ba88e5a45a55894af66ac)] Add fake target to draft github release from RELEASE_NOTES.md (Issue #44).
* Bugfixes:
    * [[#648f8b6](https://github.com/nfdi4plants/Swate/commit/648f8b63526e16f4e155833d0504e0b415f666c5)] Fix multiple worksheets/annotation tables bug (Issue #58).
    * [[#05f4c39](https://github.com/nfdi4plants/Swate/commit/05f4c39a4eb19c19159d9782d56e9afad43f4286)] Fix font, as the correct scss was not loaded correctly.
    * [[#c6e543b](https://github.com/nfdi4plants/Swate/commit/c6e543bf3844b165f77f272aa6b38f6894da88cb)] Fix inconsistencies in building block has-unit functioniality.
    * [[#fadbea8](https://github.com/nfdi4plants/Swate/commit/fadbea8337eb6ae304085f6f1fbbd4df99d8003f)] Fix disappearing checkboxes (Issue #54).
    * [[#c402c70](https://github.com/nfdi4plants/Swate/commit/c402c7022bf120ae6b6a25822799aec8d80b2e7b)] Fix api docs not showing examples with DateTime (Issue #55).

### 0.1.1+7c567fd (Released 2020-11-18)
* Additions:
    * #7c567fd
    * Allow for multiples of the same column.
    * Implement basic validation system for current worksheet. (WIP)
    * Add info page with social media links and contact.
    * Add extensive api docs.
* Bugfixes:
    * Unit Term Search broke due to a change in naming conventions in the stored procedures. Fixed it!

### v0.1-beta - 2020-11-05
* Release of [Minimal POC milestone](https://github.com/nfdi4plants/Swate/milestone/1?closed=1). Rough feature set:
    * Update advanced term search to use stored procedure introduced in 0.0.2-alpha.
    * Bugfixes:
	    * Responsive design should now render immediatly upon window size change
	    * Add-in should not reload after navigating to a new tab for the first time.
        * Term search input field no longer looses focus after clicking into it.

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
