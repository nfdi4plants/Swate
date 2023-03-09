module BuildingBlock.Helper

open Shared
open Messages
open TermTypes
open Fulma
open Fable.React
open Fable.React.Props
open Fable.FontAwesome
open ExcelColors
open Model
open Messages.BuildingBlock
open OfficeInteropTypes

let isValidBuildingBlock (block : BuildingBlockNamePrePrint) =
    if block.Type.isFeaturedColumn then true
    elif block.Type.isTermColumn then block.Name.Length > 0
    elif block.Type.isSingleColumn then true
    else false