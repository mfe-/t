using System;
using System.Runtime.Serialization;

namespace t.lib.Network
{
    [Serializable]
    public class GameActionProtocolException : Exception
    {
        public GameActionProtocolException(GameActionProtocol gameActionProtocol, string? message) : base(message)
        {
            GameActionProtocol = gameActionProtocol;
        }
        protected GameActionProtocolException(SerializationInfo info, StreamingContext context) : base(info, context)
        {

        }
        public GameActionProtocol GameActionProtocol { get; }
    }
}
