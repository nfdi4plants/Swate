import { Test_testCase, Test_testList } from "./fable_modules/Fable.Mocha.2.17.0/Mocha.fs.js";
import { PathRerooting_rerootPath } from "./src/Client/Pages/FilePicker/FilePickerView.js";
import { structuralHash, assertEqual } from "./fable_modules/fable-library.4.9.0/Util.js";
import { ofArray, contains } from "./fable_modules/fable-library.4.9.0/List.js";
import { equals, class_type, decimal_type, float64_type, bool_type, int32_type, string_type } from "./fable_modules/fable-library.4.9.0/Reflection.js";
import { printf, toText } from "./fable_modules/fable-library.4.9.0/String.js";

export const Paths_windows_findable = "c:\\Users\\Kevin\\source\\repos\\scripting-playground\\Swate\\assays\\assay1\\dataset\\my_datafile.txt";

export const Paths_windows_nonFindable = "c:\\Users\\Kevin\\source\\repos\\scripting-playground\\Swate\\assay1\\dataset\\my_datafile.txt";

export const Paths_windows_duplicateFindable = "c:\\Users\\Kevin\\source\\repos\\workflows\\scripting-playground\\Swate\\assays\\assay1\\dataset\\my_datafile.txt";

export const Paths_linux_findable = "/home/seth/documents/ExampleArc/workflows/my_super_workflow/penguin.jpg";

export const Paths_linux_nonFindable = "/home/seth/Pictures/penguin.jpg";

export const Paths_linux_duplicateFindable = "/home/seth/documents/assays/ExampleArc/workflows/my_super_workflow/penguin.jpg";

export const Paths_mac_findable = "~/Library/Containers/com.microsoft.Excel/Data/Documents/studies/The_STUDY/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml";

export const Paths_mac_nonFindable = "~/Library/Containers/com.microsoft.Excel/Data/Documents/wef/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml";

export const tests_FilePickerView_PathRerooting = Test_testList("FilePickerView_PathRerooting", ofArray([Test_testCase("Windows_find", () => {
    let copyOfStruct;
    const actual = PathRerooting_rerootPath(Paths_windows_findable);
    if ((actual === "assays/assay1/dataset/my_datafile.txt") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual, "assays/assay1/dataset/my_datafile.txt", "");
    }
    else {
        throw new Error(contains((copyOfStruct = actual, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("assays/assay1/dataset/my_datafile.txt")(actual)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("assays/assay1/dataset/my_datafile.txt")(actual)(""));
    }
}), Test_testCase("Windows_noFind", () => {
    let copyOfStruct_1;
    const actual_1 = PathRerooting_rerootPath(Paths_windows_nonFindable);
    if ((actual_1 === "my_datafile.txt") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_1, "my_datafile.txt", "");
    }
    else {
        throw new Error(contains((copyOfStruct_1 = actual_1, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("my_datafile.txt")(actual_1)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("my_datafile.txt")(actual_1)(""));
    }
}), Test_testCase("Windows_duplicateFind", () => {
    let copyOfStruct_2;
    const actual_2 = PathRerooting_rerootPath(Paths_windows_duplicateFindable);
    if ((actual_2 === "assays/assay1/dataset/my_datafile.txt") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_2, "assays/assay1/dataset/my_datafile.txt", "");
    }
    else {
        throw new Error(contains((copyOfStruct_2 = actual_2, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("assays/assay1/dataset/my_datafile.txt")(actual_2)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("assays/assay1/dataset/my_datafile.txt")(actual_2)(""));
    }
}), Test_testCase("Linux_find", () => {
    let copyOfStruct_3;
    const actual_3 = PathRerooting_rerootPath(Paths_linux_findable);
    if ((actual_3 === "workflows/my_super_workflow/penguin.jpg") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_3, "workflows/my_super_workflow/penguin.jpg", "");
    }
    else {
        throw new Error(contains((copyOfStruct_3 = actual_3, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("workflows/my_super_workflow/penguin.jpg")(actual_3)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("workflows/my_super_workflow/penguin.jpg")(actual_3)(""));
    }
}), Test_testCase("Linux_noFind", () => {
    let copyOfStruct_4;
    const actual_4 = PathRerooting_rerootPath(Paths_linux_nonFindable);
    if ((actual_4 === "penguin.jpg") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_4, "penguin.jpg", "");
    }
    else {
        throw new Error(contains((copyOfStruct_4 = actual_4, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("penguin.jpg")(actual_4)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("penguin.jpg")(actual_4)(""));
    }
}), Test_testCase("Linux_duplicateFind", () => {
    let copyOfStruct_5;
    const actual_5 = PathRerooting_rerootPath(Paths_linux_duplicateFindable);
    if ((actual_5 === "workflows/my_super_workflow/penguin.jpg") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_5, "workflows/my_super_workflow/penguin.jpg", "");
    }
    else {
        throw new Error(contains((copyOfStruct_5 = actual_5, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("workflows/my_super_workflow/penguin.jpg")(actual_5)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("workflows/my_super_workflow/penguin.jpg")(actual_5)(""));
    }
}), Test_testCase("Mac_find", () => {
    let copyOfStruct_6;
    const actual_6 = PathRerooting_rerootPath(Paths_mac_findable);
    if ((actual_6 === "studies/The_STUDY/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_6, "studies/The_STUDY/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml", "");
    }
    else {
        throw new Error(contains((copyOfStruct_6 = actual_6, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("studies/The_STUDY/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml")(actual_6)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("studies/The_STUDY/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml")(actual_6)(""));
    }
}), Test_testCase("Mac_noFind", () => {
    let copyOfStruct_7;
    const actual_7 = PathRerooting_rerootPath(Paths_mac_nonFindable);
    if ((actual_7 === "5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml") ? true : !(new Function("try {return this===window;}catch(e){ return false;}"))()) {
        assertEqual(actual_7, "5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml", "");
    }
    else {
        throw new Error(contains((copyOfStruct_7 = actual_7, string_type), ofArray([int32_type, bool_type, float64_type, string_type, decimal_type, class_type("System.Guid")]), {
            Equals: equals,
            GetHashCode: structuralHash,
        }) ? toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%s</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%s</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml")(actual_7)("") : toText(printf("<span style=\'color:black\'>Expected:</span> <br /><div style=\'margin-left:20px; color:crimson\'>%A</div><br /><span style=\'color:black\'>Actual:</span> </br ><div style=\'margin-left:20px;color:crimson\'>%A</div><br /><span style=\'color:black\'>Message:</span> </br ><div style=\'margin-left:20px; color:crimson\'>%s</div>"))("5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml")(actual_7)(""));
    }
})]));

//# sourceMappingURL=FilePickerView.Tests.js.map
