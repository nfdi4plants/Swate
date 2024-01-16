import { Mocha_runTests, Test_testCase, Test_testList } from "./fable_modules/Fable.Mocha.2.17.0/Mocha.fs.js";
import { sayHello } from "./src/Client/Client.js";
import { structuralHash, assertEqual } from "./fable_modules/fable-library.4.9.0/Util.js";
import { singleton, ofArray, contains } from "./fable_modules/fable-library.4.9.0/List.js";
import { equals, class_type, decimal_type, float64_type, bool_type, int32_type, string_type } from "./fable_modules/fable-library.4.9.0/Reflection.js";
import { printf, toText } from "./fable_modules/fable-library.4.9.0/String.js";
import { shared } from "./Shared/Shared.Tests.js";
import { tests_buildingBlockFunctions } from "./BuildingBlockFunctions.Tests.js";
import { tests_FilePickerView_PathRerooting } from "./FilePickerView.Tests.js";
import { tests_OfficeInterop_Indexing } from "./OfficeInterop.Indexing.Tests.js";

export const client = Test_testList("Client", singleton(Test_testCase("Hello received", () => {
    let copyOfStruct;
    const actual = sayHello("SAFE V3");
    if ((actual === "Hello SAFE V3") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual, "Hello SAFE V3", "Unexpected greeting");
    }
    else {
        throw new Error(contains((copyOfStruct = actual, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Hello SAFE V3")(actual)("Unexpected greeting") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("Hello SAFE V3")(actual)("Unexpected greeting"));
    }
})));

export const all = Test_testList("All", ofArray([shared, tests_buildingBlockFunctions, tests_FilePickerView_PathRerooting, tests_OfficeInterop_Indexing, client]));

(function (_arg) {
    return Mocha_runTests(all);
})(typeof process === 'object' ? process.argv.slice(2) : []);

//# sourceMappingURL=Client.Tests.js.map
