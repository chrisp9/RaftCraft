module Utils

open RaftCraft.Domain
open Operators
open System.Collections.Generic
open System
open RaftCraft.RaftDomain

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

// Holds entries for a fixed amount of time.
type Pipeline(retryIntervalMs : int) =
    let requestsById = Dictionary<Guid, RequestMessage>()
    let requestsByTick = SortedDictionary<int64, HashSet<Guid>>()

    // We expect that the amortised number of ExpiryTicks will be O(1), so we benefit from pooling. We only expect 
    // entries for N time slots. At each tick, we retry any items scheduled for that tick and return this HashSet
    // to the pool.
    let pool = HashSetPool()

    let getOrAdd(dict : IDictionary<'a, 'b>, key : 'a, valueFactory : unit -> 'b) =
        let mutable success, value = dict.TryGetValue(key)
        if (not success) then
            value <- valueFactory()
            dict.[key] <- value
        value
    
    // TODO Although the Raft specification says that we should retry indefinitely, I'm not convnced that this is a good
    // idea. We can't just store these items in memory indefinitely, since under sufficient load we would eventually 
    // hit performance problems. A better approach may be to retry the action for a given number of times, then completely 
    // back off, clear the state and in future only send the periodic empty AppendEntriesRequests until we hear back a 
    // successful response. The main reason for the retries is to attempt to avoid packet loss from causing election timeouts. 
    // Importantly, the Raft algorithm makes no assumptions about the underlying protocol or message ordering.
    // This implementation doesn't even guarantee message ordering. Incoming messages are optimistically sent to peers, but we
    // may have many prior messages in the retry collection. We have to be optimistic - assume the delayed messages will
    
    // eventually received by the peer, and let the algorithm handle any state inconsistences as a result of message reordering.
    // Enforcing only one in-flight message per peer would be too slow.

    // For now though, we just retry indefinitely.
    member __.Add(message : RequestMessage, currentTick : TimerTick) =

        let expiryTick = currentTick.CurrentTick + ((int64 retryIntervalMs) / (int64 (currentTick.Granularity)))

        let expiriesForThisTick = getOrAdd(requestsByTick, expiryTick, fun() -> pool.Borrow())

        expiriesForThisTick.Add(message.RequestId) |> ignore

        requestsById.[message.RequestId] <- message
