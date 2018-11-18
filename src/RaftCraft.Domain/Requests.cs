﻿using ProtoBuf;
using System;

namespace RaftCraft.Domain
{
    [ProtoContract]
    [Serializable]
    public class RequestMessage
    {
        [ProtoMember(1)]
        public int NodeId { get; set; }

        [ProtoMember(2)]
        Guid RequestId { get; set; }

        [ProtoMember(3)]
        public AppendEntriesRequest AppendEntriesRequest { get; set; }

        [ProtoMember(4)]
        public AppendEntriesResponse AppendEntriesResponse { get; set; }

        [ProtoMember(5)]
        public VoteRequest VoteRequest { get; set; }

        [ProtoMember(6)]
        public VoteResponse VoteResponse { get; set; }

        private RequestMessage(
            int nodeId,
            Guid requestId,
            AppendEntriesRequest appendEntriesRequest,
            AppendEntriesResponse appendEntriesResponse,
            VoteRequest voteRequest,
            VoteResponse voteResponse)
        {
            RequestId = requestId;
            NodeId = nodeId;

            AppendEntriesRequest = appendEntriesRequest;
            AppendEntriesResponse = appendEntriesResponse;

            VoteRequest = voteRequest;
            VoteResponse = voteResponse;
        }

        public RequestMessage() { }

        public static RequestMessage NewAppendEntiresRequest(
            int nodeId,
            Guid requestId, 
            AppendEntriesRequest appendEntries)
        {
            return new RequestMessage(nodeId, requestId, appendEntries, null, null, null);
        }

        public static RequestMessage NewAppendEntriesResponse(
            int nodeId,
            Guid requestId,
            AppendEntriesResponse appendEntriesResponse)
        {
            return new RequestMessage(nodeId, requestId, null, appendEntriesResponse, null, null);
        }

        public static RequestMessage NewVoteRequest(
            int nodeId,
            Guid requestId, 
            VoteRequest voteRequest)
        {
            return new RequestMessage(nodeId, requestId, null, null, voteRequest, null);
        }

        public static RequestMessage NewVoteResponse(
            int nodeId,
            Guid requestId,
            VoteResponse voteResponse)
        {
            return new RequestMessage(nodeId, requestId, null, null, null, voteResponse);
        }
    }

    [ProtoContract]
    [Serializable]
    public class VoteRequest
    {
        [ProtoMember(1)]
        public int Term { get; set; }

        [ProtoMember(2)]
        public int CandidateId { get; set; }

        [ProtoMember(3)]
        public int LastLogIndex { get; set; }

        [ProtoMember(4)]
        public int LastLogTerm { get; set; }

        public VoteRequest(
            int term, 
            int candidateId, 
            int lastLogIndex, 
            int lastLogTerm)
        {
            Term = term;
            CandidateId = candidateId;
            LastLogIndex = lastLogIndex;
            LastLogTerm = lastLogTerm;
        }

        public VoteRequest() { }
    }

    [ProtoContract]
    [Serializable]
    public class AppendEntriesRequest
    {
        [ProtoMember(1)]
        public int Term { get; set; }

        [ProtoMember(2)]
        public int LeaderId { get; set; }

        [ProtoMember(3)]
        public int PrevLogIndex { get; set; }

        [ProtoMember(4)]
        public byte[][] Entries { get; set; }

        public AppendEntriesRequest(int term, int leaderId, int prevLogIndex, byte[][] entries)
        {
            Term = term;
            LeaderId = leaderId;
            PrevLogIndex = prevLogIndex;
            Entries = entries;
        }

        public AppendEntriesRequest() { }
    }

    [ProtoContract]
    [Serializable]
    public class AppendEntriesResponse
    {
        [ProtoMember(1)]
        public bool Successful { get; set; }

        public AppendEntriesResponse(bool successful)
        {
            Successful = successful;
        }

        public AppendEntriesResponse() { }
    }

    [ProtoContract]
    [Serializable]
    public class VoteResponse
    {
        [ProtoMember(1)]
        public int Term { get; set; }

        [ProtoMember(2)]
        public bool VoteGranted { get; set; }

        public VoteResponse(int term, bool voteGranted)
        {
            Term = term;
            VoteGranted = voteGranted;
        }
    }
}