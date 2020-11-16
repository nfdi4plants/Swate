Swate
=====

> **Swate** - something or someone that gets you absolutely joyed([Urban dictionary](https://www.urbandictionary.com/define.php?term=swate))

**Swate** is a **S**wate **W**orkflow **A**nnotation **T**ool for **E**xcel.

_This project is in an exerimental state._



The aims of Swate are:

1. Provide an easy way to annotate experimental data in an application (excel) that every wet lab scientist is familiar with
2. Provide a way to create computational workflows that start from raw data and end with results.
3. ~~Create ISA and CWL compatible data models from the input~~ (moved to the [ARC commander project](https://github.com/nfdi4plants/arcCommander))
4. Provide an interface for remote execution of the defined workflows

Check the state of the [minimal POC milestone ](https://github.com/nfdi4plants/Swate/milestone/1) for the current state of features.

Test
----

You can test swate by following the steps below. If you decide to do so, please take the time to set up a Github account to report your issues and suggestions [here](https://github.com/nfdi4plants/Swate/issues).
You can also search existing issues for solutions for your questions and/or discussions about your suggestions.

**Here are the necessary steps to use SWATE:**


 - **If you use Excel locally**:
   - Install [node.js LTS](https://nodejs.org/en/) (needed for office addin related tooling)
   - Download the [latest test release archive](https://github.com/nfdi4plants/Swate/releases) and extract it
   - Execute the test.cmd (windows, as administrator) or test.sh (macOS, you will need to make it executable via chmod a+x) script.

 - **If you use Excel in the browser**:
   - Download the [latest test release archive](https://github.com/nfdi4plants/Swate/releases) and extract it
   - Launch Excel online, open a (blank) workbook 
   - Under the `Insert` tab, select `Add-Ins`
   - Go to `Manage my Add-Ins` and select `Upload my Add-In`
   - select and upload the `manifest.xml` file contained in the test release archive.

Discuss
-------

Documentation of the features is now a top priority as we completed the POC stage :rocket:. You can discuss needed docs and the available ones [here](https://github.com/nfdi4plants/Swate/issues/47)

Please use Github issues to track problems, feature requests, questions, and discussions. Additionally, you can get in touch with us on [Twitter](https://twitter.com/nfdi4plants)

Develop
-------

#### Contribute:

Before you contribute to the project remember to return all placeholders to your project:

-   webpack.config.js    
    ```
    https: {
        key: "{USERFOLDER}/.office-addin-dev-certs/localhost.key",
        cert: "{USERFOLDER}/.office-addin-dev-certs/localhost.crt",
        ca: "{USERFOLDER}/.office-addin-dev-certs/ca.crt"
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

#### Prerequisites:

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

        ![File](docsrc/files/img/file.png)

        installing this ca certificate under your trusted root certification authorities will enable you to use https via localhost.

        The paths to these certificates are then added to your webpack.config file.

     - You may need a loopback exemption for Edge/IE (whatever is run in your excel version): 

        `CheckNetIsolation LoopbackExempt -a -n="microsoft.win32webviewhost_cw5n1h2txyewy"`

### use install.cmd

The install.cmd executes several console commands for one of which it needs adminstratorial rights (dotnet fake build -t setup) to install the certificate mentioned above.
Open powershell as adminstrator and navigate to the Swate-folder ```cd your\folder\path\Swate``` then use ```.\install.cmd``` to initialize the setup.
While running a installation dialogue for the certificate will open and can be handled as described above.

By installing this repo via the install.cmd file the MySql password will default to 'example'.

### Project Decription

This project uses the [SAFE Stack](https://github.com/SAFE-Stack) to create a website that uses [office.js](https://github.com/OfficeDev/office-js) to interop with Excel.

The file [OfficeJS.fs](src/Client/OfficeJS.fs) contains Fable bindings autogenerated from the office.js typescript definitions.

to debug the AddIn locally, use the build target `OfficeDebug`:

`fake build -t OfficeDebug`

this will launch an Excel instance with the AddIn sideloaded, while also running docker with a MySql- and a Adminer instance.
The MySql user/password will be root/example and can be set in .db/docker-compose.yml.
Adminer can be accessed at localhost:8085, MySql at localhost:42333, and the app runs at localhost:3000 for client and localhost:8080 for the server.

However it is currently pretty hard to attach a debugger to the instance of edge that runs in
the Excel window (update: you can now use [EdgeDevToolsPreview](https://www.microsoft.com/en-us/p/microsoft-edge-devtools-preview/9mzbfrmz0mnj?activetab=pivot:overviewtab) for that aswell). You can circumvent this issue by additionally testing in Excel online:

 - open Excel online in your favorite browser
 - create a blank new workbook (or use any workbook, but be warned that you can't undo stuff done via Office.js) 
 - Go to Insert > Office-Add-Ins and upload the manifest.xml file contained in this repo
    ![Add In Upload](docsrc/files/img/AddInUpload.png)
 - You will now have the full debug experience in your browser dev tools.

Alternatively, you can debug all functionality that does not use Excel Interop in your normal browser (the app runs on port 3000 via https)

