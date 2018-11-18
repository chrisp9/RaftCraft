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
        public VoteRequest VoteRequest { get; set; }

        private RequestMessage(
            Guid requestId,
            AppendEntriesRequest appendEntriesRequest,
            VoteRequest voteRequest,
            int nodeId)
        {
            RequestId = requestId;
            AppendEntriesRequest = appendEntriesRequest;
            VoteRequest = voteRequest;
        }

        public RequestMessage() { }

        public static RequestMessage AppendEntries(
            int nodeId,
            Guid requestId, 
            AppendEntriesRequest appendEntries)
        {
            return new RequestMessage(requestId, appendEntries, null, nodeId);
        }

        public static RequestMessage Vote(
            int nodeId,
            Guid requestId, 
            VoteRequest vote)
        {
            return new RequestMessage(requestId, null, vote, nodeId);
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
}
