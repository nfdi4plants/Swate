# Swate

> **Swate** - something or someone that gets you absolutely joyed ([Urban dictionary](https://www.urbandictionary.com/define.php?term=swate))

**Swate** is a **S**wate **W**orkflow **A**nnotation **T**ool for **E**xcel.

Swate aims to provide a low-friction workflow annotation experience that makes the usage of controlled vocabularies (ontologies) as easy and intuitive as possible. It is designed to integrate in the familiar spreadsheet environment that is the center of a great deal of data-focused wetlab work.

![image](https://user-images.githubusercontent.com/39732517/135290851-cacd8626-2cc3-4c58-a343-c5ad037e3c5c.png)


<!-- TOC -->
## Table of contents

- [Features](#features)
- [Install/Use](#installuse)
- [Contact](#contact)
- [Develop](#develop)
  - [Contribute](#contribute)
  - [Prerequisites](#prerequisites)
  - [Use install.cmd](#use-installcmd)
  - [Set up Sql Dump](#set-up-sql-dump)
  - [Project Decription](#project-decription)

<!-- /TOC -->


## Features

For a full in-depth view of all Swate features check the [documentation](https://github.com/nfdi4plants/Swate/wiki).

Feature | Excel 365/ Office online  	| Excel 2019  	|
|---            |:---:	        |:---:	        |
|  **Core**     |  	            |
| Adding and managing <br> building blocks|<span style="color:#1FC2A7">✓</span>|<span style="color:#1FC2A7">✓</span>|
| Ontology Term search <br> and insert|<span style="color:#1FC2A7">✓</span>| <span style="color:#1FC2A7">✓</span>
| Ontology Term update | <span style="color:#1FC2A7">✓</span> |<span style="color:#1FC2A7">✓</span>|
| Add Templates|<span style="color:#1FC2A7">✓</span>|<span style="color:#1FC2A7">✓</span>
| Copy filenames into table| <span style="color:#1FC2A7">✓</span>| <span style="color:#1FC2A7">✓</span>
| Display DAG| <span style="color:#1FC2A7">✓</span>| <span style="color:#c21f3a">x</span>
|  **Experts**     |  	            |
| Export Swate information | <span style="color:#1FC2A7">✓</span>| <span style="color:#c21f3a">x</span>
| Create Template Metadata | <span style="color:#1FC2A7">✓</span>| <span style="color:#c21f3a">x</span>
| Write Checklist Custom Xml | <span style="color:#1FC2A7">✓</span>| <span style="color:#1FC2A7">✓</span>
| Edit Custom Xml | <span style="color:#1FC2A7">✓</span>| <span style="color:#1FC2A7">✓</span>

## Install/Use

[Swate installation](https://github.com/nfdi4plants/Swate/wiki/docs01-installing-Swate)


## Contact

If you have any issues using Swate, missing features or found a nasty bug :bug: you can always contact us via:

- [GitHub Issues](https://github.com/nfdi4plants/Swate/issues)
- [DataPLANT Helpdesk](https://support.nfdi4plants.org/?topic=Tools_Swate)

## Develop

### Contribute
Starting at [Prerequisites](#prerequisites) we will explain the set up, but please read the following first.
Before you contribute to the project remember to return all placeholders to your project:

-   webpack.config.js    
    ```
    server: {
            type: 'https',
            options: {
                key: "{USERFOLDER}/.office-addin-dev-certs/localhost.key",
                cert: "{USERFOLDER}/.office-addin-dev-certs/localhost.crt",
                ca: "{USERFOLDER}/.office-addin-dev-certs/ca.crt"
            },
        },
    ```
-   .db/docker-compose.yml
    ```
    MYSQL_ROOT_PASSWORD: {PASSWORD}
    ```
-   Server/dev.json
    ```
    "Swate:ConnectionString": "server=127.0.0.1;user id=root;password={PASSWORD}; port=42333;database=SwateDB;allowuservariables=True;persistsecurityinfo=True"
    ```

### Prerequisites

 - .NET Core SDK at of at least the version in [the global.json file](global.json)
 - Docker with Docker-compose
 - Node.js with npm/npx
 - To setup all dev dependencies, you can run the following commands or the install.cmd file (explained further below). The first run will take some time to import the database from the .sql file:

    `dotnet tool restore` (to restore local dotnet tools)

    `dotnet fake build -t setup pw:example`, which will take care of installing necessary certificates and loopback exempts for your browser. Here are the steps if you want to execute them by yourself:

    - 'pw:example' is an optional parameter for the setup target to use a custom password for the local MySql docker instance. If this argument is not passed the instance will be created with the password 'example'.

    - connections from excel to localhost need to be via https, so you need a certificate and trust it. [office-addin-dev-certs](https://www.npmjs.com/package/office-addin-dev-certs?activeTab=versions) does that for you.

        you can also use the fake build target for certificate creation and installation by using `dotnet fake build -t createdevcerts` in the project root (after restoring tools via `dotnet tool restore`).

        This will use office-addin-dev-certs to create the necessary certificates, and open the installation dialogue for you:

        ![File](.img/install_certificate_window.png)

        installing this ca certificate under your trusted root certification authorities will enable you to use https via localhost.

        The paths to these certificates are then added to your webpack.config file.

     - You may need a loopback exemption for Edge/IE (whatever is run in your excel version): 

        `CheckNetIsolation LoopbackExempt -a -n="microsoft.win32webviewhost_cw5n1h2txyewy"`

### Use install.cmd

The install.cmd executes several console commands for one of which it needs adminstratorial rights (dotnet fake build -t setup) to install the certificate mentioned above.
Open powershell as adminstrator and navigate to the Swate-folder ```cd your\folder\path\Swate``` then use ```.\install.cmd``` to initialize the setup.
While running a installation dialogue for the certificate will open and can be handled as described above.

By installing this repo via the install.cmd file the MySql password will default to 'example'.

### Set up Sql Dump

The database became too large for GitHub to upload the dump directly. Therefore we now provide the basic Database structure as .sql dump file in this repo, including the data for ontologies and all template related tables. If you wish to participate in development open an issue and we can figure out how to share a ready-to-use database dump.

### Project Decription

This project uses the [SAFE Stack](https://github.com/SAFE-Stack) to create a website that uses [office.js](https://github.com/OfficeDev/office-js) to interop with Excel.

The utilized Fable bindings for office-js can be found in [office-fable](https://github.com/Freymaurer/office-fable).

To debug the AddIn locally, use the build target `officedebug`:

`dotnet run officedebug`

this will launch an Excel instance with the AddIn sideloaded, while also running docker with a MySql- and a Adminer instance.
The MySql user/password will be root/example and can be set in .db/docker-compose.yml.
Adminer can be accessed at localhost:8085, MySql at localhost:42333, and the app runs at localhost:3000 for client and localhost:8080 for the server.

To debug add-in instances you can now easily open debug console/inspect for newer Excel versions. For Excel 2019 and earlier you can use the create a `.cmd` file with `C:\Windows\SysWOW64\F12\IEChooser.exe` to easily open the f12 tools (on windows). Alternatively use office online:

 - open Excel online in your favorite browser
 - create a blank new workbook (or use any workbook, but be warned that you can't undo stuff done via Office.js) 
 - Go to Insert > Office-Add-Ins and upload the manifest.xml file contained in this repo
    ![Add In Upload](.img/AddInUpload.png)
 - You will now have the full debug experience in your browser dev tools.

Alternatively, you can debug all functionality that does not use Excel Interop in your normal browser (the app runs on port 3000 via https)

