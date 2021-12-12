using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace t.lib.Network
{
    public static class AsByte
    {
        public static byte[] ToByteArray(this GameActionProtocol gameActionProtocol)
        {
            byte version = gameActionProtocol.Version;
            byte[] playerId = gameActionProtocol.PlayerId.ToByteArray();
            byte phase = gameActionProtocol.Phase;
            byte payloadsize = (byte)gameActionProtocol.Payload.Length;

            byte[] byteArray = new byte[1 + playerId.Length + 1 + 1 + payloadsize];
            int i = 0;
            byteArray[i] = version;
            for (i = 1; i < playerId.Length + 1; i++)
            {
                byteArray[i] = playerId[i - 1];
            }
            byteArray[i++] = phase;
            byteArray[i++] = payloadsize;
            for (int a = 0; i < byteArray.Length; i++, a++)
            {
                byteArray[i] = gameActionProtocol.Payload[a];
            }
            return byteArray;
        }
        private static int PositionVersion => 0;
        private static int PositionGuid => 1;
        private static int PositionPhase => Marshal.SizeOf(typeof(Guid)) + 1;
        private static int PositionPayloadSize => PositionPhase + 1;
        private static int PositionPayLoad => PositionPayloadSize + 1;

        public static GameActionProtocol ToGameActionProtocol(this byte[] byteArray, int length = -1)
        {
            if (length != -1 && byteArray.Length < length) throw new ArgumentOutOfRangeException($"Expected {nameof(byteArray)} with at least a length of {length} ");
            GameActionProtocol gameActionProtocol = new GameActionProtocol();
            gameActionProtocol.Version = byteArray[PositionVersion];
            Span<byte> byteSpan = byteArray;
            var guidSpan = byteSpan.Slice(PositionGuid, Marshal.SizeOf(typeof(Guid)));
            gameActionProtocol.PlayerId = new Guid(guidSpan);
            gameActionProtocol.Phase = byteSpan.Slice(PositionPhase, 1)[0];
            gameActionProtocol.PayloadSize = byteSpan.Slice(PositionPayloadSize, 1)[0];
            var payloadSpan = byteSpan.Slice(PositionPayLoad, gameActionProtocol.PayloadSize);
            gameActionProtocol.Payload = payloadSpan.ToArray();
            return gameActionProtocol;
        }
    }
}
