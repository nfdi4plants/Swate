How to add a new page to the project (For example page **"NewName"**)

1. Start by adding a route to the new page in ``Routing.fs``
(multiple steps, work from top to bottom and add to all)
2. Create a new file in the `States` folder with the page `Model` and `Msg`:

```fsharp
namespace NewName

type Model = {
    Default: obj
} with
    static member init() = {
        Default = ""
    }

type Msg =
| DefaultMsg
```

3. Add the submodel to `Messages.Model` and the subrouting to `Messages.Msg` (`Messages.fs`)

```fsharp
type Model = {
    NewNameModel : NewName.Model
} with
    member this.updateByJSONExporterModel (m:NewName.Model) =
        { this with NewNameModel = m}

type Msg =
| NewNameMsg       of NewName.Msg

```

3. 1 Update the ``Model.init()`` function accordingly.

4. Create a new folder ``NewName`` and add ``NewName.fs``. Move the folder above the ``Update``. folder.
5. Add the following to ``NewName.fs``.

```fsharp
open Fable.React
open Fable.React.Props
open Fulma
open Fulma.Extensions.Wikiki
open Fable.FontAwesome
open Fable.Core.JsInterop
open Elmish

open Shared

open ExcelColors
open Model
open Messages

open NewName //!

let update (msg:Msg) (currentModel: Messages.Model) : Messages.Model * Cmd<Messages.Msg> =
    match msg with
    | NewName.DefaultMsg ->
        Fable.Core.JS.console.log "Default Msg"
        currentModel, Cmd.none

open Messages

let defaultMessageEle (model:Model) dispatch =
    mainFunctionContainer [
        Button.a [
            Button.OnClick(fun e -> DefaultMsg |> NewNameMsg |> dispatch)
        ][
            str "Click me!"
        ]
    ]

let newNameMainElement (model:Messages.Model) dispatch =
    form [
        OnSubmit    (fun e -> e.preventDefault())
        OnKeyDown   (fun k -> if (int k.which) = 13 then k.preventDefault())
    ] [

        Label.label [Label.Size Size.IsLarge; Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]][ str "JSON Exporter"]

        Label.label [Label.Props [Style [Color model.SiteStyleState.ColorMode.Accent]]] [str "Function 1"]

        defaultMessageEle model dispatch
    ]
```

6. Add update subrouting to the update function in ``Update.fs``.

```fsharp
    | NewNameMsg msg ->
        let nextModel, nextCmd = currentModel |> (*NewName.*)update msg
        nextModel, nextCmd
```

7. Add to Client.view function

```fsharp
    | Routing.Route.NewName ->
        BaseView.baseViewMainElement model dispatch [
            JSONExporter.newNameMainElement model dispatch
        ] [ (*Footer*) ]
```

8. Add navigation tab (optional). Just follow existing scheme in ``View.BaseView.fs``.

```fsharp
let tabs (model:Model) dispatch =
    tabRow model dispatch [
        if model.PersistentStorageState.PageEntry = Routing.SwateEntry.Core then
            createNavigationTab Routing.Route.BuildingBlock         model dispatch
            createNavigationTab Routing.Route.TermSearch            model dispatch
            createNavigationTab Routing.Route.Protocol              model dispatch
            createNavigationTab Routing.Route.FilePicker            model dispatch
            createNavigationTab Routing.Route.Info                  model dispatch
        else
            createNavigationTab Routing.Route.Validation            model dispatch
            createNavigationTab Routing.Route.JSONExporter          model dispatch
            createNavigationTab Routing.Route.XLSXConverter         model dispatch
            createNavigationTab Routing.Route.Info                  model dispatch
    ]
```