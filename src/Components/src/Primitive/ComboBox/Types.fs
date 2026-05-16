[<AutoOpenAttribute>]
module Swate.Components.Primitive.ComboBox.Types

open Fable.Core
open Feliz

type ComboBoxRef = {|
    focus: unit -> unit
    close: unit -> unit
    isOpen: unit -> bool
|}