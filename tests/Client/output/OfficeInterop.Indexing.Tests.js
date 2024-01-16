import { BuildingBlockNamePrePrint__toAnnotationTableHeader, BuildingBlockType, BuildingBlockNamePrePrint_create, InsertBuildingBlock_create } from "./src/Shared/OfficeInteropTypes.js";
import { TermTypes_TermMinimal_create } from "./src/Shared/TermTypes.js";
import { append } from "./fable_modules/fable-library.4.9.0/Array.js";
import { extendName, createTAN, createTSR, createUnit, createColumnNames } from "./src/Client/OfficeInterop/Functions/Indexing.js";
import { Expect_all, Expect_exists, Expect_hasLength, Test_testCase, Test_testList } from "./fable_modules/Fable.Mocha.2.17.0/Mocha.fs.js";
import { singleton, append as append_1, delay, toList } from "./fable_modules/fable-library.4.9.0/Seq.js";
import { structuralHash, assertEqual } from "./fable_modules/fable-library.4.9.0/Util.js";
import { ofArray, contains } from "./fable_modules/fable-library.4.9.0/List.js";
import { equals, class_type, decimal_type, float64_type, bool_type, int32_type, string_type } from "./fable_modules/fable-library.4.9.0/Reflection.js";
import { printf, toText } from "./fable_modules/fable-library.4.9.0/String.js";

export const Instrument_Model = InsertBuildingBlock_create(BuildingBlockNamePrePrint_create(new BuildingBlockType(3, []), "instrument model"), TermTypes_TermMinimal_create("instrument model", "MS:1000031"), void 0, []);

export const Centrifugation_Time_MinuteUnit = InsertBuildingBlock_create(BuildingBlockNamePrePrint_create(new BuildingBlockType(0, []), "Centrifugation Time"), TermTypes_TermMinimal_create("Centrifugation Time", "NCIT:C178881"), TermTypes_TermMinimal_create("minute", "UO:0000031"), []);

/**
 * iteratively create column names
 */
export function loop(names_mut, buildingBlocks_mut, ind_mut) {
    loop:
    while (true) {
        const names = names_mut, buildingBlocks = buildingBlocks_mut, ind = ind_mut;
        if (ind >= buildingBlocks.length) {
            return names;
        }
        else {
            names_mut = append(names, createColumnNames(buildingBlocks[ind], names));
            buildingBlocks_mut = buildingBlocks;
            ind_mut = (ind + 1);
            continue loop;
        }
        break;
    }
}

