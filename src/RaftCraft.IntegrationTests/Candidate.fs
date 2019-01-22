namespace RaftCraft.IntegrationTests

open NUnit.Framework
open RaftCraft.IntegrationTests.Framework

type Candidate() =

    let createFixture() = 
        let harness = new RaftTestHarness(3, (fun _ -> 50), (fun _ -> 1000), (fun _ -> 200))
        harness.Initialize()

    [<Test>]
    member __.``Initial state is correct``() = 
        let fixture = createFixture()
        fixture.GetNode(1).GlobalTimer.Tick()

        fixture.Tick