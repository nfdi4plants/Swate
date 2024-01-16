namespace ARCtrl.ISA

open ARCtrl.ISA.Aux
open Regex.ActivePatterns

type Process = 
    {
        ID : URI option
        Name : string option
        ExecutesProtocol : Protocol option
        ParameterValues : ProcessParameterValue list option
        Performer : string option
        Date : string option
        PreviousProcess : Process  option
        NextProcess : Process option
        Inputs : ProcessInput list option
        Outputs : ProcessOutput list option
        Comments : Comment list option
    }

    static member make id name executesProtocol parameterValues performer date previousProcess nextProcess inputs outputs comments : Process= 
        {       
            ID                  = id
            Name                = name
            ExecutesProtocol    = executesProtocol
            ParameterValues     = parameterValues
            Performer           = performer
            Date                = date
            PreviousProcess     = previousProcess
            NextProcess         = nextProcess
            Inputs              = inputs
            Outputs             = outputs
            Comments            = comments       
        }

    static member create (?Id,?Name,?ExecutesProtocol,?ParameterValues,?Performer,?Date,?PreviousProcess,?NextProcess,?Inputs,?Outputs,?Comments) : Process= 
        Process.make Id Name ExecutesProtocol ParameterValues Performer Date PreviousProcess NextProcess Inputs Outputs Comments

    static member empty =
        Process.create()

    interface IISAPrintable with
        member this.Print() = 
            this.ToString()
        member this.PrintCompact() =
            let inputCount = this.Inputs |> Option.defaultValue [] |> List.length
            let outputCount = this.Outputs |> Option.defaultValue [] |> List.length
            let paramCount = this.ParameterValues |> Option.defaultValue [] |> List.length

            let name = this.Name |> Option.defaultValue "Unnamed Process"

            sprintf "%s [%i Inputs -> %i Params -> %i Outputs]" name inputCount paramCount outputCount
            
    static member composeName (processNameRoot : string) (i : int) =
        $"{processNameRoot}_{i}"

    static member decomposeName (name : string) =
        let pattern = """(?<name>.+)_(?<num>\d+)"""

        match name with 
        | Regex pattern r ->
            (r.Groups.Item "name").Value, Some ((r.Groups.Item "num").Value |> int)
        | _ ->
            name, None
    
    /// Returns the name of the protocol the given process executes
    static member tryGetProtocolName (p: Process) =
        p.ExecutesProtocol
        |> Option.bind (fun p -> p.Name)

    /// Returns the name of the protocol the given process executes
    static member getProtocolName (p: Process) =
        p.ExecutesProtocol
        |> Option.bind (fun p -> p.Name)
        |> Option.get 

    /// Returns the parameter values describing the process
    static member getParameterValues (p: Process) =
        p.ParameterValues |> Option.defaultValue []

    /// Returns the parameters describing the process
    static member getParameters (p: Process) =
        Process.getParameterValues p
        |> List.choose (fun pv -> pv.Category)

    /// Returns the characteristics describing the inputs of the process
    static member getInputCharacteristicValues (p: Process) =
        match p.Inputs with
        | Some ins ->
            ins 
            |> List.collect (fun inp -> ProcessInput.tryGetCharacteristicValues inp |> Option.defaultValue [])
            |> List.distinct
        | None -> []

    /// Returns the characteristics describing the outputs of the process
    static member getOutputCharacteristicValues (p: Process) =
        match p.Outputs with
        | Some outs ->
            outs 
            |> List.collect (fun out -> ProcessOutput.tryGetCharacteristicValues out |> Option.defaultValue [])
            |> List.distinct
        | None -> []

    /// Returns the characteristic values describing the inputs and outputs of the process
    static member getCharacteristicValues (p: Process) =
        Process.getInputCharacteristicValues p @ Process.getOutputCharacteristicValues p
        |> List.distinct

    /// Returns the characteristics describing the inputs and outputs of the process
    static member getCharacteristics (p: Process) =
        Process.getCharacteristicValues p
        |> List.choose (fun cv -> cv.Category)
        |> List.distinct

    /// Returns the factor values of the samples of the process
    static member getFactorValues (p : Process) =
        p.Outputs |> Option.defaultValue [] |> List.collect (ProcessOutput.tryGetFactorValues >> Option.defaultValue [])
        |> List.distinct

    /// Returns the factors of the samples of the process
    static member getFactors (p : Process) =
        Process.getFactorValues p
        |> List.choose (fun fv -> fv.Category)
        |> List.distinct

    /// Returns the units of the process
    static member getUnits (p : Process) =
        (Process.getCharacteristicValues p |> List.choose (fun cv -> cv.Unit))
        @ (Process.getParameterValues p |> List.choose (fun pv -> pv.Unit))
        @ (Process.getFactorValues p |> List.choose (fun fv -> fv.Unit))

    /// If the process implements the given parameter, return the list of input files together with their according parameter values of this parameter
    static member tryGetInputsWithParameterBy (predicate : ProtocolParameter -> bool) (p : Process) =
        match p.ParameterValues with
        | Some paramValues ->
            match paramValues |> List.tryFind (fun pv -> Option.defaultValue ProtocolParameter.empty pv.Category |> predicate ) with
            | Some paramValue ->
                p.Inputs
                |> Option.map (List.map (fun i -> i,paramValue))
            | None -> None
        | None -> None

    
    /// If the process implements the given parameter, return the list of output files together with their according parameter values of this parameter
    static member tryGetOutputsWithParameterBy (predicate : ProtocolParameter -> bool) (p : Process) =
        match p.ParameterValues with
        | Some paramValues ->
            match paramValues |> List.tryFind (fun pv -> Option.defaultValue ProtocolParameter.empty pv.Category |> predicate ) with
            | Some paramValue ->
                p.Outputs
                |> Option.map (List.map (fun i -> i,paramValue))
            | None -> None
        | None -> None

    /// If the process implements the given characteristic, return the list of input files together with their according characteristic values of this characteristic
    static member tryGetInputsWithCharacteristicBy (predicate : MaterialAttribute -> bool) (p : Process) =
        match p.Inputs with
        | Some is ->
            is
            |> List.choose (fun i ->
                ProcessInput.tryGetCharacteristicValues i
                |> Option.defaultValue []
                |> List.tryPick (fun mv -> 
                    match mv.Category with
                    | Some m when predicate m -> Some (i,mv)
                    | _ -> None

                )
            )
            |> Option.fromValueWithDefault []
        | None -> None

    /// If the process implements the given characteristic, return the list of output files together with their according characteristic values of this characteristic
    static member tryGetOutputsWithCharacteristicBy (predicate : MaterialAttribute -> bool) (p : Process) =
        match  p.Inputs, p.Outputs with
        | Some is,Some os ->
            List.zip is os
            |> List.choose (fun (i,o) ->
                ProcessInput.tryGetCharacteristicValues i
                |> Option.defaultValue []
                |> List.tryPick (fun mv -> 
                    match mv.Category with
                    | Some m when predicate m -> Some (o,mv)
                    | _ -> None
                )
            )
            |> Option.fromValueWithDefault []
        | _ -> None


    /// If the process implements the given factor, return the list of output files together with their according factor values of this factor
    static member tryGetOutputsWithFactorBy (predicate : Factor -> bool) (p : Process) =
        match p.Outputs with
        | Some os ->
            os
            |> List.choose (fun o ->
                ProcessOutput.tryGetFactorValues o
                |> Option.defaultValue []
                |> List.tryPick (fun mv -> 
                    match mv.Category with
                    | Some m when predicate m -> Some (o,mv)
                    | _ -> None

                )
            )
            |> Option.fromValueWithDefault []
        | None -> None
        
    static member getSources (p : Process) =
        p.Inputs |> Option.defaultValue [] |> List.choose ProcessInput.trySource
        |> List.distinct

    static member getData (p : Process) =
        (p.Inputs |> Option.defaultValue [] |> List.choose ProcessInput.tryData)
        @
        (p.Inputs |> Option.defaultValue [] |> List.choose ProcessInput.tryData)
        |> List.distinct

    static member getSamples (p : Process) =
        (p.Inputs |> Option.defaultValue [] |> List.choose ProcessInput.trySample)
        @
        (p.Inputs |> Option.defaultValue [] |> List.choose ProcessInput.trySample)
        |> List.distinct       
        
    static member getMaterials (p : Process) =
        (p.Inputs |> Option.defaultValue [] |> List.choose ProcessInput.tryMaterial)
        @
        (p.Inputs |> Option.defaultValue [] |> List.choose ProcessInput.tryMaterial)
        |> List.distinct

    static member updateProtocol (referenceProtocols : Protocol list) (p : Process) =
        match p.ExecutesProtocol with
        | Some protocol when protocol.Name.IsSome ->
            match referenceProtocols |> List.tryFind (fun prot -> prot.Name.Value = (protocol.Name |> Option.defaultValue "")) with
            | Some refProtocol ->
                {p with ExecutesProtocol = Some (Update.UpdateByExistingAppendLists.updateRecordType protocol refProtocol)}
            | _ -> p
        | _ -> p
