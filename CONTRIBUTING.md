# Local Setup

- [Requirements](#requirements)
- [Setup](#setup)
- [Start](#start)
  - [Available commands](#available-commands)
- [Contribute](#contribute)

## Requirements

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
- Any F# IDE, e.g. [Visual Studio Code](https://code.visualstudio.com/) + [Ionide extension](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp#:~:text=Ionide-VSCode%20is%20a%20VSCode,powers%20language%20features%20is%20FSAutoComplete.), [Rider](https://www.jetbrains.com/rider/), [Visual Studio](https://visualstudio.microsoft.com/)

## Setup

This needs to be done once per repository download.

1. Clone this repo
2. Run dotnet tool restore
3. Run npm install

> [!NOTE]
> After starting the database for the first time you will need to setup the db from online ressources. This can be done by Swobup on
> [http://localhost:8000/docs](http://localhost:8000/docs). You can find the credentials in the [docker-compose](.db/docker-compose.yml) file (SWOBUP_USERNAME=swobup, SWOBUP_PASSWORD=test).
>
> Run:
> 1. PUT `api/v2/database/init`
> 2. PUT `api/v2/ontology/build`
>
> You can verify the correct task execution on [redis](http://localhost:1111). It might happen that some ontologies fail to build, in this case you can try to build them again with `POST api/v2/ontology` and the correct link from our [ontology repository](https://github.com/nfdi4plants/nfdi4plants_ontology).

## Start

`./build.cmd run db`, to start up Swate (+ Database network)

**Swate** runs on [https://localhost:3000](https://localhost:3000), **Swobup** on [http://localhost:8000/docs](http://localhost:8000/docs), **Database - Neo4j** can be viewed in the browser on [http://localhost:7474/browser/](http://localhost:7474/browser/).

> [!WARNING]
> Should the neo4j database not correctly start up with `./build.cmd run db` delete the container (only container not the volume) and start it again. This can be easily done with the docker ui. Open the `swate_dev` network and click on the thrash bin icon next to the neo4j container.
>
> If you know a solution to this issue, please let us know 😞

### Available commands

```
Usage: ./build.cmd <command>

run [db]                            Start .net backend server, vite frontend (and database,
                                    swobup with docker if `db`)

release [pre]                       Run .net tests tag current branch and force push to
                                    release branch (nightly if `pre`), this will trigger
                                    Github release with docker image
```

## Contribute

If you want to contribute to Swate, open an issue with the feature/bug you want to work on. This way you can ensure that your approach is in line with the project goals and you can get feedback from the maintainers.

Afterwards you can fork the repository and start working on your feature/bug. When you are done, open a pull request with a detailed description of your changes and the issue you are working on.

We are currently still working on a nice project structure. For now ask us is any questions arise in the related GitHub issue!