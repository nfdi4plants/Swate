# Swate

> **Swate** - something or someone that gets you absolutely joyed ([Urban dictionary](https://www.urbandictionary.com/define.php?term=swate))

**Swate** is a **S**wate **w**orkflow **a**nnotation **t**ool for **e**xcel.

Swate aims to provide a low-friction workflow annotation experience that makes the usage of controlled vocabularies (ontologies) as easy and intuitive as possible. It is designed to integrate in the familiar spreadsheet environment that is the center of a great deal of data-focused wetlab work.

![image](https://user-images.githubusercontent.com/39732517/135290851-cacd8626-2cc3-4c58-a343-c5ad037e3c5c.png)


<!-- TOC -->
## Table of contents

- [Docs](#docs)
- [Install/Use](#installuse)
- [Contact](#contact)

<!-- /TOC -->

## Docs

- Ontology term search
- ISA conform output
- Minimal information standards

Try our [quickstart](https://nfdi4plants.github.io/nfdi4plants.knowledgebase/docs/guides/swate_QuickStart.html) or a more in-depth [walkthrough](https://nfdi4plants.github.io/nfdi4plants.knowledgebase/docs/guides/swate_walkthrough.html).

For a full in-depth view of all Swate features check the [documentation](https://nfdi4plants.github.io/nfdi4plants.knowledgebase/docs/SwateManual/index.html).

## Install/Use

[Swate installation](https://nfdi4plants.github.io/nfdi4plants.knowledgebase/docs/SwateManual/Docs01-Installing-Swate.html)

## Contact

If you have any issues using Swate, missing features or found a nasty bug :bug: you can always contact us via:

- [GitHub Issues](https://github.com/nfdi4plants/Swate/issues)
- [DataPLANT Helpdesk](https://support.nfdi4plants.org/?topic=Tools_Swate)

## Dev

These instructions are only relevant if you too want to participate in developing Swate!

### Requirements

- [.NET SDK](https://dotnet.microsoft.com/en-us/download), >= 8.0.0
  - verify with `dotnet --version`
- [nodejs](https://nodejs.org/en/download), >=18
  - verify with `node --version`
- npm, >=9
  - likely part of nodejs installation
  - verify with `npm --version`
- [docker](https://docs.docker.com/engine/install/), >= 24
  - verify with `docker --version`
  - this is required for database setup

### Setup

- clone this repo
- Run dotnet tool restore
- Run npm install

### Development

#### Start

`./build.cmd run`, to start up Swate (+ Database network)

Swate runs on localhost:8080 (and swobup on localhost:8000).

#### Available commands

```
Usage: ./build.cmd <command>

run (--nodb)                        Start .net backend server, vite frontend (and database, 
                                    swobup with docker if not `--nodb`)

release (pre)                       Run .net tests tag current branch and force push to 
                                    release branch (nightly if `pre`), this will trigger
                                    Github release with docker image

bundle                              Create distributable, used in docker image creation.

docker  
    create                          Create new swate:new image
                
    test                            Start test instance of docker compose network from swate:new image

version

    create-file <version>           Create new `src/Server/Version.fs` with `<version>`.
```