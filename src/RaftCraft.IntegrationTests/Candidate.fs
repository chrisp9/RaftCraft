namespace RaftCraft.IntegrationTests

open NUnit.Framework
open RaftCraft.IntegrationTests.Framework
open RaftCraft.RaftDomain
open RaftCraft.IntegrationTests.Common

type Candidate() =

    let createFixture() = 
        let harness = new RaftTestHarness(3, (fun _ -> 50), (fun _ -> 1000), (fun _ -> 200))
        harness.Initialize().Start()

    [<Test>]
    member __.``All nodes start as followers``() =
        use fixture = createFixture()

        let state1 = fixture.GetNode(1).State
        let state2 = fixture.GetNode(2).State
        let state3 = fixture.GetNode(3).State

        Assert.AreEqual(RaftRole.Follower, state1.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state2.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state3.RaftRole)
   
    [<Test>]
    member __.``Node starts election and becomes leader``() = 
        use fixture = createFixture()

        fixture.GetNode(1).AdvanceToElectionTimeout() |> Async.RunSynchronously

        Poller.Until(fun () -> fixture.GetNode(1).State.RaftRole = RaftRole.Leader)

        let state1 = fixture.GetNode(1).State
        let state2 = fixture.GetNode(2).State
        let state3 = fixture.GetNode(3).State
        
        Assert.AreEqual(RaftRole.Leader, state1.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state2.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state3.RaftRole)