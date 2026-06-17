module Contexts 

open Feliz
open Types

module ModalContext=

    let start:ModalInfo = {
            isActive = false
            location = (0,0)
            }
            
    let createModalContext:Fable.React.IContext<DropdownModal> = React.createContext(name="modal") //makes context


