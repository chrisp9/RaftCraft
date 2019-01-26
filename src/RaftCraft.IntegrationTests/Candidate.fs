namespace RaftCraft.IntegrationTests

open NUnit.Framework
open RaftCraft.IntegrationTests.Framework

type Candidate() =

    let createFixture() = 
        let harness = new RaftTestHarness(3, (fun _ -> 50), (fun _ -> 1000), (fun _ -> 200))
        harness.Initialize().Start()

    [<Test>]
    member __.``Initial state is correct``() = 
        let fixture = createFixture()

        System.Threading.Thread.Sleep(2000)
        fixture.GetNode(1).AdvanceToElectionTimeout() |> Async.RunSynchronously

        System.Threading.Thread.Sleep(5000)

        let state1 = fixture.GetNode(1).State
        let state2 = fixture.GetNode(2).State
        let state3 = fixture.GetNode(3).State

        ()