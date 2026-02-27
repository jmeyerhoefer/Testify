module Tests


open Implementation
open Microsoft.VisualStudio.TestTools.UnitTesting
open FsCheck
open Swensen.Unquote



[<TestClass>]
type Tests () =
    [<TestMethod>]
    member _.``replicatePeano Example`` (): unit =
        test <@ replicatePeano 0N 3N = [0N; 0N; 0N] @>
        test <@ replicatePeano 0N 3N = [] @>
        test <@ replicatePeano "Hallo" 2N = ["Hallo"; "Hallo"] @>

    [<TestMethod>]
    member _.``replicatePeano Random`` (): unit =
        Check.One(
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (k: Nat, n: Nat) -> Assert.AreEqual (replicatePeano k n, Solution.replicate k n)
        )

    [<TestMethod>]
    member _.``replicateLeibniz Example`` (): unit =
        test <@ replicateLeibniz 0N 3N = [0N; 0N; 0N] @>
        test <@ replicateLeibniz 0N 3N = [] @>
        test <@ replicateLeibniz "Hallo" 2N = ["Hallo"; "Hallo"] @>

    [<TestMethod>]
    member _.``replicateLeibniz Random`` (): unit =
        Check.One(
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (k: Nat, n: Nat) -> Assert.AreEqual (replicateLeibniz k n, Solution.replicate k n)
        )

    [<TestMethod>]
    member _.``exists Example`` (): unit =
        test <@ exists (fun x -> x = 9N) 10N 20N = false @>
        test <@ exists (fun x -> x = 10N) 10N 20N = true @>
        test <@ exists (fun x -> x = 15N) 10N 20N = true @>
        test <@ exists (fun x -> x = 20N) 10N 20N = true @>
        test <@ exists (fun x -> x = 21N) 10N 20N = false @>
        test <@ exists (fun x -> true) 20N 10N = false @>
        test <@ exists (fun x -> x = 42N) 42N 42N = true @>

    [<TestMethod>]
    member _.``exists Random`` (): unit =
        Check.One(
            { Config.QuickThrowOnFailure with MaxTest = 1000 },
            fun (n: Nat, lower: Nat, upper: Nat) ->
                let f (x: Nat): bool = x < n
                Assert.AreEqual<bool> (exists f lower upper, Solution.exists f lower upper)
        )