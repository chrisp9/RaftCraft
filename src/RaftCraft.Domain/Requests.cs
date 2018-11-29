﻿using ProtoBuf;
using System;

namespace RaftCraft.Domain
{
    [ProtoContract]
    [Serializable]
    public class RaftMessage
    {
        [ProtoMember(1)]
        public int SourceNodeId { get; set; }

        [ProtoMember(2)]
        public Guid RequestId { get; set; }

        [ProtoMember(3)]
        public AppendEntriesRequest AppendEntriesRequest { get; set; }

        [ProtoMember(4)]
        public AppendEntriesResponse AppendEntriesResponse { get; set; }

        [ProtoMember(5)]
        public VoteRequest VoteRequest { get; set; }

        [ProtoMember(6)]
        public VoteResponse VoteResponse { get; set; }

        private RaftMessage(
            int sourceNodeId,
            Guid requestId,
            AppendEntriesRequest appendEntriesRequest,
            AppendEntriesResponse appendEntriesResponse,
            VoteRequest voteRequest,
            VoteResponse voteResponse)
        {
            RequestId = requestId;
            SourceNodeId = sourceNodeId;

            AppendEntriesRequest = appendEntriesRequest;
            AppendEntriesResponse = appendEntriesResponse;

            VoteRequest = voteRequest;
            VoteResponse = voteResponse;
        }

        public override string ToString()
        {
            var str = $"{nameof(RaftMessage)}: " +
                $"{nameof(SourceNodeId)}: {SourceNodeId}, " +
                $"{nameof(RequestId)}: {RequestId}, ";

            if (AppendEntriesRequest != null)
                str = str + $"{nameof(AppendEntriesRequest)}: {AppendEntriesRequest}, ";
            if (AppendEntriesResponse != null)
                str = str + $"{nameof(AppendEntriesResponse)}: {AppendEntriesResponse}, ";
            if (VoteRequest != null)
                str = str + $"{nameof(VoteRequest)}: {VoteRequest}, ";
            if(VoteResponse != null)
                str = str + $"{nameof(VoteResponse)}: {VoteResponse}";

            return str;
        }

        public RaftMessage() { }

        public static RaftMessage NewAppendEntiresRequest(
            int nodeId,
            Guid requestId, 
            AppendEntriesRequest appendEntries)
        {
            return new RaftMessage(nodeId, requestId, appendEntries, null, null, null);
        }

        public static RaftMessage NewAppendEntriesResponse(
            int nodeId,
            Guid requestId,
            AppendEntriesResponse appendEntriesResponse)
        {
            return new RaftMessage(nodeId, requestId, null, appendEntriesResponse, null, null);
        }

        public static RaftMessage NewVoteRequest(
            int nodeId,
            Guid requestId, 
            VoteRequest voteRequest)
        {
            return new RaftMessage(nodeId, requestId, null, null, voteRequest, null);
        }

        public static RaftMessage NewVoteResponse(
            int nodeId,
            Guid requestId,
            VoteResponse voteResponse)
        {
            return new RaftMessage(nodeId, requestId, null, null, null, voteResponse);
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

        public override string ToString()
        {
            return $"{nameof(VoteRequest)}: " +
                $"{nameof(Term)}: {Term}, " +
                $"{nameof(CandidateId)}: {CandidateId}, " +
                $"{nameof(LastLogIndex)}: {LastLogIndex}, " +
                $"{nameof(LastLogTerm)}: {LastLogTerm}";
        }
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
        public LogEntry[] Entries { get; set; }

        public AppendEntriesRequest(int term, int leaderId, int prevLogIndex, LogEntry[] entries)
        {
            Term = term;
            LeaderId = leaderId;
            PrevLogIndex = prevLogIndex;
            Entries = entries;
        }

        public AppendEntriesRequest() { }

        public override string ToString()
        {
            return $"{nameof(AppendEntriesRequest)}: " +
                $"{nameof(Term)}: {Term}, " +
                $"{nameof(LeaderId)}: {LeaderId}, " +
                $"{nameof(PrevLogIndex)}: {PrevLogIndex}, " +
                $"{nameof(Entries)}: {Entries}";
        }
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

        public override string ToString()
        {
            return $"{nameof(AppendEntriesResponse)}: " +
                $"{nameof(Successful)}: {Successful}";
        }
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

        public VoteResponse() { }

        public override string ToString()
        {
            return $"{nameof(VoteResponse)}: " +
                $"{nameof(Term)}: {Term}, " +
                $"{nameof(VoteGranted)}: {VoteGranted}";
        }
    }
}