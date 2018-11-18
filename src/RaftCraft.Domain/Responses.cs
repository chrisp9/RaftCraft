using ProtoBuf;
using System;

namespace RaftCraft.Domain
{
    [ProtoContract]
    [Serializable]
    public class ResponseMessage
    {
        [ProtoMember(1)]
        public Guid RequestId { get; set; }

        [ProtoMember(2)]
        public AppendEntriesResponse AppendEntriesResponse { get; set; }

        [ProtoMember(3)]
        public VoteResponse VoteResponse { get; set; }

        private ResponseMessage(
            Guid requestId, 
            AppendEntriesResponse appendEntriesResponse, 
            VoteResponse voteResponse)
        {
            RequestId = requestId;
            AppendEntriesResponse = appendEntriesResponse;
            VoteResponse = voteResponse;
        }

        public ResponseMessage() { }

        public static ResponseMessage AppendEntries(Guid requestId, AppendEntriesResponse appendEntriesResponse)
        {
            return new ResponseMessage(requestId, appendEntriesResponse, null);
        }

        public static ResponseMessage Vote(Guid requestId, VoteResponse voteResponse)
        {
            return new ResponseMessage(requestId, null, voteResponse);
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
