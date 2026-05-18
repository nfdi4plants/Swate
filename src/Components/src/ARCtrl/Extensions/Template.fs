[<AutoOpen>]
module ARCtrl.TemplateExtensions

open ARCtrl

type Template with
    member this.FileName = this.Name.Replace(" ", "_") + ".xlsx"
