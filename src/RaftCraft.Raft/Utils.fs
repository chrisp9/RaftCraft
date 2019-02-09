﻿module Utils

open RaftCraft.Domain
open Operators
open System.Collections.Generic
open System
open RaftCraft.RaftDomain

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
        let set = hashSets.Dequeue()
        set.Clear()
        set
        
    member __.Return(set : HashSet<'a>) =
        hashSets.Enqueue(set)

type public TimerUtils() =
    static member CalculateExpiryTick (timer) (granularity) (fuzzFactor) =
        (timer / granularity) + fuzzFactor + int64 10
        

// Holds entries for a fixed amount of time.
type Pipeline(retryIntervalMs : int) =
    let requestsById = Dictionary<Guid, RaftMessage>()
    let requestsByTick = SortedDictionary<int64, HashSet<Guid>>()

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

    member __.Remove(messageId) =  
        lock token (fun() ->
            let result = requestsById.Remove(messageId)
            result
        )

    // TODO Race. What happens if currentTick is in the past? The caller is not in lockstep with the expiry function.
    // Might be better to use F# agent in the PeerSupervisor and each PeerDiplomat.
    member __.Add(message : RaftMessage, currentTick : TimerTick) =

        let expiryTick = currentTick.CurrentTick + ((int64 retryIntervalMs) / (int64 (currentTick.Granularity)))

        // In rare edge cases with frequent retry interval, the expiry tick might be for the current clock tick quantum,
        // so we have to shift forward by a single tick.
        let adjustedExpiryTick = if expiryTick <= currentTick.CurrentTick then expiryTick + (int64 1) else expiryTick

        lock token (fun() ->
            let expiriesForThisTick = getOrAdd(requestsByTick, adjustedExpiryTick, fun() -> pool.Borrow())

            expiriesForThisTick.Add(message.RequestId) |> ignore

            requestsById.[message.RequestId] <- message)
           
    // Checks for any requests which have expired on the current tick (or before). 
    // Retries sending any requests which are still in-flight (haven't received
    // a response from the Peer).
    // Cleans up any requests which are no longer in-flight so that we don't retry
    // them again.
    member __.Expiry (currentTick : TimerTick) (nodeState : NodeStateHolder) (post) =
        lock token (fun() ->
            let ticks = HashSet()
            let requests = HashSet()

            let term = nodeState.Current().Term

            // First we need to figure out which messages are no longer in-flight so that
            // we don't resend them forever....
            for item in requestsByTick do
                if item.Key <= currentTick.CurrentTick then
                    ticks.Add(item.Key) |> ignore

                    for expiryCheckedItem in item.Value do
                        let success, message = requestsById.TryGetValue expiryCheckedItem

                        if(success && message.Term >= term) then
                            requests.Add(expiryCheckedItem) |> ignore
                        else
                            requestsById.Remove expiryCheckedItem |> ignore
            
            for item in ticks do
                requestsByTick.Remove(item) |> ignore

            let expiryTick = currentTick.CurrentTick + ((int64 retryIntervalMs) / (int64 (currentTick.Granularity)))
                
            let itemsAtExpiryTick = getOrAdd (requestsByTick, expiryTick, fun () -> HashSet<Guid>())
           
            // Before posting retries for the messages, we need to schedule retries for a future time- otherwise we will have
            // lost track of them!
            for request in requests do
                if(requestsById.ContainsKey(request)) then
                    itemsAtExpiryTick.Add(request) |> ignore
                else
                    for request in new HashSet<Guid>(requests) do 
                        if(not (requestsById.ContainsKey(request))) then
                            requests.Remove(request) |> ignore
                        
                    if(requests.Count = 0) then 
                        pool.Return(requests)
                    else
                        requestsByTick.[expiryTick] <- requests
            
            // Now that our schedule has been updated, we can post the messages.
            for request in requests do
                let success, messageForRequest = requestsById.TryGetValue(request)

                if success then post messageForRequest)