namespace RaftCraft.IntegrationTests

open NUnit.Framework
open RaftCraft.IntegrationTests.Framework
open RaftCraft.RaftDomain

type Candidate() =

    let createFixture() = 
        let harness = new RaftTestHarness(3, (fun _ -> 50), (fun _ -> 1000), (fun _ -> 200))
        harness.Initialize().Start()

    [<Test>]
    member __.``Initial state is correct``() = 
        let fixture = createFixture()

        fixture.GetNode(1).AdvanceToElectionTimeout() |> Async.RunSynchronously

        let state1 = fixture.GetNode(1).State
        let state2 = fixture.GetNode(2).State
        let state3 = fixture.GetNode(3).State
        
        Assert.AreEqual(RaftRole.Leader, state1.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state2.RaftRole)
        Assert.AreEqual(RaftRole.Candidate, state3.RaftRole)