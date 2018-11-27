module Utils

open RaftCraft.Domain
open Operators
open System.Collections.Generic

let (|AppendEntriesRequest|_|) (request: RequestMessage) =
    !?request.AppendEntriesRequest
    
let (|AppendEntriesResponse|_|) (request: RequestMessage) =
    !?request.AppendEntriesResponse

let (|VoteRequest|_|) (request: RequestMessage) =
    !?request.VoteRequest
    
let (|VoteResponse|_|) (request: RequestMessage) =
    !?request.VoteResponse


type HashSetPool<'a>() =
    let hashSets = new Queue<HashSet<'a>>()

    member __.Borrow() =
        if hashSets.Count = 0 then
            hashSets.Enqueue(new HashSet<'a>())
        hashSets.Dequeue()
        

    member __.Return(set : HashSet<'a>) =
        set.Clear()
        hashSets.Enqueue(set)