export const tests_OfficeInterop_Indexing = Test_testList("OfficeInterop_Indexing", toList(delay(() => append_1(singleton(Test_testCase("createUnit", () => {
    let copyOfStruct;
    const actual = createUnit();
    if ((actual === "Unit") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual, "Unit", "");
    }
    else {
        throw new Error(contains((copyOfStruct = actual, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Unit")(actual)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Unit")(actual)(""));
    }
})), delay(() => append_1(singleton(Test_testCase("createTSR", () => {
    let copyOfStruct_1, copyOfStruct_2;
    const res_Instrument_Model = createTSR(Instrument_Model);
    const res_Centrifugation_Time_MinuteUnit = createTSR(Centrifugation_Time_MinuteUnit);
    const actual_1 = res_Instrument_Model;
    if ((actual_1 === "Term Source REF (MS:1000031)") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_1, "Term Source REF (MS:1000031)", "expected1");
    }
    else {
        throw new Error(contains((copyOfStruct_1 = actual_1, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Source REF (MS:1000031)")(actual_1)("expected1") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Source REF (MS:1000031)")(actual_1)("expected1"));
    }
    const actual_2 = res_Centrifugation_Time_MinuteUnit;
    if ((actual_2 === "Term Source REF (NCIT:C178881)") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_2, "Term Source REF (NCIT:C178881)", "expected2");
    }
    else {
        throw new Error(contains((copyOfStruct_2 = actual_2, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Source REF (NCIT:C178881)")(actual_2)("expected2") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Source REF (NCIT:C178881)")(actual_2)("expected2"));
    }
})), delay(() => append_1(singleton(Test_testCase("createTAN", () => {
    let copyOfStruct_3, copyOfStruct_4;
    const res_Instrument_Model_1 = createTAN(Instrument_Model);
    const res_Centrifugation_Time_MinuteUnit_1 = createTAN(Centrifugation_Time_MinuteUnit);
    const actual_3 = res_Instrument_Model_1;
    if ((actual_3 === "Term Accession Number (MS:1000031)") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_3, "Term Accession Number (MS:1000031)", "expected1");
    }
    else {
        throw new Error(contains((copyOfStruct_3 = actual_3, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Accession Number (MS:1000031)")(actual_3)("expected1") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Accession Number (MS:1000031)")(actual_3)("expected1"));
    }
    const actual_4 = res_Centrifugation_Time_MinuteUnit_1;
    if ((actual_4 === "Term Accession Number (NCIT:C178881)") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_4, "Term Accession Number (NCIT:C178881)", "expected2");
    }
    else {
        throw new Error(contains((copyOfStruct_4 = actual_4, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Accession Number (NCIT:C178881)")(actual_4)("expected2") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Accession Number (NCIT:C178881)")(actual_4)("expected2"));
    }
})), delay(() => append_1(singleton(Test_testCase("toAnnotationHeader", () => {
    let copyOfStruct_5, copyOfStruct_6;
    const res_Instrument_Model_2 = BuildingBlockNamePrePrint__toAnnotationTableHeader(Instrument_Model.ColumnHeader);
    const res_Centrifugation_Time_MinuteUnit_2 = BuildingBlockNamePrePrint__toAnnotationTableHeader(Centrifugation_Time_MinuteUnit.ColumnHeader);
    const actual_5 = res_Instrument_Model_2;
    if ((actual_5 === "Component [instrument model]") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_5, "Component [instrument model]", "expected1");
    }
    else {
        throw new Error(contains((copyOfStruct_5 = actual_5, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Component [instrument model]")(actual_5)("expected1") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Component [instrument model]")(actual_5)("expected1"));
    }
    const actual_6 = res_Centrifugation_Time_MinuteUnit_2;
    if ((actual_6 === "Parameter [Centrifugation Time]") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_6, "Parameter [Centrifugation Time]", "expected2");
    }
    else {
        throw new Error(contains((copyOfStruct_6 = actual_6, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Parameter [Centrifugation Time]")(actual_6)("expected2") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Parameter [Centrifugation Time]")(actual_6)("expected2"));
    }
})), delay(() => {
    const existingNames = loop([], [Instrument_Model, Centrifugation_Time_MinuteUnit], 0);
    return append_1(singleton(Test_testCase("checkExistingNames", () => {
        Expect_hasLength(existingNames, 7, "hasLength");
        Expect_exists(existingNames, (x_7) => (x_7 === "Component [instrument model]"), "exists: Component [instrument model]");
        Expect_exists(existingNames, (x_8) => (x_8 === "Parameter [Centrifugation Time]"), "exists: Parameter [Centrifugation Time]");
        Expect_exists(existingNames, (x_9) => (x_9 === "Unit"), "exists: Unit");
    })), delay(() => append_1(singleton(Test_testCase("extendName_mainColumn", () => {
        let copyOfStruct_7;
        const actual_7 = extendName(existingNames, BuildingBlockNamePrePrint__toAnnotationTableHeader(Instrument_Model.ColumnHeader));
        if ((actual_7 === "Component [instrument model] ") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
            assertEqual(actual_7, "Component [instrument model] ", "");
        }
        else {
            throw new Error(contains((copyOfStruct_7 = actual_7, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                Equals: equals,
                GetHashCode: structuralHash,
            }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Component [instrument model] ")(actual_7)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Component [instrument model] ")(actual_7)(""));
        }
    })), delay(() => append_1(singleton(Test_testCase("extendName_unit", () => {
        let copyOfStruct_8;
        const actual_8 = extendName(existingNames, createUnit());
        if ((actual_8 === "Unit ") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
            assertEqual(actual_8, "Unit ", "");
        }
        else {
            throw new Error(contains((copyOfStruct_8 = actual_8, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                Equals: equals,
                GetHashCode: structuralHash,
            }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Unit ")(actual_8)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Unit ")(actual_8)(""));
        }
    })), delay(() => append_1(singleton(Test_testCase("extendName_TSR", () => {
        let copyOfStruct_9, copyOfStruct_10;
        const res_Instrument_Model_3 = createTSR(Instrument_Model);
        const res_Centrifugation_Time_MinuteUnit_3 = createTSR(Centrifugation_Time_MinuteUnit);
        const res1 = extendName(existingNames, res_Instrument_Model_3);
        const res2 = extendName(existingNames, res_Centrifugation_Time_MinuteUnit_3);
        const actual_9 = res1;
        if ((actual_9 === "Term Source REF (MS:1000031) ") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
            assertEqual(actual_9, "Term Source REF (MS:1000031) ", "expected1");
        }
        else {
            throw new Error(contains((copyOfStruct_9 = actual_9, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                Equals: equals,
                GetHashCode: structuralHash,
            }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Source REF (MS:1000031) ")(actual_9)("expected1") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Source REF (MS:1000031) ")(actual_9)("expected1"));
        }
        const actual_10 = res2;
        if ((actual_10 === "Term Source REF (NCIT:C178881) ") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
            assertEqual(actual_10, "Term Source REF (NCIT:C178881) ", "expected2");
        }
        else {
            throw new Error(contains((copyOfStruct_10 = actual_10, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
                Equals: equals,
                GetHashCode: structuralHash,
            }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Source REF (NCIT:C178881) ")(actual_10)("expected2") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Term Source REF (NCIT:C178881) ")(actual_10)("expected2"));
        }
    })), delay(() => singleton(Test_testCase("extendName_deep", () => {
        const existingNames_1 = loop([], [Instrument_Model, Centrifugation_Time_MinuteUnit, Centrifugation_Time_MinuteUnit, Centrifugation_Time_MinuteUnit, Centrifugation_Time_MinuteUnit], 0);
        Expect_exists(existingNames_1, (x_14) => (x_14 === "Parameter [Centrifugation Time]"), "deep 0");
        Expect_exists(existingNames_1, (x_15) => (x_15 === "Parameter [Centrifugation Time] "), "deep 1");
        Expect_exists(existingNames_1, (x_16) => (x_16 === "Parameter [Centrifugation Time]  "), "deep 2");
        Expect_exists(existingNames_1, (x_17) => (x_17 === "Parameter [Centrifugation Time]   "), "deep 3");
        Expect_all(existingNames_1, (x_18) => (x_18 !== "Parameter [Centrifugation Time]    "), "deep 4");
    }))))))))));
})))))))))));

//# sourceMappingURL=OfficeInterop.Indexing.Tests.js.map
