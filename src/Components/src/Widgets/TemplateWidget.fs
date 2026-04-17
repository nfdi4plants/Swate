namespace Swate.Components

open ARCtrl
open Fable.Core
open Feliz
open Swate.Components.Shared
open Swate.Components.Template

module TemplateWidgetMain =

    let Main
        (arcFile: ArcFiles, activeTableIndex: int option, setArcFile: ArcFiles -> unit, services: TemplateWidgetServices) =
        global.Swate.Components.Template.TemplateWidget.Main(arcFile, activeTableIndex, setArcFile, services)