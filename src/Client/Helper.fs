[<AutoOpenAttribute>]
module Helper

open Fable.Core

let log (a) = Browser.Dom.console.log a

let logf a b = 
    let txt : string = sprintf a b
    log txt