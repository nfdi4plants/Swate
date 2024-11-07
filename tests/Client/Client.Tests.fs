module Client.Tests

open Fable.Mocha

let client = testList "Client" [
    testCase "Hello received" <| fun _ ->
        let hello = Index.sayHello "SAFE V3"

        Expect.equal hello "Hello SAFE V3" "Unexpected greeting"
    testCase "develop mock" <| fun _ ->
        let mockData =
            {|
                workbook = {|
                    range = {|
                    address = "C2:G3"
                    |}
                |}
            |};
        let mock = OfficeAddinMock.OfficeAddinMock.OfficeMockObject(mockData)
        log mock
        //let result = mock.getMockData()
        Expect.equal 1 1 "testing"
]

let all =
    testList "All"
        [
#if FABLE_COMPILER // This preprocessor directive makes editor happy
            Shared.Tests.shared
#endif
            client
        ]

[<EntryPoint>]
let main _ = Mocha.runTests all