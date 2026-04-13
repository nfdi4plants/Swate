namespace Swate.Components.ArcFileEditor

open Feliz
open Fable.Core
open ARCtrl
open Swate.Components.ArcFileEditor.Types
open Swate.Components.Shared
open Swate.Components

[<Erase; Mangle(false)>]
type ArcFileFooterTabs =

    [<ReactComponent>]
    static member Main
        (arcFile: ArcFiles, activeView: ActiveView, setActiveView: ActiveView -> unit, setArcFile: ArcFiles -> unit)
        =
        let tables = arcFile.Tables()
        let canAddTable = arcFile.CanCreateTables()

        let addNewTable () =
            if canAddTable then
                let nextName = Helper.createNewTableName tables
                let nextTable = ArcTable.init nextName

                tables.Add nextTable
                setArcFile (WidgetArcFile.refreshRef arcFile)
                setActiveView (ActiveView.Table(tables.Count - 1))

        let footerTabBaseClasses =
            "swt:btn swt:btn-sm swt:border swt:border-white! swt:hover:border-white! swt:rounded-none"

        let tabIcon iconClass =
            Html.i [ prop.className [ "swt:iconify " + iconClass ] ]

        Html.div [
            prop.className
                "swt:flex swt:gap-0 swt:p-2 swt:bg-base-200 swt:border-t swt:border-base-300 swt:overflow-x-auto"
            prop.children [
                Html.button [
                    prop.className [
                        footerTabBaseClasses
                        if activeView = ActiveView.Metadata then
                            "swt:btn-primary swt:border-2 swt:border-white!"
                        else
                            "swt:btn-ghost"
                    ]
                    prop.onClick (fun _ -> setActiveView ActiveView.Metadata)
                    prop.children [
                        tabIcon "swt:fluent--info-24-regular"
                        Html.span [ prop.text "Metadata" ]
                    ]
                ]
                for index = 0 to tables.Count - 1 do
                    let table = tables.[index]

                    Html.button [
                        prop.key (string index)
                        prop.className [
                            footerTabBaseClasses
                            if activeView = ActiveView.Table index then
                                "swt:btn-primary swt:border-2 swt:border-white!"
                            else
                                "swt:btn-ghost"
                        ]
                        prop.onClick (fun _ -> setActiveView (ActiveView.Table index))
                        prop.children [
                            tabIcon "swt:fluent--table-24-regular"
                            Html.span [ prop.text table.Name ]
                        ]
                    ]
                if arcFile.CanRenderDataMapView() then
                    Html.button [
                        prop.className [
                            footerTabBaseClasses
                            if activeView = ActiveView.DataMap then
                                "swt:btn-primary swt:border-2 swt:border-white!"
                            else
                                "swt:btn-ghost"
                        ]
                        prop.onClick (fun _ -> setActiveView ActiveView.DataMap)
                        prop.children [
                            tabIcon "swt:fluent--database-24-regular"
                            Html.span [ prop.text "DataMap" ]
                        ]
                    ]
                if canAddTable then
                    Html.button [
                        prop.key "new-table-button"
                        prop.title "+"
                        prop.className
                            "swt:btn swt:btn-sm swt:btn-outline swt:items-center swt:border swt:border-white! swt:hover:border-white! swt:rounded-none"
                        prop.onClick (fun _ -> addNewTable ())
                        prop.children [ Html.span [ prop.text "+" ] ]
                    ]
            ]
        ]