
module LocalStorage.AutosaveConfig

open Browser

let [<Literal>] AutosaveConfig_Key = "AutosaveConfig"

let getAutosaveConfiguration() =
    try
        WebStorage.localStorage.getItem(AutosaveConfig_Key)
        |> (fun item -> bool.Parse(item))
        |> Some
    with
        | _ ->
            WebStorage.localStorage.removeItem(AutosaveConfig_Key)
            printfn "Could not find %s" AutosaveConfig_Key
            None

let setAutosaveConfiguration(autosaveConfig: bool) =
    WebStorage.localStorage.setItem(AutosaveConfig_Key, autosaveConfig.ToString())
