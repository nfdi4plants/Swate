import { SwateColumnHeader__get_isSingleCol, BuildingBlock__get_hasUnit, BuildingBlock__get_hasCompleteTerm, BuildingBlock__get_hasTerm, BuildingBlock__get_hasCompleteTSRTAN, BuildingBlock_$reflection, Column, Cell, SwateColumnHeader } from "./src/Shared/OfficeInteropTypes.js";
import { TermTypes_TermMinimal_$reflection, TermTypes_TermMinimal_create, TermTypes_TermMinimal } from "./src/Shared/TermTypes.js";
import { Expect_notEqual, Expect_isTrue, Test_testCase, Test_testList } from "./fable_modules/Fable.Mocha.2.17.0/Mocha.fs.js";
import { ofArray, contains, reverse, toArray } from "./fable_modules/fable-library.4.9.0/List.js";
import { Aux_GetBuildingBlocksPostSync_getMainColumnTerm, Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks } from "./src/Client/OfficeInterop/Functions/BuildingBlockFunctions.js";
import { append, equalsWith, map } from "./fable_modules/fable-library.4.9.0/Array.js";
import { equals as equals_1, int32ToString, structuralHash, assertEqual } from "./fable_modules/fable-library.4.9.0/Util.js";
import { option_type, array_type, equals, class_type, decimal_type, string_type, float64_type, bool_type, int32_type } from "./fable_modules/fable-library.4.9.0/Reflection.js";
import { printf, toText } from "./fable_modules/fable-library.4.9.0/String.js";
import { toString, seqToString } from "./fable_modules/fable-library.4.9.0/Types.js";
import { value } from "./fable_modules/fable-library.4.9.0/Option.js";

export const case_newTableColumns = [new Column(0, new SwateColumnHeader("Source Name"), [new Cell(1, "", void 0)]), new Column(1, new SwateColumnHeader("Sample Name"), [new Cell(1, "", void 0)])];

export const case_tableWithParameterAndValue = [new Column(0, new SwateColumnHeader("Source Name"), [new Cell(1, "test/source/path", void 0)]), new Column(1, new SwateColumnHeader("Parameter [instrument model]"), [new Cell(1, "SCIEX instrument model", void 0)]), new Column(2, new SwateColumnHeader("Term Source REF (MS:1000031)"), [new Cell(1, "MS", void 0)]), new Column(3, new SwateColumnHeader("Term Accession Number (MS:1000031)"), [new Cell(1, "http://purl.obolibrary.org/obo/MS_1000121", void 0)]), new Column(4, new SwateColumnHeader("Sample Name"), [new Cell(1, "test/sink/path", void 0)])];

export const case_tableWithParamsWithUnit = [new Column(5, new SwateColumnHeader("Factor [temperature]"), [new Cell(1, "30", new TermTypes_TermMinimal("degree Celsius", ""))]), new Column(6, new SwateColumnHeader("Unit"), [new Cell(1, "", void 0)]), new Column(7, new SwateColumnHeader("Term Source REF (PATO:0000146)"), [new Cell(1, "", void 0)]), new Column(8, new SwateColumnHeader("Term Accession Number (PATO:0000146)"), [new Cell(1, "", void 0)])];

export const case_tableWithFeaturedCol = [new Column(9, new SwateColumnHeader("Protocol Type"), [new Cell(1, "extract protocol", void 0)]), new Column(10, new SwateColumnHeader("Term Source REF (NFDI4PSO:1000161)"), [new Cell(1, "KF_PH", void 0)]), new Column(11, new SwateColumnHeader("Term Accession Number (NFDI4PSO:1000161)"), [new Cell(1, "http://purl.obolibrary.org/obo/KF_PH_002", void 0)])];

export const case_tableWithSingleCol = [new Column(12, new SwateColumnHeader("Protocol REF"), [new Cell(1, "My Fancy Protocol Name", void 0)])];

