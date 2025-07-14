module LocalStorage.AutosaveConfig

open Browser

[<Literal>]
let AutosaveConfig_Key = "AutosaveConfig"

let getAutosaveConfiguration () =
    try
        WebStorage.localStorage.getItem (AutosaveConfig_Key)
        |> (fun item -> bool.Parse(item))
        |> Some
    with _ ->
        WebStorage.localStorage.removeItem (AutosaveConfig_Key)
        None

let setAutosaveConfiguration (autosaveConfig: bool) =
    WebStorage.localStorage.setItem (AutosaveConfig_Key, autosaveConfig.ToString())