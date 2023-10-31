module BuildingBlock.Helper

open Shared
open OfficeInteropTypes

let isValidBuildingBlock (block : BuildingBlockNamePrePrint) =
    if block.Type.isFeaturedColumn then true
    elif block.Type.isTermColumn then block.Name.Length > 0
    elif block.Type.isSingleColumn then true
    else false