export const tests_buildingBlockFunctions = Test_testList("BuildingBlockFunctions", ofArray([Test_testCase("BuildingBlocks from case_newTableColumns", () => {
    let copyOfStruct, arg, arg_1, copyOfStruct_1, copyOfStruct_2, copyOfStruct_3, arg_8, arg_1_3;
    const buildingBlocks = toArray(reverse(Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks(case_newTableColumns)));
    const buildingBlock_withMainColumnTerms = map(Aux_GetBuildingBlocksPostSync_getMainColumnTerm, buildingBlocks);
    const actual = buildingBlocks.length | 0;
    if ((actual === 2) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual, 2, "Different number of BuildingBlocks expected.");
    }
    else {
        throw new Error(contains((copyOfStruct = actual, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg = int32ToString(2), (arg_1 = int32ToString(actual), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg)(arg_1)("Different number of BuildingBlocks expected.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(2)(actual)("Different number of BuildingBlocks expected."));
    }
    const actual_1 = buildingBlocks[0].MainColumn.Header.SwateColumnHeader;
    if ((actual_1 === "Source Name") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_1, "Source Name", "First Building Block must be \'Source Name\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_1 = actual_1, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Source Name")(actual_1)("First Building Block must be \'Source Name\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Source Name")(actual_1)("First Building Block must be \'Source Name\'."));
    }
    const actual_2 = buildingBlocks[1].MainColumn.Header.SwateColumnHeader;
    if ((actual_2 === "Sample Name") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_2, "Sample Name", "Second/last Building Block must be \'Sample Name\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_2 = actual_2, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Sample Name")(actual_2)("Second/last Building Block must be \'Sample Name\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Sample Name")(actual_2)("Second/last Building Block must be \'Sample Name\'."));
    }
    const actual_3 = buildingBlock_withMainColumnTerms;
    const expected_3 = buildingBlocks;
    if (equalsWith(equals_1, actual_3, expected_3) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_3, expected_3, "Sample Name and Source Name should not change when updating record type with main column terms.");
    }
    else {
        throw new Error(contains((copyOfStruct_3 = actual_3, array_type(BuildingBlock_$reflection())), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_8 = seqToString(expected_3), (arg_1_3 = seqToString(actual_3), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_8)(arg_1_3)("Sample Name and Source Name should not change when updating record type with main column terms.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_3)(actual_3)("Sample Name and Source Name should not change when updating record type with main column terms."));
    }
}), Test_testCase("BuildingBlocks from case_tableWithParameterAndValue", () => {
    let copyOfStruct_4, arg_9, arg_1_4, copyOfStruct_5, copyOfStruct_6, copyOfStruct_7, arg_12, arg_1_7, copyOfStruct_8, copyOfStruct_9, arg_14, arg_1_9, copyOfStruct_10, copyOfStruct_11, copyOfStruct_12, copyOfStruct_13, arg_18, arg_1_13;
    const buildingBlocks_1 = toArray(reverse(Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks(case_tableWithParameterAndValue)));
    const buildingBlock_withMainColumnTerms_1 = map(Aux_GetBuildingBlocksPostSync_getMainColumnTerm, buildingBlocks_1);
    const actual_4 = buildingBlocks_1.length | 0;
    if ((actual_4 === 3) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_4, 3, "Different number of BuildingBlocks expected.");
    }
    else {
        throw new Error(contains((copyOfStruct_4 = actual_4, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_9 = int32ToString(3), (arg_1_4 = int32ToString(actual_4), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_9)(arg_1_4)("Different number of BuildingBlocks expected.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(3)(actual_4)("Different number of BuildingBlocks expected."));
    }
    const actual_5 = buildingBlocks_1[0].MainColumn.Header.SwateColumnHeader;
    if ((actual_5 === "Source Name") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_5, "Source Name", "First Building Block must be \'Source Name\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_5 = actual_5, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Source Name")(actual_5)("First Building Block must be \'Source Name\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Source Name")(actual_5)("First Building Block must be \'Source Name\'."));
    }
    const actual_6 = value(buildingBlocks_1[0].MainColumn.Cells[0].Value);
    if ((actual_6 === "test/source/path") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_6, "test/source/path", "");
    }
    else {
        throw new Error(contains((copyOfStruct_6 = actual_6, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("test/source/path")(actual_6)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("test/source/path")(actual_6)(""));
    }
    const actual_7 = buildingBlocks_1[0];
    const expected_7 = buildingBlock_withMainColumnTerms_1[0];
    if (equals_1(actual_7, expected_7) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_7, expected_7, "Source Name MUST not change after update with main column term.");
    }
    else {
        throw new Error(contains((copyOfStruct_7 = actual_7, BuildingBlock_$reflection()), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_12 = toString(expected_7), (arg_1_7 = toString(actual_7), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_12)(arg_1_7)("Source Name MUST not change after update with main column term.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_7)(actual_7)("Source Name MUST not change after update with main column term."));
    }
    const actual_8 = buildingBlocks_1[1].MainColumn.Header.SwateColumnHeader;
    if ((actual_8 === "Parameter [instrument model]") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_8, "Parameter [instrument model]", "Second Building Block must be \'Parameter [instrument model]\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_8 = actual_8, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Parameter [instrument model]")(actual_8)("Second Building Block must be \'Parameter [instrument model]\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Parameter [instrument model]")(actual_8)("Second Building Block must be \'Parameter [instrument model]\'."));
    }
    Expect_isTrue(BuildingBlock__get_hasCompleteTSRTAN(buildingBlocks_1[1]))("\'Parameter [instrument model]\' must have complete TSR and TAN");
    Expect_notEqual(buildingBlocks_1[1], buildingBlock_withMainColumnTerms_1[1], "\'Parameter [instrument model]\' MUST change after update with main column term.");
    Expect_isTrue(BuildingBlock__get_hasTerm(buildingBlock_withMainColumnTerms_1[1]))("\'Parameter [instrument model]\' MUST have term after \'getMainColumnTerm\'.");
    Expect_isTrue(BuildingBlock__get_hasCompleteTerm(buildingBlock_withMainColumnTerms_1[1]))("\'Parameter [instrument model]\' MUST have complete term after \'getMainColumnTerm\'.");
    const actual_9 = buildingBlock_withMainColumnTerms_1[1].MainColumnTerm;
    const expected_9 = TermTypes_TermMinimal_create("instrument model", "MS:1000031");
    if (equals_1(actual_9, expected_9) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_9, expected_9, "MainColumnTerm for \'Parameter [instrument model]\' differs from expected value.");
    }
    else {
        throw new Error(contains((copyOfStruct_9 = actual_9, option_type(TermTypes_TermMinimal_$reflection())), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_14 = toString(expected_9), (arg_1_9 = toString(actual_9), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_14)(arg_1_9)("MainColumnTerm for \'Parameter [instrument model]\' differs from expected value.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_9)(actual_9)("MainColumnTerm for \'Parameter [instrument model]\' differs from expected value."));
    }
    const actual_10 = value(buildingBlock_withMainColumnTerms_1[1].MainColumn.Cells[0].Value);
    if ((actual_10 === "SCIEX instrument model") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_10, "SCIEX instrument model", "Value of \'Parameter [instrument model]\' differs from expected.");
    }
    else {
        throw new Error(contains((copyOfStruct_10 = actual_10, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("SCIEX instrument model")(actual_10)("Value of \'Parameter [instrument model]\' differs from expected.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("SCIEX instrument model")(actual_10)("Value of \'Parameter [instrument model]\' differs from expected."));
    }
    const actual_11 = buildingBlocks_1[2].MainColumn.Header.SwateColumnHeader;
    if ((actual_11 === "Sample Name") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_11, "Sample Name", "Last Building Block must be \'Sample Name\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_11 = actual_11, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Sample Name")(actual_11)("Last Building Block must be \'Sample Name\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Sample Name")(actual_11)("Last Building Block must be \'Sample Name\'."));
    }
    const actual_12 = value(buildingBlocks_1[2].MainColumn.Cells[0].Value);
    if ((actual_12 === "test/sink/path") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_12, "test/sink/path", "");
    }
    else {
        throw new Error(contains((copyOfStruct_12 = actual_12, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("test/sink/path")(actual_12)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("test/sink/path")(actual_12)(""));
    }
    const actual_13 = buildingBlocks_1[2];
    const expected_13 = buildingBlock_withMainColumnTerms_1[2];
    if (equals_1(actual_13, expected_13) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_13, expected_13, "Sample Name MUST not change after update with main column term.");
    }
    else {
        throw new Error(contains((copyOfStruct_13 = actual_13, BuildingBlock_$reflection()), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_18 = toString(expected_13), (arg_1_13 = toString(actual_13), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_18)(arg_1_13)("Sample Name MUST not change after update with main column term.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_13)(actual_13)("Sample Name MUST not change after update with main column term."));
    }
}), Test_testCase("BuildingBlocks from case_tableWithParamsWithUnit", () => {
    let copyOfStruct_14, arg_19, arg_1_14, copyOfStruct_15, copyOfStruct_16, copyOfStruct_17, arg_22, arg_1_17, copyOfStruct_18, arg_23, arg_1_18;
    const buildingBlocks_2 = toArray(reverse(Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks(case_tableWithParamsWithUnit)));
    const buildingBlock_withMainColumnTerms_2 = map(Aux_GetBuildingBlocksPostSync_getMainColumnTerm, buildingBlocks_2);
    const actual_14 = buildingBlocks_2.length | 0;
    if ((actual_14 === 1) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_14, 1, "Different number of BuildingBlocks expected.");
    }
    else {
        throw new Error(contains((copyOfStruct_14 = actual_14, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_19 = int32ToString(1), (arg_1_14 = int32ToString(actual_14), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_19)(arg_1_14)("Different number of BuildingBlocks expected.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(1)(actual_14)("Different number of BuildingBlocks expected."));
    }
    const actual_15 = buildingBlocks_2[0].MainColumn.Header.SwateColumnHeader;
    if ((actual_15 === "Factor [temperature]") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_15, "Factor [temperature]", "Building Block must be \'Factor [temperature]\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_15 = actual_15, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Factor [temperature]")(actual_15)("Building Block must be \'Factor [temperature]\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Factor [temperature]")(actual_15)("Building Block must be \'Factor [temperature]\'."));
    }
    Expect_isTrue(BuildingBlock__get_hasCompleteTSRTAN(buildingBlocks_2[0]))("Factor [temperature] hasCompleteTSRTAN");
    Expect_isTrue(BuildingBlock__get_hasUnit(buildingBlocks_2[0]))("Factor [temperature] hasUnit");
    const actual_16 = value(buildingBlocks_2[0].MainColumn.Cells[0].Value);
    if ((actual_16 === "30") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_16, "30", "Factor [temperature] MainColumn.Cells.[0].Value.Value");
    }
    else {
        throw new Error(contains((copyOfStruct_16 = actual_16, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("30")(actual_16)("Factor [temperature] MainColumn.Cells.[0].Value.Value") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("30")(actual_16)("Factor [temperature] MainColumn.Cells.[0].Value.Value"));
    }
    Expect_isTrue(buildingBlocks_2[0].MainColumn.Cells[0].Unit != null)("Factor [temperature] MainColumn.Cells.[0].Unit.IsSome");
    const actual_17 = value(buildingBlocks_2[0].MainColumn.Cells[0].Unit);
    const expected_17 = new TermTypes_TermMinimal("degree Celsius", "");
    if (equals_1(actual_17, expected_17) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_17, expected_17, "Factor [temperature] MainColumn.Cells.[0].Unit.Value");
    }
    else {
        throw new Error(contains((copyOfStruct_17 = actual_17, TermTypes_TermMinimal_$reflection()), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_22 = toString(expected_17), (arg_1_17 = toString(actual_17), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_22)(arg_1_17)("Factor [temperature] MainColumn.Cells.[0].Unit.Value")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_17)(actual_17)("Factor [temperature] MainColumn.Cells.[0].Unit.Value"));
    }
    Expect_notEqual(buildingBlocks_2[0], buildingBlock_withMainColumnTerms_2[0], "\'Factor [temperature]\' MUST change after update with main column term.");
    Expect_isTrue(BuildingBlock__get_hasCompleteTerm(buildingBlock_withMainColumnTerms_2[0]))("Factor [temperature] hasCompleteTerm");
    const actual_18 = buildingBlock_withMainColumnTerms_2[0].MainColumnTerm;
    const expected_18 = TermTypes_TermMinimal_create("temperature", "PATO:0000146");
    if (equals_1(actual_18, expected_18) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_18, expected_18, "MainColumnTerm for \'Factor [temperature]\' differs from expected value.");
    }
    else {
        throw new Error(contains((copyOfStruct_18 = actual_18, option_type(TermTypes_TermMinimal_$reflection())), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_23 = toString(expected_18), (arg_1_18 = toString(actual_18), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_23)(arg_1_18)("MainColumnTerm for \'Factor [temperature]\' differs from expected value.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_18)(actual_18)("MainColumnTerm for \'Factor [temperature]\' differs from expected value."));
    }
}), Test_testCase("BuildingBlocks from case_tableWithFeaturedCol", () => {
    let copyOfStruct_19, arg_24, arg_1_19, copyOfStruct_20, copyOfStruct_21, copyOfStruct_22, arg_27, arg_1_22;
    const buildingBlocks_3 = toArray(reverse(Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks(case_tableWithFeaturedCol)));
    const buildingBlock_withMainColumnTerms_3 = map(Aux_GetBuildingBlocksPostSync_getMainColumnTerm, buildingBlocks_3);
    const actual_19 = buildingBlocks_3.length | 0;
    if ((actual_19 === 1) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_19, 1, "Different number of BuildingBlocks expected.");
    }
    else {
        throw new Error(contains((copyOfStruct_19 = actual_19, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_24 = int32ToString(1), (arg_1_19 = int32ToString(actual_19), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_24)(arg_1_19)("Different number of BuildingBlocks expected.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(1)(actual_19)("Different number of BuildingBlocks expected."));
    }
    const actual_20 = buildingBlocks_3[0].MainColumn.Header.SwateColumnHeader;
    if ((actual_20 === "Protocol Type") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_20, "Protocol Type", "Building Block must be \'Protocol Type\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_20 = actual_20, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Protocol Type")(actual_20)("Building Block must be \'Protocol Type\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Protocol Type")(actual_20)("Building Block must be \'Protocol Type\'."));
    }
    Expect_isTrue(BuildingBlock__get_hasCompleteTSRTAN(buildingBlocks_3[0]))("Protocol Type hasCompleteTSRTAN");
    const actual_21 = value(buildingBlocks_3[0].MainColumn.Cells[0].Value);
    if ((actual_21 === "extract protocol") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_21, "extract protocol", "Protocol Type MainColumn.Cells.[0].Value.Value");
    }
    else {
        throw new Error(contains((copyOfStruct_21 = actual_21, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("extract protocol")(actual_21)("Protocol Type MainColumn.Cells.[0].Value.Value") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("extract protocol")(actual_21)("Protocol Type MainColumn.Cells.[0].Value.Value"));
    }
    Expect_isTrue(buildingBlocks_3[0].MainColumn.Cells[0].Unit == null)("Protocol Type MainColumn.Cells.[0].Unit.IsNone");
    Expect_notEqual(buildingBlocks_3[0], buildingBlock_withMainColumnTerms_3[0], "Protocol Type MUST change after update with main column term.");
    Expect_isTrue(BuildingBlock__get_hasCompleteTerm(buildingBlock_withMainColumnTerms_3[0]))("Protocol Type hasCompleteTerm");
    const actual_22 = buildingBlock_withMainColumnTerms_3[0].MainColumnTerm;
    const expected_22 = TermTypes_TermMinimal_create("protocol type", "NFDI4PSO:1000161");
    if (equals_1(actual_22, expected_22) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_22, expected_22, "MainColumnTerm for \'Protocol Type\' differs from expected value");
    }
    else {
        throw new Error(contains((copyOfStruct_22 = actual_22, option_type(TermTypes_TermMinimal_$reflection())), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_27 = toString(expected_22), (arg_1_22 = toString(actual_22), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_27)(arg_1_22)("MainColumnTerm for \'Protocol Type\' differs from expected value")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_22)(actual_22)("MainColumnTerm for \'Protocol Type\' differs from expected value"));
    }
}), Test_testCase("BuildingBlocks from case_tableWithSingleCol", () => {
    let copyOfStruct_23, arg_28, arg_1_23, copyOfStruct_24, copyOfStruct_25, arg_30, arg_1_25;
    const buildingBlocks_4 = toArray(reverse(Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks(case_tableWithSingleCol)));
    const buildingBlock_withMainColumnTerms_4 = map(Aux_GetBuildingBlocksPostSync_getMainColumnTerm, buildingBlocks_4);
    const actual_23 = buildingBlocks_4.length | 0;
    if ((actual_23 === 1) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_23, 1, "Different number of BuildingBlocks expected.");
    }
    else {
        throw new Error(contains((copyOfStruct_23 = actual_23, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_28 = int32ToString(1), (arg_1_23 = int32ToString(actual_23), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_28)(arg_1_23)("Different number of BuildingBlocks expected.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(1)(actual_23)("Different number of BuildingBlocks expected."));
    }
    const actual_24 = buildingBlocks_4[0].MainColumn.Header.SwateColumnHeader;
    if ((actual_24 === "Protocol REF") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_24, "Protocol REF", "First Building Block must be \'Protocol REF\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_24 = actual_24, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Protocol REF")(actual_24)("First Building Block must be \'Protocol REF\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Protocol REF")(actual_24)("First Building Block must be \'Protocol REF\'."));
    }
    Expect_isTrue(SwateColumnHeader__get_isSingleCol(buildingBlocks_4[0].MainColumn.Header))("First Building Block must be \'isSingleCol\'.");
    const actual_25 = buildingBlock_withMainColumnTerms_4;
    const expected_25 = buildingBlocks_4;
    if (equalsWith(equals_1, actual_25, expected_25) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_25, expected_25, "Protocol REF should not change when updating record type with main column terms.");
    }
    else {
        throw new Error(contains((copyOfStruct_25 = actual_25, array_type(BuildingBlock_$reflection())), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_30 = seqToString(expected_25), (arg_1_25 = seqToString(actual_25), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_30)(arg_1_25)("Protocol REF should not change when updating record type with main column terms.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_25)(actual_25)("Protocol REF should not change when updating record type with main column terms."));
    }
}), Test_testCase("BuildingBlocks from combined cases", () => {
    let copyOfStruct_26, arg_31, arg_1_26, copyOfStruct_27, copyOfStruct_28, copyOfStruct_29, arg_34, arg_1_29, copyOfStruct_30, copyOfStruct_31, arg_36, arg_1_31, copyOfStruct_32, copyOfStruct_33, copyOfStruct_34, copyOfStruct_35, arg_40, arg_1_35, copyOfStruct_36, copyOfStruct_37, copyOfStruct_38, arg_43, arg_1_38, copyOfStruct_39, arg_44, arg_1_39, copyOfStruct_40, copyOfStruct_41, copyOfStruct_42, arg_47, arg_1_42, copyOfStruct_43;
    const buildingBlocks_5 = toArray(reverse(Aux_GetBuildingBlocksPostSync_sortColsIntoBuildingBlocks(append(case_tableWithParameterAndValue, append(case_tableWithParamsWithUnit, append(case_tableWithFeaturedCol, case_tableWithSingleCol))))));
    const buildingBlock_withMainColumnTerms_5 = map(Aux_GetBuildingBlocksPostSync_getMainColumnTerm, buildingBlocks_5);
    const actual_26 = buildingBlocks_5.length | 0;
    if ((actual_26 === 6) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_26, 6, "Different number of BuildingBlocks expected.");
    }
    else {
        throw new Error(contains((copyOfStruct_26 = actual_26, int32_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_31 = int32ToString(6), (arg_1_26 = int32ToString(actual_26), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_31)(arg_1_26)("Different number of BuildingBlocks expected.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(6)(actual_26)("Different number of BuildingBlocks expected."));
    }
    const actual_27 = buildingBlocks_5[0].MainColumn.Header.SwateColumnHeader;
    if ((actual_27 === "Source Name") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_27, "Source Name", "First Building Block must be \'Source Name\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_27 = actual_27, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Source Name")(actual_27)("First Building Block must be \'Source Name\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Source Name")(actual_27)("First Building Block must be \'Source Name\'."));
    }
    const actual_28 = value(buildingBlocks_5[0].MainColumn.Cells[0].Value);
    if ((actual_28 === "test/source/path") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_28, "test/source/path", "");
    }
    else {
        throw new Error(contains((copyOfStruct_28 = actual_28, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("test/source/path")(actual_28)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("test/source/path")(actual_28)(""));
    }
    const actual_29 = buildingBlocks_5[0];
    const expected_29 = buildingBlock_withMainColumnTerms_5[0];
    if (equals_1(actual_29, expected_29) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_29, expected_29, "Source Name MUST not change after update with main column term.");
    }
    else {
        throw new Error(contains((copyOfStruct_29 = actual_29, BuildingBlock_$reflection()), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_34 = toString(expected_29), (arg_1_29 = toString(actual_29), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_34)(arg_1_29)("Source Name MUST not change after update with main column term.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_29)(actual_29)("Source Name MUST not change after update with main column term."));
    }
    const actual_30 = buildingBlocks_5[1].MainColumn.Header.SwateColumnHeader;
    if ((actual_30 === "Parameter [instrument model]") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_30, "Parameter [instrument model]", "Second Building Block must be \'Parameter [instrument model]\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_30 = actual_30, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Parameter [instrument model]")(actual_30)("Second Building Block must be \'Parameter [instrument model]\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Parameter [instrument model]")(actual_30)("Second Building Block must be \'Parameter [instrument model]\'."));
    }
    Expect_isTrue(BuildingBlock__get_hasCompleteTSRTAN(buildingBlocks_5[1]))("\'Parameter [instrument model]\' must have complete TSR and TAN");
    Expect_notEqual(buildingBlocks_5[1], buildingBlock_withMainColumnTerms_5[1], "\'Parameter [instrument model]\' MUST change after update with main column term.");
    Expect_isTrue(BuildingBlock__get_hasTerm(buildingBlock_withMainColumnTerms_5[1]))("\'Parameter [instrument model]\' MUST have term after \'getMainColumnTerm\'.");
    Expect_isTrue(BuildingBlock__get_hasCompleteTerm(buildingBlock_withMainColumnTerms_5[1]))("\'Parameter [instrument model]\' MUST have complete term after \'getMainColumnTerm\'.");
    const actual_31 = buildingBlock_withMainColumnTerms_5[1].MainColumnTerm;
    const expected_31 = TermTypes_TermMinimal_create("instrument model", "MS:1000031");
    if (equals_1(actual_31, expected_31) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_31, expected_31, "MainColumnTerm for \'Parameter [instrument model]\' differs from expected value.");
    }
    else {
        throw new Error(contains((copyOfStruct_31 = actual_31, option_type(TermTypes_TermMinimal_$reflection())), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_36 = toString(expected_31), (arg_1_31 = toString(actual_31), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_36)(arg_1_31)("MainColumnTerm for \'Parameter [instrument model]\' differs from expected value.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_31)(actual_31)("MainColumnTerm for \'Parameter [instrument model]\' differs from expected value."));
    }
    const actual_32 = value(buildingBlock_withMainColumnTerms_5[1].MainColumn.Cells[0].Value);
    if ((actual_32 === "SCIEX instrument model") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_32, "SCIEX instrument model", "Value of \'Parameter [instrument model]\' differs from expected.");
    }
    else {
        throw new Error(contains((copyOfStruct_32 = actual_32, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("SCIEX instrument model")(actual_32)("Value of \'Parameter [instrument model]\' differs from expected.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("SCIEX instrument model")(actual_32)("Value of \'Parameter [instrument model]\' differs from expected."));
    }
    const actual_33 = buildingBlocks_5[2].MainColumn.Header.SwateColumnHeader;
    if ((actual_33 === "Sample Name") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_33, "Sample Name", "Last Building Block must be \'Sample Name\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_33 = actual_33, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Sample Name")(actual_33)("Last Building Block must be \'Sample Name\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Sample Name")(actual_33)("Last Building Block must be \'Sample Name\'."));
    }
    const actual_34 = value(buildingBlocks_5[2].MainColumn.Cells[0].Value);
    if ((actual_34 === "test/sink/path") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_34, "test/sink/path", "");
    }
    else {
        throw new Error(contains((copyOfStruct_34 = actual_34, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("test/sink/path")(actual_34)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("test/sink/path")(actual_34)(""));
    }
    const actual_35 = buildingBlocks_5[2];
    const expected_35 = buildingBlock_withMainColumnTerms_5[2];
    if (equals_1(actual_35, expected_35) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_35, expected_35, "Sample Name MUST not change after update with main column term.");
    }
    else {
        throw new Error(contains((copyOfStruct_35 = actual_35, BuildingBlock_$reflection()), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_40 = toString(expected_35), (arg_1_35 = toString(actual_35), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_40)(arg_1_35)("Sample Name MUST not change after update with main column term.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_35)(actual_35)("Sample Name MUST not change after update with main column term."));
    }
    const actual_36 = buildingBlocks_5[3].MainColumn.Header.SwateColumnHeader;
    if ((actual_36 === "Factor [temperature]") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_36, "Factor [temperature]", "Building Block must be \'Factor [temperature]\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_36 = actual_36, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Factor [temperature]")(actual_36)("Building Block must be \'Factor [temperature]\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Factor [temperature]")(actual_36)("Building Block must be \'Factor [temperature]\'."));
    }
    Expect_isTrue(BuildingBlock__get_hasCompleteTSRTAN(buildingBlocks_5[3]))("Factor [temperature] hasCompleteTSRTAN");
    Expect_isTrue(BuildingBlock__get_hasUnit(buildingBlocks_5[3]))("Factor [temperature] hasUnit");
    const actual_37 = value(buildingBlocks_5[3].MainColumn.Cells[0].Value);
    if ((actual_37 === "30") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_37, "30", "Factor [temperature] MainColumn.Cells.[0].Value.Value");
    }
    else {
        throw new Error(contains((copyOfStruct_37 = actual_37, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("30")(actual_37)("Factor [temperature] MainColumn.Cells.[0].Value.Value") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("30")(actual_37)("Factor [temperature] MainColumn.Cells.[0].Value.Value"));
    }
    Expect_isTrue(buildingBlocks_5[3].MainColumn.Cells[0].Unit != null)("Factor [temperature] MainColumn.Cells.[0].Unit.IsSome");
    const actual_38 = value(buildingBlocks_5[3].MainColumn.Cells[0].Unit);
    const expected_38 = new TermTypes_TermMinimal("degree Celsius", "");
    if (equals_1(actual_38, expected_38) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_38, expected_38, "Factor [temperature] MainColumn.Cells.[0].Unit.Value");
    }
    else {
        throw new Error(contains((copyOfStruct_38 = actual_38, TermTypes_TermMinimal_$reflection()), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_43 = toString(expected_38), (arg_1_38 = toString(actual_38), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_43)(arg_1_38)("Factor [temperature] MainColumn.Cells.[0].Unit.Value")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_38)(actual_38)("Factor [temperature] MainColumn.Cells.[0].Unit.Value"));
    }
    Expect_notEqual(buildingBlocks_5[3], buildingBlock_withMainColumnTerms_5[3], "\'Factor [temperature]\' MUST change after update with main column term.");
    Expect_isTrue(BuildingBlock__get_hasCompleteTerm(buildingBlock_withMainColumnTerms_5[3]))("Factor [temperature] hasCompleteTerm");
    const actual_39 = buildingBlock_withMainColumnTerms_5[3].MainColumnTerm;
    const expected_39 = TermTypes_TermMinimal_create("temperature", "PATO:0000146");
    if (equals_1(actual_39, expected_39) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_39, expected_39, "MainColumnTerm for \'Factor [temperature]\' differs from expected value.");
    }
    else {
        throw new Error(contains((copyOfStruct_39 = actual_39, option_type(TermTypes_TermMinimal_$reflection())), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_44 = toString(expected_39), (arg_1_39 = toString(actual_39), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_44)(arg_1_39)("MainColumnTerm for \'Factor [temperature]\' differs from expected value.")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_39)(actual_39)("MainColumnTerm for \'Factor [temperature]\' differs from expected value."));
    }
    const actual_40 = buildingBlocks_5[4].MainColumn.Header.SwateColumnHeader;
    if ((actual_40 === "Protocol Type") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_40, "Protocol Type", "Building Block must be \'Protocol Type\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_40 = actual_40, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Protocol Type")(actual_40)("Building Block must be \'Protocol Type\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Protocol Type")(actual_40)("Building Block must be \'Protocol Type\'."));
    }
    Expect_isTrue(BuildingBlock__get_hasCompleteTSRTAN(buildingBlocks_5[4]))("Protocol Type hasCompleteTSRTAN");
    const actual_41 = value(buildingBlocks_5[4].MainColumn.Cells[0].Value);
    if ((actual_41 === "extract protocol") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_41, "extract protocol", "Protocol Type MainColumn.Cells.[0].Value.Value");
    }
    else {
        throw new Error(contains((copyOfStruct_41 = actual_41, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("extract protocol")(actual_41)("Protocol Type MainColumn.Cells.[0].Value.Value") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("extract protocol")(actual_41)("Protocol Type MainColumn.Cells.[0].Value.Value"));
    }
    Expect_isTrue(buildingBlocks_5[4].MainColumn.Cells[0].Unit == null)("Protocol Type MainColumn.Cells.[0].Unit.IsNone");
    Expect_notEqual(buildingBlocks_5[4], buildingBlock_withMainColumnTerms_5[4], "Protocol Type MUST change after update with main column term.");
    Expect_isTrue(BuildingBlock__get_hasCompleteTerm(buildingBlock_withMainColumnTerms_5[4]))("Protocol Type hasCompleteTerm");
    const actual_42 = buildingBlock_withMainColumnTerms_5[4].MainColumnTerm;
    const expected_42 = TermTypes_TermMinimal_create("protocol type", "NFDI4PSO:1000161");
    if (equals_1(actual_42, expected_42) ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_42, expected_42, "MainColumnTerm for \'Protocol Type\' differs from expected value");
    }
    else {
        throw new Error(contains((copyOfStruct_42 = actual_42, option_type(TermTypes_TermMinimal_$reflection())), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? ((arg_47 = toString(expected_42), (arg_1_42 = toString(actual_42), toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(arg_47)(arg_1_42)("MainColumnTerm for \'Protocol Type\' differs from expected value")))) : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))(expected_42)(actual_42)("MainColumnTerm for \'Protocol Type\' differs from expected value"));
    }
    const actual_43 = buildingBlocks_5[5].MainColumn.Header.SwateColumnHeader;
    if ((actual_43 === "Protocol REF") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_43, "Protocol REF", "First Building Block must be \'Protocol REF\'.");
    }
    else {
        throw new Error(contains((copyOfStruct_43 = actual_43, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Protocol REF")(actual_43)("First Building Block must be \'Protocol REF\'.") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Protocol REF")(actual_43)("First Building Block must be \'Protocol REF\'."));
    }
    Expect_isTrue(SwateColumnHeader__get_isSingleCol(buildingBlocks_5[5].MainColumn.Header))("First Building Block must be \'isSingleCol\'.");
})]));

//# sourceMappingURL=BuildingBlockFunctions.Tests.js.map
