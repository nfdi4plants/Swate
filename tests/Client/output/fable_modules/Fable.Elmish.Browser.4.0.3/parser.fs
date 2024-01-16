(** UrlParser
------
Parsers designed to be used with `Navigation` module to help folks create
single-page applications (SPAs) where you manage browser navigation yourself.
*)

module Elmish.UrlParser
open Browser.Types
open Elmish.UrlParser

(**
#### Parsers
Parse based on `location.pathname` and `location.search`. This parser
ignores the hash entirely.
*)
let parsePath (parser: Parser<_,_>) (location: Location) =
    parse parser location.pathname (parseParams location.search)

(** Parse based on `location.hash`. This parser ignores the normal
path entirely.
*)
let parseHash (parser: Parser<_,_>) (location: Location) =
    let hash, queryParams =
        let hash =
            if location.hash.Length > 1 then location.hash.Substring 1
            else ""
        let pos = hash.IndexOf "?"
        if pos >= 0 then
            let path = hash.Substring(0,pos)
            let search = hash.Substring(pos+1)
            path, parseParams search
        else
            hash, Map.empty

    parse parser hash queryParams
