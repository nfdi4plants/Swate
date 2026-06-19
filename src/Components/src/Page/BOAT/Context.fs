module Contexts 

open Feliz
open Types

module ModalContext=

    let start:ModalInfo = {
            isActive = false
            location = (0,0)
    }

    let defaultModule:DropdownModal = {
        modalState = start
        setter = fun _ -> ()
        }
            
    let createModalContext: ReactContext<DropdownModal> = React.createContext(defaultModule) //makes context


