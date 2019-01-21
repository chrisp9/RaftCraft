namespace RaftCraft.IntegrationTests

open NUnit.Framework
open RaftCraft.IntegrationTests.Framework

type Candidate() =

    let createFixture() = 
        new RaftTestHarness(3, (fun _ -> 50), (fun _ -> 1000), (fun _ -> 200))
        |> Initialize

    [<Test>]
    member __.``Initial state is correct``() = 
        let fixture = createFixture()
        
