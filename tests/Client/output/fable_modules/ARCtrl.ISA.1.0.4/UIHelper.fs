module ARCtrl.ISA.UIHelper

module CompositeCell =
    /// <summary>
    /// Updates current CompositeCell with information from OntologyAnnotation.
    ///
    /// For `Term`, OntologyAnnotation (oa) is fully set. For `Unitized`, oa is set as unit while value is untouched.
    /// For `FreeText` oa.NameText is set.
    /// </summary>
    /// <param name="oa"></param>
    /// <param name="cell"></param>
    let updateWithOA (oa:OntologyAnnotation) (cell: CompositeCell) =
        match cell with
        | CompositeCell.Term _ -> CompositeCell.createTerm oa
        | CompositeCell.Unitized (v,_) -> CompositeCell.createUnitized (v,oa)
        | CompositeCell.FreeText _ -> CompositeCell.createFreeText oa.NameText
