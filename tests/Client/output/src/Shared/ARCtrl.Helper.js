import { toString, Union } from "../../fable_modules/fable-library.4.9.0/Types.js";
import { Template_$reflection } from "../../fable_modules/ARCtrl.1.0.4/Templates/Template.fs.js";
import { ArcAssay_$reflection, ArcStudy_$reflection, ArcInvestigation_$reflection } from "../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTypes.fs.js";
import { union_type, list_type } from "../../fable_modules/fable-library.4.9.0/Reflection.js";
import { ArcTables } from "../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/ArcTables.fs.js";
import { printf, toFail, replace } from "../../fable_modules/fable-library.4.9.0/String.js";
import { IOType, CompositeHeader } from "../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeHeader.fs.js";
import { OntologyAnnotation } from "../../fable_modules/ARCtrl.ISA.1.0.4/JsonTypes/OntologyAnnotation.fs.js";
import { value } from "../../fable_modules/fable-library.4.9.0/Option.js";
import { CompositeCell } from "../../fable_modules/ARCtrl.ISA.1.0.4/ArcTypes/CompositeCell.fs.js";
import { TermTypes_TermMinimal_create } from "./TermTypes.js";

export class ARCtrlHelper_ArcFiles extends Union {
    constructor(tag, fields) {
        super();
        this.tag = tag;
        this.fields = fields;
    }
    cases() {
        return ["Template", "Investigation", "Study", "Assay"];
    }
}

export function ARCtrlHelper_ArcFiles_$reflection() {
    return union_type("Shared.ARCtrlHelper.ArcFiles", [], ARCtrlHelper_ArcFiles, () => [[["Item", Template_$reflection()]], [["Item", ArcInvestigation_$reflection()]], [["Item1", ArcStudy_$reflection()], ["Item2", list_type(ArcAssay_$reflection())]], [["Item", ArcAssay_$reflection()]]]);
}

export function ARCtrlHelper_ArcFiles__Tables(this$) {
    switch (this$.tag) {
        case 1:
            return new ArcTables([]);
        case 2:
            return this$.fields[0];
        case 3:
            return this$.fields[0];
        default:
            return new ArcTables([this$.fields[0].Table]);
    }
}

export function ARCtrl_Template_Template__Template_get_FileName(this$) {
    return replace(this$.Name, " ", "_") + ".xlsx";
}

export function ARCtrl_ISA_CompositeHeader__CompositeHeader_get_AsButtonName(this$) {
    switch (this$.tag) {
        case 3:
            return "Parameter";
        case 1:
            return "Characteristic";
        case 0:
            return "Component";
        case 2:
            return "Factor";
        case 11:
            return "Input";
        case 12:
            return "Output";
        default:
            return toString(this$);
    }
}

export function ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateWithOA_Z4C0FE73C(this$, oa) {
    switch (this$.tag) {
        case 0:
            return new CompositeHeader(0, [oa]);
        case 3:
            return new CompositeHeader(3, [oa]);
        case 1:
            return new CompositeHeader(1, [oa]);
        case 2:
            return new CompositeHeader(2, [oa]);
        default:
            return toFail(printf("Cannot update OntologyAnnotation on CompositeHeader without OntologyAnnotation: \'%A\'"))(this$);
    }
}

export function ARCtrl_ISA_CompositeHeader__CompositeHeader_get_ParameterEmpty_Static() {
    return new CompositeHeader(3, [OntologyAnnotation.empty]);
}

export function ARCtrl_ISA_CompositeHeader__CompositeHeader_get_CharacteristicEmpty_Static() {
    return new CompositeHeader(1, [OntologyAnnotation.empty]);
}

export function ARCtrl_ISA_CompositeHeader__CompositeHeader_get_ComponentEmpty_Static() {
    return new CompositeHeader(0, [OntologyAnnotation.empty]);
}

export function ARCtrl_ISA_CompositeHeader__CompositeHeader_get_FactorEmpty_Static() {
    return new CompositeHeader(2, [OntologyAnnotation.empty]);
}

export function ARCtrl_ISA_CompositeHeader__CompositeHeader_get_InputEmpty_Static() {
    return new CompositeHeader(11, [new IOType(6, [""])]);
}

