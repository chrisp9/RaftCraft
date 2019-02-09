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

        fixture.GetNode(1).AdvanceToCandidate() |> Async.RunSynchronously

        Poller.Until(fun () -> fixture.GetNode(1).State.RaftRole = RaftRole.Leader)

        let state1 = fixture.GetNode(1).State
        let state2 = fixture.GetNode(2).State
        let state3 = fixture.GetNode(3).State
        
        Assert.AreEqual(RaftRole.Leader, state1.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state2.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state3.RaftRole)

    [<Test>]
    member __.``Node can become leader when it receives majority vote``() =
        use fixture = createFixture()

        fixture.KillCommunicationWith(3)

        // Even though other nodes can't communicate with node 3, we still expect
        // that node 1 can become leader because it receives votes from a majority
        fixture.GetNode(1).AdvanceToCandidate() |> Async.RunSynchronously

        Poller.Until(fun () -> fixture.GetNode(1).State.RaftRole = RaftRole.Leader)

        let state1 = fixture.GetNode(1).State
        let state2 = fixture.GetNode(2).State
        let state3 = fixture.GetNode(3).State
        
        Assert.AreEqual(RaftRole.Leader, state1.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state2.RaftRole)
        Assert.AreEqual(RaftRole.Follower, state3.RaftRole)

    [<Test>]
    member __.``Node becomes leader after communication failure``() =
        use fixture = createFixture()

        // All nodes are initially blind to each other
        fixture.KillCommunicationWith(1)
        fixture.KillCommunicationWith(2)
        fixture.KillCommunicationWith(3)

        // Node 1 runs an election 
        fixture.GetNode(1).AdvanceToCandidate() |> Async.RunSynchronously
        fixture.GetNode(2).AdvanceToCandidate() |> Async.RunSynchronously
        fixture.GetNode(2).AdvanceToCandidate() |> Async.RunSynchronously

        fixture.RestoreCommunicationWith(1)
        fixture.RestoreCommunicationWith(2)
        fixture.RestoreCommunicationWith(3)

        let highestTerm = fixture.ExpectTerm(3)
        Assert.AreEqual(RaftRole.Leader, highestTerm.State.RaftRole)