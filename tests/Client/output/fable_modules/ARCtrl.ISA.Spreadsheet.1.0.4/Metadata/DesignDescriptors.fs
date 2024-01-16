namespace ARCtrl.ISA.Spreadsheet

open ARCtrl.ISA
open Comment
open Remark
open System.Collections.Generic

module DesignDescriptors = 

    let designTypeLabel = "Type"
    let designTypeTermAccessionNumberLabel = "Type Term Accession Number"
    let designTypeTermSourceREFLabel = "Type Term Source REF"

    let labels = [designTypeLabel;designTypeTermAccessionNumberLabel;designTypeTermSourceREFLabel]

    let fromSparseTable (matrix : SparseTable) =
        OntologyAnnotationSection.fromSparseTable designTypeLabel designTypeTermSourceREFLabel designTypeTermAccessionNumberLabel matrix

    let toSparseTable (designs: OntologyAnnotation list) =
        OntologyAnnotationSection.toSparseTable designTypeLabel designTypeTermSourceREFLabel designTypeTermAccessionNumberLabel designs

    let fromRows (prefix : string option) lineNumber (rows : IEnumerator<SparseRow>) =
        OntologyAnnotationSection.fromRows prefix designTypeLabel designTypeTermSourceREFLabel designTypeTermAccessionNumberLabel lineNumber rows
    
    let toRows (prefix : string option) (designs : OntologyAnnotation list) =
        OntologyAnnotationSection.toRows prefix designTypeLabel designTypeTermSourceREFLabel designTypeTermAccessionNumberLabel designs