export function ARCtrl_ISA_CompositeHeader__CompositeHeader_get_OutputEmpty_Static() {
    return new CompositeHeader(12, [new IOType(6, [""])]);
}

/**
 * Keep the outer `CompositeHeader` information (e.g.: Parameter, Factor, Input, Output..) and update the inner "of" value with the value from `other.`
 * This will only run successfully if the inner values are of the same type
 */
export function ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateDeepWith_Z331CE692(this$, other) {
    if (this$.IsIOType && other.IsIOType) {
        const h1_2 = this$;
        const io1 = value(other.TryIOType());
        switch (h1_2.tag) {
            case 11:
                return new CompositeHeader(11, [io1]);
            case 12:
                return new CompositeHeader(12, [io1]);
            default:
                throw new Error("Error 1 in UpdateSurfaceTo. This should never hit.");
        }
    }
    else if (((this$.IsTermColumn && other.IsTermColumn) && !this$.IsFeaturedColumn) && !other.IsFeaturedColumn) {
        return ARCtrl_ISA_CompositeHeader__CompositeHeader_UpdateWithOA_Z4C0FE73C(this$, other.ToTerm());
    }
    else {
        return this$;
    }
}

export function ARCtrl_ISA_CompositeCell__CompositeCell_UpdateWithOA_Z4C0FE73C(this$, oa) {
    switch (this$.tag) {
        case 2:
            return CompositeCell.createUnitized(this$.fields[0], oa);
        case 1:
            return CompositeCell.createFreeText(oa.NameText);
        default:
            return CompositeCell.createTerm(oa);
    }
}

export function ARCtrl_ISA_CompositeCell__CompositeCell_ToTerm(this$) {
    switch (this$.tag) {
        case 2:
            return this$.fields[1];
        case 1:
            return OntologyAnnotation.fromString(this$.fields[0]);
        default:
            return this$.fields[0];
    }
}

export function ARCtrl_ISA_CompositeCell__CompositeCell_UpdateMainField_Z721C83C5(this$, s) {
    switch (this$.tag) {
        case 2:
            return new CompositeCell(2, [s, this$.fields[1]]);
        case 1:
            return new CompositeCell(1, [s]);
        default: {
            const oa = this$.fields[0];
            return new CompositeCell(0, [new OntologyAnnotation(oa.ID, s, oa.TermSourceREF, oa.TermAccessionNumber, oa.Comments)]);
        }
    }
}

/**
 * Will return `this` if executed on Freetext cell.
 */
export function ARCtrl_ISA_CompositeCell__CompositeCell_UpdateTSR_Z721C83C5(this$, tsr) {
    const updateTSR = (oa) => (new OntologyAnnotation(oa.ID, oa.Name, tsr, oa.TermAccessionNumber, oa.Comments));
    switch (this$.tag) {
        case 0:
            return new CompositeCell(0, [updateTSR(this$.fields[0])]);
        case 2:
            return new CompositeCell(2, [this$.fields[0], updateTSR(this$.fields[1])]);
        default:
            return this$;
    }
}

/**
 * Will return `this` if executed on Freetext cell.
 */
export function ARCtrl_ISA_CompositeCell__CompositeCell_UpdateTAN_Z721C83C5(this$, tan) {
    const updateTAN = (oa) => (new OntologyAnnotation(oa.ID, oa.Name, oa.TermSourceREF, tan, oa.Comments));
    switch (this$.tag) {
        case 0:
            return new CompositeCell(0, [updateTAN(this$.fields[0])]);
        case 2:
            return new CompositeCell(2, [this$.fields[0], updateTAN(this$.fields[1])]);
        default:
            return this$;
    }
}

export function ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_fromTerm_Static_Z5E0A7659(term) {
    return OntologyAnnotation.fromString(term.Name, term.FK_Ontology, term.Accession);
}

export function ARCtrl_ISA_OntologyAnnotation__OntologyAnnotation_ToTermMinimal(this$) {
    return TermTypes_TermMinimal_create(this$.NameText, this$.TermAccessionShort);
}

//# sourceMappingURL=ARCtrl.Helper.js.map
