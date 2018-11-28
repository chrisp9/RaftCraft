module Utils

open RaftCraft.Domain
open Operators
open System.Collections.Generic
open System
open RaftCraft.RaftDomain
open System.ServiceModel.Channels

let (|AppendEntriesRequest|_|) (request: RaftMessage) =
    !?request.AppendEntriesRequest
    
let (|AppendEntriesResponse|_|) (request: RaftMessage) =
    !?request.AppendEntriesResponse

let (|VoteRequest|_|) (request: RaftMessage) =
    !?request.VoteRequest
    
let (|VoteResponse|_|) (request: RaftMessage) =
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
    let requestsById = Dictionary<Guid, RaftMessage>()
    let requestsByTick = Dictionary<int64, HashSet<Guid>>()

    let token = Object()

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

    let stopwatch = System.Diagnostics.Stopwatch()

    member __.Add(message : RaftMessage, currentTick : TimerTick) =

        let expiryTick = currentTick.CurrentTick + ((int64 retryIntervalMs) / (int64 (currentTick.Granularity)))

        // Using lock here because it makes the rest of the code far simpler. I tried the alternative approach but decided
        // the othher approach (using agents) was more complex and probably less efficient than just using a lock here.

        stopwatch.Start()
        lock token (fun() ->
            stopwatch.Stop()
            let expiriesForThisTick = getOrAdd(requestsByTick, expiryTick, fun() -> pool.Borrow())

            expiriesForThisTick.Add(message.RequestId) |> ignore

            requestsById.[message.RequestId] <- message)
    
    member __.Expiry (currentTick : TimerTick) (post) =
        lock token (fun() -> 
            let success, requests = requestsByTick.TryGetValue(currentTick.CurrentTick)
            if success then
                requestsByTick.Remove(currentTick.CurrentTick) |> ignore
                let expiryTick = currentTick.CurrentTick + ((int64 retryIntervalMs) / (int64 (currentTick.Granularity)))
                requestsByTick.[expiryTick] <- requests
                
                for request in requests do
                    let success, messageForRequest = requestsById.TryGetValue(request)
                    if success then post messageForRequest)


