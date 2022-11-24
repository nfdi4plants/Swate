module FilePickerView.Tests

open Fable.Mocha
open Client

open Shared.OfficeInteropTypes
open Shared.TermTypes

open FilePicker

module Paths =
    let windows_findable = """c:\Users\Kevin\source\repos\scripting-playground\Swate\assays\assay1\dataset\my_datafile.txt"""
    let windows_nonFindable = """c:\Users\Kevin\source\repos\scripting-playground\Swate\assay1\dataset\my_datafile.txt"""
    let windows_duplicateFindable = """c:\Users\Kevin\source\repos\workflows\scripting-playground\Swate\assays\assay1\dataset\my_datafile.txt"""

    let linux_findable = """/home/seth/documents/ExampleArc/workflows/my_super_workflow/penguin.jpg"""
    let linux_nonFindable = """/home/seth/Pictures/penguin.jpg"""
    let linux_duplicateFindable = """/home/seth/documents/assays/ExampleArc/workflows/my_super_workflow/penguin.jpg"""

    // not necessary, paths on mac behave as linux paths.
    let mac_findable = """~/Library/Containers/com.microsoft.Excel/Data/Documents/studies/The_STUDY/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml"""
    let mac_nonFindable = """~/Library/Containers/com.microsoft.Excel/Data/Documents/wef/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml"""
    //let mac_duplicateFindable = 

open PathRerooting

let tests_FilePickerView_PathRerooting = testList "FilePickerView_PathRerooting" [
    testCase "Windows_find" <| fun _ ->
        let relativePath = rerootPath Paths.windows_findable
        let expected = "assays/assay1/dataset/my_datafile.txt"
        Expect.equal relativePath expected ""
    // no supported directory in path
    testCase "Windows_noFind" <| fun _ ->
        let relativePath = rerootPath Paths.windows_nonFindable
        let expected = "my_datafile.txt"
        Expect.equal relativePath expected ""
    // two supported directories in path, should return last
    testCase "Windows_duplicateFind" <| fun _ ->
        let relativePath = rerootPath Paths.windows_duplicateFindable
        let expected = "assays/assay1/dataset/my_datafile.txt"
        Expect.equal relativePath expected ""

    testCase "Linux_find" <| fun _ ->
        let relativePath = rerootPath Paths.linux_findable
        let expected = "workflows/my_super_workflow/penguin.jpg"
        Expect.equal relativePath expected ""
    // no supported directory in path
    testCase "Linux_noFind" <| fun _ ->
        let relativePath = rerootPath Paths.linux_nonFindable
        let expected = "penguin.jpg"
        Expect.equal relativePath expected ""
    // two supported directories in path, should return last
    testCase "Linux_duplicateFind" <| fun _ ->
        let relativePath = rerootPath Paths.linux_duplicateFindable
        let expected = "workflows/my_super_workflow/penguin.jpg"
        Expect.equal relativePath expected ""

    testCase "Mac_find" <| fun _ ->
        let relativePath = rerootPath Paths.mac_findable
        let expected = "studies/The_STUDY/5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml"
        Expect.equal relativePath expected ""
    // no supported directory in path
    testCase "Mac_noFind" <| fun _ ->
        let relativePath = rerootPath Paths.mac_nonFindable
        let expected = "5d6f5462-3401-48ec-9406-d12882e9ad83.manifest.xml"
        Expect.equal relativePath expected ""
]