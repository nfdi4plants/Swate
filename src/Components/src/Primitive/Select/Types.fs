[<AutoOpenAttribute>]
module Swate.Components.Primitive.Select.Types

open Fable.Core
open Feliz

type SelectItem<'a> = {| item: 'a; label: string |}

type SelectItemRender<'a> = {|
    isActive: bool
    isSelected: bool
    item: SelectItem<'a>
|}