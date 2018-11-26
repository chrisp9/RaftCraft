module Utils

open RaftCraft.Domain
open Operators

let (|AppendEntriesRequest|_|) (request: RequestMessage) =
    !?request.AppendEntriesRequest
    
let (|AppendEntriesResponse|_|) (request: RequestMessage) =
    !?request.AppendEntriesResponse

let (|VoteRequest|_|) (request: RequestMessage) =
    !?request.VoteRequest
    
let (|VoteResponse|_|) (request: RequestMessage) =
    !?request.VoteResponse