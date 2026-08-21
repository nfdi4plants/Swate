module Swate.Tests.Cwl.Main

open Expecto

[<EntryPoint>]
let main args =
    let allTests =
        testList "CWL" [
            Swate.Tests.Cwl.RoundtripTests.allTests
            Swate.Tests.Cwl.ValidationEngineTests.allTests
            Swate.Tests.Cwl.StateReducerTests.allTests
            Swate.Tests.Cwl.EffectRunnerTests.allTests
            Swate.Tests.Cwl.ArCtrlAdapterTests.allTests
            Swate.Tests.Cwl.EditorControllerLogicTests.allTests
        ]

    runTestsWithCLIArgs [] args allTests
