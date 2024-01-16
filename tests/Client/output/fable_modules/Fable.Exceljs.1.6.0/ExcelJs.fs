module Fable.ExcelJs.ExcelJs

open Fable.Core
open Fable.Core.JsInterop

type ExcelJS =
    [<Emit("new $0.Workbook()")>]
    abstract member Workbook: unit -> Workbook
    
///// exceljs is unable to parse xml nodes with global namespaces. OpenXml writes global namespaces to nearly all xml nodes with "x:".
///// The responsible function `BaseXform.prototype.parse` is overwritten here to remove this "x:" namespace. 
///// Allowing us to read in OpenXml generated xlsx files
//module internal PatchExcelJs =
//    // relevant links:
//    // - https://github.com/dotnet/Open-XML-SDK
//    // - https://github.com/exceljs/exceljs/issues/1437

//    let internal BaseXform : obj = importDefault "exceljs/lib/xlsx/xform/base-xform.js"
//    let internal RelationshipXform : obj = importDefault "exceljs/lib/xlsx/xform/core/relationship-xform.js"

//    let internal patch (obj:obj) =
//        emitJsStatement 
//            (obj)
//            """
//    $0.prototype.parse = async function(saxParser){
//      for await (const events of saxParser) {
//        for (const {eventType, value} of events) {
//          if(value.name && value.name.startsWith('x:')) value.name = value.name.slice(2);
//          if (eventType === 'opentag') {
//            this.parseOpen(value);
//          } else if (eventType === 'text') {
//            this.parseText(value);
//          } else if (eventType === 'closetag') {
//            if (!this.parseClose(value.name)) {
//              return this.model;
//            }
//          }
//        }
//      }
//      return this.model;
//    }"""

//    let internal patchTableRelationshipTarget (obj:obj) =
//        emitJsStatement 
//            (obj)
//            """
//    $0.prototype.parseOpen = function(node) {
//        switch (node.name) {
//          case 'Relationship':
//            node.attributes.Target = node.attributes.Target.replace("/xl/tables", "../tables")
//            this.model = node.attributes;
//            return true;
//          default:
//            return false;
//        }
//    }"""

//    patch(BaseXform)
//    patchTableRelationshipTarget(RelationshipXform)

let Excel: ExcelJS = importDefault "@nfdi4plants/exceljs"
