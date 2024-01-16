module ARCtrl.ISA.ProcessSequence

open ARCtrl.ISA.Aux
open Update

/// Returns the names of the protocols the given processes impelement
let getProtocolNames (processSequence : Process list) =
    processSequence
    |> List.choose (fun p ->
        p.ExecutesProtocol
        |> Option.bind (fun protocol ->
            protocol.Name
        )        
    )
    |> List.distinct
        
/// Returns the protocols the given processes impelement
let getProtocols (processSequence : Process list) =
    processSequence
    |> List.choose (fun p -> p.ExecutesProtocol)
    |> List.distinct

/// Returns a list of the processes, containing only the ones executing a protocol for which the predicate returns true
let filterByProtocolBy (predicate : Protocol -> bool) (processSequence : Process list) =
    processSequence
    |> List.filter (fun p ->
        match p.ExecutesProtocol with
        | Some protocol when predicate protocol -> true
        | _ -> false
    )

/// Returns a list of the processes, containing only the ones executing a protocol with the given name
let filterByProtocolName (protocolName : string) (processSequence : Process list) =
    filterByProtocolBy (fun (p:Protocol) -> p.Name = Some protocolName) processSequence

/// If the processes contain a process implementing the given parameter, return the list of input files together with their according parameter values of this parameter
let getInputsWithParameterBy (predicate:ProtocolParameter -> bool) (processSequence : Process list) =
    processSequence
    |> List.choose (Process.tryGetInputsWithParameterBy predicate)
    |> List.concat
        
/// If the processes contain a process implementing the given parameter, return the list of output files together with their according parameter values of this parameter
let getOutputsWithParameterBy (predicate:ProtocolParameter -> bool) (processSequence : Process list) =
    processSequence
    |> List.choose (Process.tryGetOutputsWithParameterBy predicate)
    |> List.concat

/// Returns the parameters implemented by the processes contained in these processes
let getParameters (processSequence : Process list) =
    processSequence
    |> List.collect Process.getParameters
    |> List.distinct
    
/// Returns the characteristics describing the inputs and outputs of the processssequence
let getCharacteristics  (processSequence : Process list) =
    processSequence
    |> List.collect Process.getCharacteristics
    |> List.distinct

/// If the processes contain a process implementing the given parameter, return the list of input files together with their according parameter values of this parameter
let getInputsWithCharacteristicBy (predicate:MaterialAttribute -> bool) (processSequence : Process list) =
    processSequence
    |> List.choose (Process.tryGetInputsWithCharacteristicBy predicate)
    |> List.concat
        
/// If the processes contain a process implementing the given parameter, return the list of output files together with their according parameter values of this parameter
let getOutputsWithCharacteristicBy (predicate:MaterialAttribute -> bool) (processSequence : Process list) =
    processSequence
    |> List.choose (Process.tryGetOutputsWithCharacteristicBy predicate)
    |> List.concat

/// If the processes contain a process implementing the given factor, return the list of output files together with their according factor values of this factor
let getOutputsWithFactorBy (predicate:Factor -> bool) (processSequence : Process list) =
    processSequence
    |> List.choose (Process.tryGetOutputsWithFactorBy predicate)
    |> List.concat

/// Returns the factors implemented by the processes contained in these processes
let getFactors (processSequence : Process list) =
    processSequence
    |> List.collect Process.getFactors
    |> List.distinct

/// Returns the initial inputs final outputs of the processSequence, to which no processPoints
let getRootInputs (processSequence : Process list) =
    let inputs = processSequence |> List.collect (fun p -> p.Inputs |> Option.defaultValue [])
    let outputs = processSequence |> List.collect (fun p -> p.Outputs |> Option.defaultValue [] |> List.map ProcessOutput.getName) |> Set.ofList
    inputs
    |> List.filter (fun i -> ProcessInput.getName i |> outputs.Contains |> not)

/// Returns the final outputs of the processSequence, which point to no further nodes
let getFinalOutputs (processSequence : Process list) =
    let inputs = processSequence |> List.collect (fun p -> p.Inputs |> Option.defaultValue [] |> List.map ProcessInput.getName) |> Set.ofList
    let outputs = processSequence |> List.collect (fun p -> p.Outputs |> Option.defaultValue [])
    outputs
    |> List.filter (fun o -> ProcessOutput.getName o |> inputs.Contains |> not)

/// Returns the initial inputs final outputs of the processSequence, to which no processPoints
let getRootInputOf (processSequence : Process list) (sample : string) =
    let mappings = 
        processSequence 
        |> List.collect (fun p -> 
            List.zip 
                (p.Outputs.Value |> List.map (fun o -> o.Name)) 
                (p.Inputs.Value  |> List.map (fun i -> i.Name))
            |> List.distinct
        ) 
        |> List.groupBy fst 
        |> List.map (fun (out,ins) -> out, ins |> List.map snd)
        |> Map.ofList
    let rec loop lastState state = 
        if lastState = state then state 
        else
            let newState = 
                state 
                |> List.collect (fun s -> 
                    mappings.TryFind s 
                    |> Option.defaultValue [s]
                )
            loop state newState
    loop [] [sample]
        
/// Returns the final outputs of the processSequence, which point to no further nodes
let getFinalOutputsOf (processSequence : Process list) (sample : string) =
    let mappings = 
        processSequence 
        |> List.collect (fun p -> 
            List.zip 
                (p.Inputs.Value  |> List.map (fun i -> i.Name))
                (p.Outputs.Value |> List.map (fun o -> o.Name)) 
            |> List.distinct
        ) 
        |> List.groupBy fst 
        |> List.map (fun (inp,outs) -> inp, outs |> List.map snd)
        |> Map.ofList
    let rec loop lastState state = 
        if lastState = state then state 
        else
            let newState = 
                state 
                |> List.collect (fun s -> 
                    mappings.TryFind s 
                    |> Option.defaultValue [s]
                )
            loop state newState
    loop [] [sample]

let getUnits (processSequence : Process list) =
    List.collect Process.getUnits processSequence
    |> List.distinct
        
/// Returns the data the given processes contain
let getData (processSequence : Process list) =
    processSequence
    |> List.collect Process.getData
    |> List.distinct

let getSources (processSequence : Process list) =
    processSequence
    |> List.collect Process.getSources
    |> List.distinct

let getSamples (processSequence : Process list) =
    processSequence
    |> List.collect Process.getSamples
    |> List.distinct

let getMaterials (processSequence : Process list) =
    processSequence
    |> List.collect Process.getMaterials
    |> List.distinct

let updateProtocols (protocols : Protocol list) (processSequence : Process list) =
    processSequence
    |> List.map (Process.updateProtocol protocols)
