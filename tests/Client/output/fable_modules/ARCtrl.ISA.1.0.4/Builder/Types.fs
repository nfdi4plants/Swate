namespace ARCtrl.ISA.Builder

open ARCtrl.ISA

type ProtocolTransformation =
    
    | AddName of string
    | AddProtocolType of OntologyAnnotation
    | AddDescription of string

    member this.Transform(p : Protocol) =
        match this with
        | AddName n -> 
            {p with Name = Some n}
        | AddProtocolType t -> 
            {p with ProtocolType = Some t}
        | AddDescription d -> 
            {p with Description = Some d}

    member this.Equals(p : Protocol) =
        match this,p.Name with
        | AddName n, Some n' when n = n'-> 
            true
        | _ -> false

type ProcessTransformation = 

    | AddName of string

    | AddParameter of ProcessParameterValue
    | AddCharacteristic of MaterialAttributeValue
    | AddFactor of FactorValue

    //| RemoveParameter of ProcessParameterValue
    //| RemoveCharacteristic of MaterialAttributeValue
    //| RemoveFactor of FactorValue

    //| TransformProtocol of ProtocolTransformation list
    | AddProtocol of ProtocolTransformation list

    member this.Transform(p : Process) =
        match this with
        | AddName n -> 
            {p with Name = Some n}
        | AddParameter pv -> 
            let parameterValues = 
                (p.ParameterValues |> Option.defaultValue []) @ [pv]
            let protocol = 
                let pro = p.ExecutesProtocol |> Option.defaultValue Protocol.empty
                {pro with Parameters = Some ((pro.Parameters |> Option.defaultValue []) @ [pv.Category.Value])}
            {p with 
                ParameterValues = Some parameterValues
                ExecutesProtocol = Some protocol
                }
        | AddCharacteristic c -> 
            let inputs = 
                p.Inputs 
                |> Option.map (fun i -> 
                    //Add characteristic
                    //API.ProcessInput.addCharacteristics c i
                    i
                )
            {p with Inputs = inputs}
        | AddFactor f ->
            let outputs = 
                p.Outputs 
                |> Option.map (fun i -> 
                    //Add characteristic
                    //API.ProcessInput.addCharacteristics c i
                    i
                )
            {p with Outputs = outputs}
        //| TransformProtocol of ProtocolTransformation list
        | AddProtocol pts ->
            let protocol = 
                p.ExecutesProtocol 
                |> Option.defaultValue Protocol.empty
                |> fun pro -> 
                    pts
                    |> List.fold (fun pro trans -> trans.Transform(pro)) pro 
            {p with ExecutesProtocol = Some protocol}

    member this.Equals(p : Process) =
        match this,p.Name with
        | AddName n, Some n' when n = n'-> 
            true
        | _ -> false

type AssayTransformation = 
    
    | AddFileName of string

    | AddParameter of ProcessParameterValue
    | AddCharacteristic of MaterialAttributeValue
    | AddFactor of FactorValue


    | AddProcess of ProcessTransformation list

    member this.Transform(a : Assay) = 
        
        match this with
        //| AddParameter of ProcessParameterValue
        //| AddCharacteristic of MaterialAttributeValue
        //| AddFactor of FactorValue
        | AddProcess pts -> 
            let processes = a.ProcessSequence |> Option.defaultValue []
            let processes' = 
                if processes |> List.exists (fun p -> pts |> List.exists (fun trans -> trans.Equals p)) then
                    processes |> List.map (fun p ->
                        if pts |> List.exists (fun trans -> trans.Equals p) then
                            pts
                            |> List.fold (fun p trans -> trans.Transform(p)) p
                        else p
                    )
                else 
                    let newProcess = 
                        pts
                        |> List.fold (fun p trans -> trans.Transform(p)) Process.empty
                    processes @ [newProcess]
            {a with ProcessSequence = Some processes'}
        | AddFileName name ->
            {a with FileName = Some name}
        | x -> failwithf "Builder failed: Case %O Not implemented" x

    member this.Equals(a : Assay) =
        match this,a.FileName with
        | AddFileName n, Some n' when n = n'-> 
            true
        | _ -> false

type StudyTransformation = 
    
    | AddParameter of ProcessParameterValue
    | AddCharacteristic of MaterialAttributeValue
    | AddFactor of FactorValue

    //| RemoveParameter of ProcessParameterValue
    //| RemoveCharacteristic of MaterialAttributeValue
    //| RemoveFactor of FactorValue

    | AddProcess of ProcessTransformation list
    | AddAssay of AssayTransformation list


    member this.Transform(s : Study) = 
        match this with
        //| AddParameter of ProcessParameterValue
        //| AddCharacteristic of MaterialAttributeValue
        //| AddFactor of FactorValue
        | AddProcess pts -> 
            let processes = s.ProcessSequence |> Option.defaultValue []
            let processes' = 
                if processes |> List.exists (fun p -> pts |> List.exists (fun trans -> trans.Equals p)) then
                    processes |> List.map (fun p ->
                        if pts |> List.exists (fun trans -> trans.Equals p) then
                            pts
                            |> List.fold (fun p trans -> trans.Transform(p)) p
                        else p
                    )
                else 
                    let newProcess = 
                        pts
                        |> List.fold (fun p trans -> trans.Transform(p)) Process.empty
                    processes @ [newProcess]
            {s with ProcessSequence = Some processes'}
        | AddAssay ats ->
            let assays = s.Assays |> Option.defaultValue []
            let assays' = 
                if assays |> List.exists (fun a -> ats |> List.exists (fun trans -> trans.Equals a)) then
                    assays |> List.map (fun a ->
                        if ats |> List.exists (fun trans -> trans.Equals a) then
                            ats
                            |> List.fold (fun a trans -> trans.Transform(a)) a
                        else a
                    )
                else 
                    let newAssay = 
                        ats
                        |> List.fold (fun a trans -> trans.Transform(a)) Assay.empty
                    assays @ [newAssay]
            {s with Assays = Some assays'}
        | x -> failwithf "Builder failed: Case %O Not implemented" x