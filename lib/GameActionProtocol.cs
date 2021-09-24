using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace t.lib
{
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public struct GameActionProtocol
    {
        //0b000010
        public byte Version;
        /// <summary>
        /// submitter id
        /// </summary>
        public Guid PlayerId;
        /// <summary>
        /// 0b000001 - ok
        /// 0b000010 - waiting players
        /// 0b000011 - kicked player
        /// 0b000100 - register player
        /// 0b000101 - new player (broadcast)
        /// 0b000110 - start game
        /// 0b000111 - player report
        /// 0b001000 - next round
        /// 0b001001 - player scored (can occour multiple times for each player)
        /// 0b001010 - player won
        /// 0b111110 - bye
        /// 0b111111 - error occoured
        /// </summary>
        public byte Phase;
        /// <summary>
        /// The size of the payload
        /// </summary>
        public byte PayloadSize;
        /// <summary>
        /// waiting players - contains the server name for phase
        /// kicked player - the guid of the playerid
        /// register player - contains the registered playername for phase
        /// start game - contains the amoung of players
        /// player reported - contains the number of the played card
        /// next round - the card which can be won
        /// player scored - the amount of points the player scored
        /// new player - playerguid|palyername\r\n|requiredplayer
        /// </summary>
        public byte[] Payload;
    }
}
