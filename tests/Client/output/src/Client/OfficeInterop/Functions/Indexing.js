import { map, contains } from "../../../../fable_modules/fable-library.4.9.0/Array.js";
import { stringHash } from "../../../../fable_modules/fable-library.4.9.0/Util.js";
import { value } from "../../../../fable_modules/fable-library.4.9.0/Option.js";
import { BuildingBlockType__get_isSingleColumn, BuildingBlockNamePrePrint__toAnnotationTableHeader, ColumnCoreNames, ColumnCoreNames__get_toString } from "../../../Shared/OfficeInteropTypes.js";
import { empty, singleton, append, delay, toArray } from "../../../../fable_modules/fable-library.4.9.0/Seq.js";

/**
 * This is based on a excel hack on how to add multiple header of the same name to an excel table.,
 * by just appending more whitespace to the name.
 */
export function extendName(existingNames, baseName) {
    const loop = (baseName_1_mut) => {
        loop:
        while (true) {
            const baseName_1 = baseName_1_mut;
            if (contains(baseName_1, existingNames, {
                Equals: (x, y) => (x === y),
                GetHashCode: stringHash,
            })) {
                baseName_1_mut = (baseName_1 + " ");
                continue loop;
            }
            else {
                return baseName_1;
            }
            break;
        }
    };
    return loop(baseName);
}

export function createTSR(newBB) {
    const termAccession = (newBB.ColumnTerm != null) ? value(newBB.ColumnTerm).TermAccession : "";
    return `${ColumnCoreNames__get_toString(new ColumnCoreNames(0, []))} (${termAccession})`;
}

export function createTAN(newBB) {
    const termAccession = (newBB.ColumnTerm != null) ? value(newBB.ColumnTerm).TermAccession : "";
    return `${ColumnCoreNames__get_toString(new ColumnCoreNames(1, []))} (${termAccession})`;
}

export function createUnit() {
    return `${ColumnCoreNames__get_toString(new ColumnCoreNames(2, []))}`;
}

export function createColumnNames(newBB, existingNames) {
    const mainColumn = BuildingBlockNamePrePrint__toAnnotationTableHeader(newBB.ColumnHeader);
    return map((baseName) => extendName(existingNames, baseName), toArray(delay(() => append(singleton(mainColumn), delay(() => append((newBB.UnitTerm != null) ? singleton(createUnit()) : empty(), delay(() => (!BuildingBlockType__get_isSingleColumn(newBB.ColumnHeader.Type) ? append(singleton(createTSR(newBB)), delay(() => singleton(createTAN(newBB)))) : empty()))))))));
}

//# sourceMappingURL=Indexing.js.map
