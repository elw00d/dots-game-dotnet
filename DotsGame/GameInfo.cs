using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DotsGame
{
    public sealed class GameInfo
    {
        public string OwnerName
        {
            get;
            set;
        }

        public Guid UniqueHostId
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public IPAddress OwnerIP
        {
            get;
            set;
        }

        public int PlayersConnectedCount
        {
            get;
            set;
        }

        public override string ToString() {
            return String.Format("{0} (подключено {1} игроков) IP {2}", Name, PlayersConnectedCount, OwnerIP);
        }
    }
}
