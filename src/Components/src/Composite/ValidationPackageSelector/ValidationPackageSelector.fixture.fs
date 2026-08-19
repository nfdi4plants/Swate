namespace Swate.Components.Composite.ValidationPackageSelector

open Fable.Core
open Feliz
open Types
open ARCtrl.ValidationPackages

module private Fixtures =

    let packages = [|
        ValidationPackageDTO.Create(
            "Invenio",
            "Invenio is a validation package for the Invenio project",
            "Invenio is a validation package for the Invenio project.\nIt does it very good, it does it very well.\nIt does it very fast, it does it very swell.",
            1,
            0,
            0,
            "",
            "",
            [||],
            System.DateTime.Now,
            [||],
            "",
            "",
            [||],
            ""
        )

        ValidationPackageDTO.Create(
            "MyPackage",
            "MyPackage does the thing",
            "MyPackage does the thing.\nIt does it very good, it does it very well.\nIt does it very fast, it does it very swell.",
            1,
            0,
            0,
            "alpha.1",
            "0",
            [||],
            System.DateTime.Now,
            [||],
            "",
            "",
            [||],
            ""
        )

        ValidationPackageDTO.Create(
            "MyPackage2",
            "MyPackage2 does the thing",
            "MyPackage2 does the thing.\nIt does it very good, it does it very well.\nIt does it very fast, it does it very swell.",
            1,
            0,
            0,
            "alpha.1",
            "0",
            [||],
            System.DateTime.Now,
            [||],
            "",
            "",
            [||],
            ""
        )
    |]


[<Erase; Mangle(false)>]
type ValidationPackageSelectorFixture = 

    [<ReactComponent(true)>]
    static member Main () =

        let currentConfig, setCurrentConfig = React.useState(fun () -> ValidationPackagesConfig.make (ResizeArray()) None)

        let fetch () : JS.Promise<ValidationPackageDTO []> =
            promise {
                do! Promise.sleep 2000
                return Fixtures.packages
            } 

        let write = fun (config: ValidationPackagesConfig) -> 
            Promise.lift (
                try 
                    setCurrentConfig config
                    Ok ()
                with ex -> Error ex
            )

        React.Fragment [
            Html.div [
                prop.className "swt:border-b-2 swt:p-2 swt:mb-2"
                prop.children [
                    Html.div [
                        prop.className "swt:p-2 swt:border swt:rounded"
                        prop.children [
                            Html.h1 "Current Config"
                            Html.textarea [
                                prop.testId "validation-package-selector-config"
                                prop.className "swt:w-full swt:h-32"
                                prop.value (currentConfig.ToString())
                                prop.readOnly true

                            ]
                        ]
                    ]
                ]
            ]
            ValidationPackageSelector.ValidationPackageSelector(
                config = currentConfig,
                writeConfig = write,
                fetchValidationPackages = fetch
            )
        ]

