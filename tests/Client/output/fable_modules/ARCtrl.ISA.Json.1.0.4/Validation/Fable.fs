namespace ARCtrl.ISA.Json

module Fable =

    open Fable.Core
    open Fable.Core.JsInterop

    [<CompiledName("ValidationError")>]
    type JsValidationError = {
        /// Exmp. [Array]
        path: obj
        /// Exmp. 'instance.name'
        property: string
        /// Exmp. 'is not of a type(s) string'
        message: string
        /// Exmp. [Object]
        schema: obj
        /// Exmpl. 12
        instance: obj
        /// Exmp. 'type'
        name: string
        /// Exmp. [Array]
        argument: obj
        /// Exmp. 'instance.name is not of a type(s) string'
        stack: string
    } with
        member this.ToErrorString() =
            $"Property {this.property} ({this.instance}) {this.message}."
            

    [<CompiledName("ValidatorResult")>]
    type JsValidatorResult = {
        instance: obj
        schema: obj
        options: obj
        path: obj []
        propertyPath: string
        errors: JsValidationError []
        throwError: obj option
        throFirst: obj option
        throwAll: obj option
        disableFormat: bool
    } with
        member this.ToValidationResult() : (bool * string []) = 
            let hasNoErrors = Array.isEmpty this.errors
            match hasNoErrors with
            | true -> 
                true, [||]
            | false -> 
                let errors = this.errors |> Array.map (fun x -> x.ToErrorString())
                false, errors

    open Fable.Core.JS

    type IValidate =
        abstract validateAgainstSchema: jsonString:string * schemaUrl:string -> Promise<JsValidatorResult>
        abstract helloWorld: unit -> string

    [<ImportAll("./JsonValidation.js")>]
    let JsonValidation: IValidate = jsNative

    let validate (schemaURL : string) (objectString : string) = 
        let mutable validationResult = None
        async {
            do! JsonValidation.validateAgainstSchema(objectString, schemaURL).``then``(fun o ->
                    validationResult <- Some o
                )
                |> Async.AwaitPromise
            let output = 
                validationResult
                    .Value
                    .ToValidationResult() 
            return output
        }