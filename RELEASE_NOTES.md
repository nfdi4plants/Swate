### 0.6.0+8dfa9e2 (Released 2022-3-22)
* Additions:
    * latest commit #8dfa9e2
    * [[#8dfa9e2](https://github.com/nfdi4plants/Swate/commit/8dfa9e2f0dfa9e5a42e8030adac041581d549913)] Heavily improve term insert feedback in annotation table (Issue #149, #161)
    * [[#82b896c](https://github.com/nfdi4plants/Swate/commit/82b896c152eee2b411ef8ab506b7932694144f92)] Clean up readme :shower:
    * [[#18641cd](https://github.com/nfdi4plants/Swate/commit/18641cd50a5d7854aaf2a39976479d4cda2d12e7)] Update fill reference column logic, to provide consistent results.
    * [[#2b2413f](https://github.com/nfdi4plants/Swate/commit/2b2413f3f051b096dde8d8322bbafaed6d4ac616)] Implement and/or slider for template tag filter (Issue #195) :sparkles:.
    * [[#bcf077c](https://github.com/nfdi4plants/Swate/commit/bcf077c6b910d9c8c2bdd8402e8953f2f34af657)] Update info page :lipstick: and contact links (Issue #196).
    * [[#f776367](https://github.com/nfdi4plants/Swate/commit/f7763677a81e3913468d2a8c9304cc7da604d5d9)] Update project for docker build automation :whale:
    * [[#4dc45b7](https://github.com/nfdi4plants/Swate/commit/4dc45b7c882b0fe1ccf6f5391bb3a7fcc6b5f0cc)] Auto insert table for database templates (Issue #146).
    * [[#822d375](https://github.com/nfdi4plants/Swate/commit/822d375be01fea946faf3601d8f90dfa9ead169e)] Switch to neo4j database :sparkles:.
    * [[#432c14b](https://github.com/nfdi4plants/Swate/commit/432c14b33dee96bfa1074d0078e5dda04b4b6b9b)] Improve parent-child search performance (Issue #117,#193) :racehorse:.
    * [[#7b360e8](https://github.com/nfdi4plants/Swate/commit/7b360e862b19e42eddca47f4ca8245577296c24d)] Update Advanced term search for cleaner input.
    * [[#bfec630](https://github.com/nfdi4plants/Swate/commit/bfec63094cbc78cb28644d53df8d654cde087eb4)] Persist active tabs over subpages (Issue #191) :lipstick:.
    * [[#92d973c](https://github.com/nfdi4plants/Swate/commit/92d973cd6a33144c2eafe52eb84b7b52c1fbea15)] Update navbar burger menu to always be visible (Issue #194).
    * [[#bd8e3fb](https://github.com/nfdi4plants/Swate/commit/bd8e3fb946cbe956a730de58508b3a94ae2c27d4)] Separate ER tags from other tags and add curated vs community templates badge (#187, #186).
* Bugfixes:
    * [[#5e45ac8](https://github.com/nfdi4plants/Swate/commit/5e45ac8cbb8a6a031bafeaf49c407b7fd8ee3ba7)] Fix propagation of unit to new building blocks (Issue #183) :bug:.
    * [[#d63e668](https://github.com/nfdi4plants/Swate/commit/d63e668056d3612c5e40f1c93ab9b65238aad342)] Fix bug in template.xlsx to template.json parsing :bug:
    * [[#de99630](https://github.com/nfdi4plants/Swate/commit/de99630332c1b12925fdc4c534a105652de27c4d)] Fix bug not incrementing timesUsed for templates :bug:

### 0.5.3+0eaa644 (Released 2021-12-16)
* Additions:
    * latest commit #0eaa644
* Bugfixes:
    * [[#85c47e0](https://github.com/nfdi4plants/Swate/commit/85c47e0cefe40e038fbcd93ec360f92e5ec6efb2)] Update installer (Issue #181).
    * [[#0eaa644](https://github.com/nfdi4plants/Swate/commit/0eaa6446e70d858720a1ea3428083cd3b379360a)] Fix accidental value insert from template db.
    * [[#2039287](https://github.com/nfdi4plants/Swate/commit/20392878a667a6c563a389078629f3526766c4a3)] Fix templates only inserting as characteristic.

### 0.5.2+abcc754 (Released 2021-12-14)
* Additions:
    * latest commit #abcc754
    * [[#681d9c9](https://github.com/nfdi4plants/Swate/commit/681d9c9ef7ac4afb12f912ad99f1b5f0f288c26f)] Add new installer using shared folders! This allows Swate installation without the microsoft store and any dependencies.
    * [[#9154dd5](https://github.com/nfdi4plants/Swate/commit/9154dd5eacc20cc5e6be78b554f1226abc298996)] Split Swate.Core and Experts into different add-ins :tada:
* Bugfixes:
    * [[#abcc754](https://github.com/nfdi4plants/Swate/commit/abcc754fddc57b9f9785670569ef352fb5548e56)] **Try** parse number format (fix Issue #180).

### 0.5.1+0c90a83 (Released 2021-12-8)
* Additions:
    * latest commit #0c90a83
    * Add support for Excel 2019 :tada:
    * [[#48d8d61](https://github.com/nfdi4plants/Swate/commit/48d8d61409d1404b9bd254241e54742ced36b357)] Replace drag and drop for filepicker with simplified table sorting element
    * [[#e16c861](https://github.com/nfdi4plants/Swate/commit/e16c8613acde31738ecf0af09bee503feea7246a)] Add whitespace between authors.
    * [[#7b0884f](https://github.com/nfdi4plants/Swate/commit/7b0884f9d2c76bae5d59148acecd7b4165739587)] add pure json return for common api
* Bugfixes:
    * [[#2a09a55](https://github.com/nfdi4plants/Swate/commit/2a09a55120261db8527b1c80e63a50cb0ecfe4e3)] Split template tags by "," instead of ";"
    * [[#c82dd4f](https://github.com/nfdi4plants/Swate/commit/c82dd4f3ef72a21af8f0896ea0cb103811f2c198)] Fix server port error for production.

### 0.5.0+70632951 (Released 2021-10-22)
* Additions:
    * latest commit #70632951
    * Column headers slimmed, only term accession number in brackets
        * Unit section slimmed from three columns to one
        * Units tracked with number section in Excel
        * Only terms from unit ontology (UO) allowed
    * Protocol insert is now called template insert and more generalized
        * Building blocks can only be inserted once
        * When trying to insert duplicate template no error is thrown, but no building block duplicates are inserted.
    * [[#6c62234](https://github.com/nfdi4plants/Swate/commit/6c62234aeda2d232a6054b278fff4785d8902aba)] Add visualization for source-protocol-sample chains :sparkles:
    * [[#d080748](https://github.com/nfdi4plants/Swate/commit/d080748dd90510f676aefc43aec6905bd3539830)] Add import from json to **multiple** annotation tables :sparkles:
    * [[#16ec188](https://github.com/nfdi4plants/Swate/commit/16ec1880b99d1d0b102d965f413c29ef47fe1669)] Improve server side error feedback.
    * [[#e63bffc](https://github.com/nfdi4plants/Swate/commit/e63bffc767f61939e09b7f2f4f4cfac950babe21)] Make protocol preview table scrollable :lipstick:
    * [[#8d83591](https://github.com/nfdi4plants/Swate/commit/8d83591f25ad20f9497db166072e4b1bc8e37ddf)] Improve template insert performance.
    * [[#f6ebef3](https://github.com/nfdi4plants/Swate/commit/f6ebef39294b76a762bc15426fde657e4cb25e40)] Allow each building block only once per sheet.
    * [[#2cbbaef](https://github.com/nfdi4plants/Swate/commit/2cbbaef34888a8d7db6bde06ef30eaa4ff8705bd)] Allow only one output column type per table.
    * [[#1e0a3f4](https://github.com/nfdi4plants/Swate/commit/1e0a3f41103082408ebec21e2782437fcd46d0ba)] Update table name generator.
    * [[#6138c65](https://github.com/nfdi4plants/Swate/commit/6138c654002b16f40225747929be39108dcab011)] Update visuals :sparkles: (Issue #162).
    * [[#cc81c47](https://github.com/nfdi4plants/Swate/commit/cc81c476c1d9e722d189f1392433a5b60b688097)] Update name for protocol templates (Issue #153).
    * [[#4187c99](https://github.com/nfdi4plants/Swate/commit/4187c993f08f461c7df8210886c2f55d96e8b586)] Improve building block info (Issue #160).
    * [[#25e0253](https://github.com/nfdi4plants/Swate/commit/25e02539f944ba93ab094b6f1c488b19b811e13a)] Refactor Settings :hammer:
    * [[#ff96e2a](https://github.com/nfdi4plants/Swate/commit/ff96e2a58e0ec8f9252f07acb284e7a23d3474f3)] Improve client logging.
    * [[#c885c0c](https://github.com/nfdi4plants/Swate/commit/c885c0c14d85bfc7424d4f800cb490300af816e5)] Track template metadata with worksheet and provide ease-of-access function.
    * [[#8df5246](https://github.com/nfdi4plants/Swate/commit/8df52468cd903273fcc81c676431122764a77857)] Add hide-reference-columns option to autofit table :sparkles:.
    * [[#db9b9e1](https://github.com/nfdi4plants/Swate/commit/db9b9e12787e672bd3ec88553118aa2ca6acb30f)] Add annotationTable create with prev output auto-insert (Issue #168).
    * [[#b07aca5](https://github.com/nfdi4plants/Swate/commit/b07aca5783dde95009853a9aaad00d4c679f9763)] Add option to export Swate tables as json files.
    * [[#c020fea](https://github.com/nfdi4plants/Swate/commit/c020fea3722d08bde5a2aeea2fe52277fd3fd2ee)] Add json export from external xlsx files :sparkles:
    * [[#b7f9920](https://github.com/nfdi4plants/Swate/commit/b7f9920dfe7653a482ee5af44687288eb7c9287d)] Add Common API to backend.
    * [[#e14c648](https://github.com/nfdi4plants/Swate/commit/e14c6482488c3755254f038015c6b2aba2b55d05)] Update Protocol search and filter functionality.
    * [[#7194b96](https://github.com/nfdi4plants/Swate/commit/7194b96c386b665c7fd3ef254258b48d5a96d8f7)] Start updating unit to ISA conformity :fire:
    * [[#6ba34f2](https://github.com/nfdi4plants/Swate/commit/6ba34f2268267e4c7686f547dce707e8b4b93700)] Improve performance with update to SAFE stack v3.
* Deletions:
    * [[#8302630](https://github.com/nfdi4plants/Swate/commit/830263002d8429ec5016fefc91ec100228e9c496)] Remove definition field from ontologies.
* Bugfixes:
    * [[#8f33b3b](https://github.com/nfdi4plants/Swate/commit/8f33b3b6e1a62f7d94e023cc791bd7e332369f1a)] Fix index error in json exporting rows :bug:
    * [[#0fb73e2](https://github.com/nfdi4plants/Swate/commit/0fb73e2b2786e2b9dfbf12ed17d983809b7cbabf)] Update navbar to stay fixed.
    * [[#6afcd91](https://github.com/nfdi4plants/Swate/commit/6afcd9102bd3e858c46621dda3e6fc804abc6afe)] Fix context update issues in interop functions.
    * [[#4fb63e9](https://github.com/nfdi4plants/Swate/commit/4fb63e9767da0fe82479c42cbd13b9f0c89b0209)] Fix warning modal when just entering validation subpage :bug:

### 0.4.8+7960150 (Released 2021-9-8)
* Additions:
    * latest commit #7960150
    * [[#4796598](https://github.com/nfdi4plants/Swate/commit/47965986e914f30b3b438bc44ede81308dc16d39)] Update SQL db dump for development.
    * [[#78458db](https://github.com/nfdi4plants/Swate/commit/78458db2338863907ce258111511a7c8ad9232bb)] Merge pull request #144 from nfdi4plants/kevinf-patch-0.4.7
    * [[#5b46ded](https://github.com/nfdi4plants/Swate/commit/5b46deda34e38fbd04f0f6f5693aa1a0bd9cfca6)] Update README.md
    * [[#ef96958](https://github.com/nfdi4plants/Swate/commit/ef96958b4d0929f80273ddc7b90659274bdbcc91)] Update for demo server

### 0.4.7+23edde8 (Released 2021-5-12)
* Additions:
    * latest commit #23edde8
    * [[#23edde8](https://github.com/nfdi4plants/Swate/commit/23edde88061148ee37a038a44324f338d3d4ee88)] Add redo search on double click to building block input elements.
    * [[#b10131b](https://github.com/nfdi4plants/Swate/commit/b10131b5a9957824f8957227ff8b192434adc62e)] Update database, add part_of Term relationships

### 0.4.6+89aa7bc (Released 2021-3-12)
* Additions:
    * latest commit #89aa7bc
    * [[#89aa7bc](https://github.com/nfdi4plants/Swate/commit/89aa7bc8a2e31b8598e0e0b916733f211035fa4c)] Add Spawn API endpoints.

### 0.4.5+b360273 (Released 2021-3-12)
* Additions:
    * latest commit #b360273
    * [[#d630a76](https://github.com/nfdi4plants/Swate/commit/d630a76445f1845bb93de9ae89bf1e983f81c51f)] Save darkmode as cookie and improve darkmode (Issue #134).
* Bugfixes:
    * [[#b360273](https://github.com/nfdi4plants/Swate/commit/b36027369f60a612e2660ec3b8bfefb09cbe5664)] Fix bug, removing protocol groups if one column is not an ontology.
    * [[#402a220](https://github.com/nfdi4plants/Swate/commit/402a220824ee69ebd1cb5e6f4ecd2ba4cf1627f7)] Fix pointer .json generator (Issue #139).

### 0.4.4+4ae3198 (Released 2021-3-11)
* Additions:
    * latest commit #4ae3198
    * [[#4ae3198](https://github.com/nfdi4plants/Swate/commit/4ae31986bc4b1a24ccead037afbde4ced9ed7a44)] Update DB protocol.

### 0.4.3+9b7d7fe (Released 2021-3-9)
* Additions:
    * latest commit #9b7d7fe
* Bugfixes:
    * [[#9b7d7fe](https://github.com/nfdi4plants/Swate/commit/9b7d7fe5ca562fee346227430c1fc1aebffa3b10)] Remove bugs with protocol update :bug:

### 0.4.2+ae04aa5 (Released 2021-3-9)
* Additions:
    * latest commit #ae04aa5
* Bugfixes:
    * [[#ae04aa5](https://github.com/nfdi4plants/Swate/commit/ae04aa51261e614d0c422f03d646e1a76d664501)] Stabilize protocol insert against bugs :bug:

### 0.4.1+d75743c (Released 2021-3-8)
* Additions:
    * latest commit #d75743c
    * [[#0d9c945](https://github.com/nfdi4plants/Swate/commit/0d9c94558052d7f13e9707da6202e3a3f34440b9)] Add links to template repository.
    * [[#6b5a56f](https://github.com/nfdi4plants/Swate/commit/6b5a56f5786eb356703438ecffcb768a6444abcb)] Improve darkmode (Issue #25).
    * [[#37503a5](https://github.com/nfdi4plants/Swate/commit/37503a50786536ba88a72d591e2cf51fdfd113dc)] Enable term search without present annotation table (Issue #132).
    * [[#05a69b3](https://github.com/nfdi4plants/Swate/commit/05a69b323db6325d1309b7fbd5cf5b7f4279308e)] Increase responsiveness for copy to clipboard.
    * [[#44a75d1](https://github.com/nfdi4plants/Swate/commit/44a75d12c1583e138ed2cc328146922d14752d4f)] Add warnings to advanced setting functions.
    * [[#7d4060b](https://github.com/nfdi4plants/Swate/commit/7d4060b15def48f17b64dd42d0e1da207a3285cd)] Add function to update used protocols. :sparkles:
    * [[#088335f](https://github.com/nfdi4plants/Swate/commit/088335f811d41026269e2489337986331534c4a6)] Add option to update raw custom xml (Issue  #123).
    * [[#a3286eb](https://github.com/nfdi4plants/Swate/commit/a3286ebcefe217bbc4354c9e19fe79004d7afb6d)] Add checksum content type (Issue #127, Issue #131).
    * [[#97407d4](https://github.com/nfdi4plants/Swate/commit/97407d45c5139ded3234824f46c530e68e0556a1)] Changed DateTime to use UTC (Issue #126).
    * [[#137cc54](https://github.com/nfdi4plants/Swate/commit/137cc542db62fecb52fad77177bb6de1a72c1965)] Add more info for existing building blocks (Issue #124).
    * [[#66fb577](https://github.com/nfdi4plants/Swate/commit/66fb5771c55632c4cc0bf229996d8fa4cd304a69)] Add option to create pointer json template (Issue #129).
* Deletions:
    * [[#84d71ee](https://github.com/nfdi4plants/Swate/commit/84d71eef62c1d55bb2130143926d47e8b462fdeb)] Remove 'decimal' validation type.
* Bugfixes:
    * [[#d75743c](https://github.com/nfdi4plants/Swate/commit/d75743cc4597ab9ba557ea9522e6beea091db209)] Add minor fixes
    * [[#bd13cbf](https://github.com/nfdi4plants/Swate/commit/bd13cbf39f013277381b04bb9f30577d2a929f42)] Fix drag n drop problems in filepicker.
    * [[#33695f4](https://github.com/nfdi4plants/Swate/commit/33695f429ac6aa76e8638b9d5b375921b3d856bd)] Fix protocol grouping bug.
    * [[#f4d08e8](https://github.com/nfdi4plants/Swate/commit/f4d08e8f1f41c712ff787ce231e4c085795eef2a)] Fix protocol xml not correctly removed bug.

### 0.4.0+a0e04f3 (Released 2021-3-1)
* Additions:
    * latest commit #a0e04f3
    * [[#24950d1](https://github.com/nfdi4plants/Swate/commit/24950d160548a04e080b7bd283a699b611a116a6)] Minor visual updates
    * [[#183a80c](https://github.com/nfdi4plants/Swate/commit/183a80c31f823ad56706459dacc631fd2da0becb)] Update dropdown navbar quick access.
    * [[#4b818db](https://github.com/nfdi4plants/Swate/commit/4b818db47d9662964be0515075945a3fa4b3261c)] Add Advanced custom xml settings (Issue #111).
    * [[#d7cce09](https://github.com/nfdi4plants/Swate/commit/d7cce0939cfdd1212e8ea1e4f12863d77280d50e)] Add link to nfdi4pso issues (Issue #99).
    * [[#848acf7](https://github.com/nfdi4plants/Swate/commit/848acf7092daf70a9f8ae6f129b58751cfe14191)] Add "Update unit" functionality (Issue #110).
    * [[#5118778](https://github.com/nfdi4plants/Swate/commit/5118778e38a95a1e71e7a80488fd2a5e9fd63715)] Rename validation to checklist
    * [[#58b58a4](https://github.com/nfdi4plants/Swate/commit/58b58a42eef1ee08f63059ae2e971e74f8d29b15)] Add drop down for quick access icons
    * [[#3778ebc](https://github.com/nfdi4plants/Swate/commit/3778ebc951857295234d2c6d12bacce27bf29fd6)] Add copy to clipboard to term search for vertical term insert (Issue #118).
    * [[#eff46ae](https://github.com/nfdi4plants/Swate/commit/eff46aec41e4f0eb529d7f37ac789f352c85f5b4)] Restructure CustomXml :hammer::boom:
    * [[#44d9277](https://github.com/nfdi4plants/Swate/commit/44d9277901a8cb617bca1b78be679c12b4fc362b)] Add option to show all child terms (Issue #114).
    * [[#746ecf4](https://github.com/nfdi4plants/Swate/commit/746ecf4c3036f3c68b92d4d74c37118e491f83c1)] Redo autocomplete search on double click.
    * [[#454ccd7](https://github.com/nfdi4plants/Swate/commit/454ccd7368e61ff5b669b197c5cd3d0ade7b1c6b)] Add database template logic (Issue #10, #107) :sparkles:
    * [[#19a2f73](https://github.com/nfdi4plants/Swate/commit/19a2f739688a4819cf3275de348d41afa1351fc3)] Add button to display building block information (Issue #96).
    * [[#71801ff](https://github.com/nfdi4plants/Swate/commit/71801ff2e558834ed5bb413d8b62d2f7eea48419)] Add 'Remove Building Block' button (Issue #102).
    * [[#80c6235](https://github.com/nfdi4plants/Swate/commit/80c6235759e13263316e4c8f60b9d0f5eb7bc947)] Improve term search search speed.
    * [[#7760257](https://github.com/nfdi4plants/Swate/commit/7760257839839a03641ba5172d46fe77d97353dc)] Improve addition of validation importance (Issue #113).
* Bugfixes:
    * [[#ba4f238](https://github.com/nfdi4plants/Swate/commit/ba4f2389644d5f5adcb0460ab498eeccea96a84c)] Fix bug not finding the correct selected building block (Issue #121).
    * [[#41e298d](https://github.com/nfdi4plants/Swate/commit/41e298d4369d320331b50c7d08494567daac7004)] Fix updating protocol group header bug if split too often (Issue #120).
    * [[#01d5cf5](https://github.com/nfdi4plants/Swate/commit/01d5cf5db6152ca16d8fa765c80fd9034d8c9f8e)] Fix protocol group headers not correctly removed bug (Issue #119).

### 0.3.1+cbc655c (Released 2021-2-12)
* Additions:
    * latest commit #cbc655c
* Bugfixes:
    * [[#cbc655c](https://github.com/nfdi4plants/Swate/commit/cbc655cd6c9692480ac46c894127831aaea5b713)] Fix protocol headers shifted if not placed in row B (Issue #108) :bug:

### 0.3.0+0d31c43 (Released 2021-2-11)
* Additions:
    * latest commit #0d31c43
    * [[#4bf33cb](https://github.com/nfdi4plants/Swate/commit/4bf33cb478861250a3f1794140821460115e3173)] Add ontology accession number as tag in ref columns (Issue #100).
    * [[#262dae3](https://github.com/nfdi4plants/Swate/commit/262dae32acef085d3bfff46c1194f80698278387)] Add option to write process.json to Swate annotation table (Issue #84). :sparkles:
    * [[#09467d9](https://github.com/nfdi4plants/Swate/commit/09467d97813b708ce8bee58935b0b5830aea15f7)] Visually group building blocks to protocols (Issues 101#, #103, #104) :sparkles:
    * [[#0516353](https://github.com/nfdi4plants/Swate/commit/05163533c6832023301e588ccc59b34af5b18f88)] Add Logos and visually update Swate (Issue #59).
    * [[#5c2e56a](https://github.com/nfdi4plants/Swate/commit/5c2e56a46b57fa627c5b37b7e8307ab633a4e12b)] Add option to add unit cols to existing building block (Issue #94).
    * [[#9987184](https://github.com/nfdi4plants/Swate/commit/99871849bc83cfa4bd4fe7760c2f43dae524d76b)] Add sorensen dice sorting to advanced term search (Issue #95).
    * [[#9158bb7](https://github.com/nfdi4plants/Swate/commit/9158bb75696399492050109ebb0d04be59eeb9b6)] Update unit search to only search UO ontology (Issue #93).
    * [[#4e0d0c9](https://github.com/nfdi4plants/Swate/commit/4e0d0c9e32c5be606542f9ca0f05b74be6626e1d)] Add easy to access navigation option to advanced search (Issue #91).
    * [[#374e326](https://github.com/nfdi4plants/Swate/commit/374e326f2123a2f61825f281bc4886b109d5261d)] Add features from #68 to Update Reference Columns (Issue 87#).
* Deletions:
    * [[#9da9c55](https://github.com/nfdi4plants/Swate/commit/9da9c55a23d737aa05ff7c12759446ce5387902f)] Remove event handlers (input assist, #87).
* Bugfixes:
    * [[#0d31c43](https://github.com/nfdi4plants/Swate/commit/0d31c43ff55961b7eed6d91183c4c91c85356c59)] Fix bug not opening "File Picker" upload window on click.
    * [[#8606b12](https://github.com/nfdi4plants/Swate/commit/8606b12fe5497c2fbea2659cafd123c9a22dfe34)] Add Protocol Xml logic and fix ISADotNet dependency.
    * [[#51928c0](https://github.com/nfdi4plants/Swate/commit/51928c0af9b5d5a39dbc54abbadc3aa81e8580f8)] Fix minor routing icon mismatch.
    * [[#fdcb58c](https://github.com/nfdi4plants/Swate/commit/fdcb58c71ce624879448c4e86e2119b72bc877ae)] Fix minor white/lightgrey mix ups in filepicker view.
    * [[#cc26e81](https://github.com/nfdi4plants/Swate/commit/cc26e81e895d7ec7fc7abef885e5d7afb4c0a7c2)] Fix bug overloading computers when creating an annotation table for whole rows (Issue #63).
    * [[#1e5eb3d](https://github.com/nfdi4plants/Swate/commit/1e5eb3d6c2b0f2f0527d4843c8bc0addabdb0b04)] Fix reset of unit search input when unchecking (Issue #92).
    * [[#474cf73](https://github.com/nfdi4plants/Swate/commit/474cf73cb48227d58e88d239e6e9e50e8676c78a)] Fix bug creating wrong TAN with insertTerm.

### 0.2.0+899b535 (Released 2021-1-11)
* Additions:
    * latest commit #899b535
    * [[#1182030](https://github.com/nfdi4plants/Swate/commit/1182030d57695643e9b333f0bfbdfe11e64ceab2)] Add Setting Page
    * [[#d4a36f1](https://github.com/nfdi4plants/Swate/commit/d4a36f1e3417f5e49c184392e30d95d353f54a07)] Provide validation information via XML metadata (Issue #45). :christmas_tree: :fireworks:
    * [[#f3a11f0](https://github.com/nfdi4plants/Swate/commit/f3a11f0257f5d7d25a67dfdb85700903573d9ec1)] Update FilePicker with reordering functionality (Issue #13).
    * [[#f6564d6](https://github.com/nfdi4plants/Swate/commit/f6564d65c9985c82cbad3b482792e94379a7b34b)] Add search term search by accession number (Issue #71).
    * [[#bdba3ae](https://github.com/nfdi4plants/Swate/commit/bdba3ae061d4c0aa473eef19ab2c55586582c462)] Properly Document Office interop functions (Issue #75).
    * [[#aa870f1](https://github.com/nfdi4plants/Swate/commit/aa870f1c2d40a20f6dc71bb6fcc0a7d4ace49847)] Update README.md
    * [[#e958024](https://github.com/nfdi4plants/Swate/commit/e958024d7ac0f804107eaf55fb66e74e966acd63)] Improve readme :book:
* Bugfixes:
    * [[#9c07338](https://github.com/nfdi4plants/Swate/commit/9c07338a624240d1f3119cee243164186c5203b2)] Fix input assistance is not added when first table is created (Issue #82).
    * [[#889b86c](https://github.com/nfdi4plants/Swate/commit/889b86c466c454e736daf950ac0df4f77dcb6355)] Fix file picker not uploading reoccuring file names (Issue #80).

### 0.1.3+c6ad5b7 (Released 2020-12-7)
* Additions:
    * latest commit #c6ad5b7
    * [[#c6ad5b7](https://github.com/nfdi4plants/Swate/commit/c6ad5b7271ea5ff4ccc51f7022722bfc95b7b116)] Add error modal (Issue #73).
    * [[#6cb6ebf](https://github.com/nfdi4plants/Swate/commit/6cb6ebf718b6184a769b7eaa67e341331cb4c1b5)] Add core functionality to File Picker (Issue #13).
    * [[#e04e953](https://github.com/nfdi4plants/Swate/commit/e04e95345d473fd6b69b196c5e4b11939aa3b6df)] Add link to ontobee page for ontology in term search suggestions (Issue #69).
    * [[#13f3639](https://github.com/nfdi4plants/Swate/commit/13f3639c7181292ccc3764e2b31d5ae91f1f4dcf)] Create issue templates
    * [[#5abee29](https://github.com/nfdi4plants/Swate/commit/5abee298f349005b609ebc21eaf70a0e76e5c5d8)] Add Unit Column when selecting a unit for a term (Issue #48).
    * [[#5abee29](https://github.com/nfdi4plants/Swate/commit/5abee298f349005b609ebc21eaf70a0e76e5c5d8)] Add option to fill hidden cols according to main column (Issue #67).
    * [[#5abee29](https://github.com/nfdi4plants/Swate/commit/5abee298f349005b609ebc21eaf70a0e76e5c5d8)] Add input assist to delete hidden col cells onChange of main col (Issue #68).
* Bugfixes:
    * [[#0a9ac89](https://github.com/nfdi4plants/Swate/commit/0a9ac899d5e5d1609b0dfdc0bd01a46e6e829735)] Fix bug of term input field not indicating change after using autocomplete suggestion.
    * [[#c4befec](https://github.com/nfdi4plants/Swate/commit/c4befecbf4141066ce9aebb8b6014b1f81eaddea)] Fix bug where auto fill would delete some rows of first column.
    * [[#2498c0b](https://github.com/nfdi4plants/Swate/commit/2498c0beee9433e5a7a71cb7661956a6f0b6a609)] Fix file picker button view.
    * [[#eb104fe](https://github.com/nfdi4plants/Swate/commit/eb104fe72f8b253e8afdb24378836dfec55e0d6c)] Fix bug where search results from "Advanced Search" are not selectable (Issue #70).
    * [[#63aa8ea](https://github.com/nfdi4plants/Swate/commit/63aa8ea3c0c36e2a821e92429331146a2a054b44)] Fix bug where cursor jumps to the end of search input field (Issue #66).
    * [[#1847bf5](https://github.com/nfdi4plants/Swate/commit/1847bf5097bc8e87861ab431e05826c19f8fd3f4)] Fix (visual) pagination components for advanced term search (Issue #65).
    * [[#d207770](https://github.com/nfdi4plants/Swate/commit/d207770a880261a2e293c721b90fc69925abc48a)] Fix not shown `No Ontology` option in advanced term search (Issue #64).
    * [[#1fd3f67](https://github.com/nfdi4plants/Swate/commit/1fd3f6716b2a1f3fc5a1bdbefb7728ef862d4627)] Fix minor bug in release notes creation.

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
