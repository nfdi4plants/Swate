module ARCtrl.ISA.CompositeRow

let toProtocol (tableName : string) (row : (CompositeHeader*CompositeCell) seq) =
    row
    |> Seq.fold (fun p hc ->
        match hc with
        | CompositeHeader.ProtocolType, CompositeCell.Term oa -> 
            Protocol.setProtocolType p oa
        | CompositeHeader.ProtocolVersion, CompositeCell.FreeText v -> Protocol.setVersion p v
        | CompositeHeader.ProtocolUri, CompositeCell.FreeText v -> Protocol.setUri p v
        | CompositeHeader.ProtocolDescription, CompositeCell.FreeText v -> Protocol.setDescription p v
        | CompositeHeader.ProtocolREF, CompositeCell.FreeText v -> Protocol.setName p v
        | CompositeHeader.Parameter oa, _ -> 
            let pp = ProtocolParameter.create(ParameterName = oa)
            Protocol.addParameter (pp) p
        | CompositeHeader.Component oa, CompositeCell.Unitized(v,unit) -> 
            let c = Component.create(ComponentType = oa, Value = Value.fromString v, Unit = unit)
            Protocol.addComponent c p        
        | CompositeHeader.Component oa, CompositeCell.Term t -> 
            let c = Component.create(ComponentType = oa, Value = Value.Ontology t)
            Protocol.addComponent c p     
        | _ -> p
    ) (Protocol.create(Name = tableName))