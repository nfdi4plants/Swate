module DocsServiceAPIvs1

open Shared
open Giraffe
open Saturn
open Shared
open Shared.DbDomain

open Fable.Remoting.Server
open Fable.Remoting.Giraffe

open DocsFunctions

let serviceDocsv1 = Docs.createFor<IServiceAPIv1>()

let serviceApiDocsv1 =
    Remoting.documentation (sprintf "Service API v1") [

        ///////////////////////////////////////////////////////////// Development /////////////////////////////////////////////////////////////
        ////////
        serviceDocsv1.route <@ fun api -> api.getAppVersion @>
        |> serviceDocsv1.alias "Get App Version (<code>getAppVersion</code>)"
        |> serviceDocsv1.description
            (
                createDocumentationDescription
                    "This function is used to get a server site saved version string for the app."
                    "<code>getAppVersion</code> is executed during app initialisation and displayed in the footer."
                    None
                    "Returns the app version."
                    (Parameter.create "Version" ParamString "App Version")
            )

]