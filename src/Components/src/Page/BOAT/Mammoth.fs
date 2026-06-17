module Mammoth

open Fable.Core.JS
open Fable.Core.JsInterop

type IMammoth =
  abstract member convertToHtml: {|arrayBuffer: ArrayBuffer|} -> Promise<{|value: string|}>

let mammoth: IMammoth = importDefault "mammoth"