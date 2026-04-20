namespace Testify.Expecto.Sample

open Expecto
open Testify
open Testify.Expecto


module Dates =
    let isLeapYear (_year: int) : bool =
        true


module SampleTests =
    [<Tests>]
    let tests =
        TestifyExpecto.testList "samples" [
            TestifyExpecto.testCase "Dates.isLeapYear 1900" (fun () ->
                <@ Dates.isLeapYear 1900 @>
                |> Assert.should (AssertExpectation.equalTo false))
        ]


module Program =
    [<EntryPoint>]
    let main args =
        TestifyExpecto.runTestsWithCLIArgs
            TestifyExpectoConfig.defaults
            args
            SampleTests.tests

