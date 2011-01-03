using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotsGame
{
    public interface ICommunicator {
        void sendData(byte[] data);
        event DataReceiveEventHandler OnDataReceive;
        event EventHandler OnDisconnect;
        event EventHandler OnConnect;
        void LeaveGame();

        string FirstPlayerName {
            get;
        }

        string SecondPlayerName {
            get;
        }

        event EventHandler OnPlayerNameReceived;

        bool IsOwner {
            get;
        }

        string GameName {
            get;
        }
    }

    public class DataReceiveEventArgs : EventArgs {
        public DataReceiveEventArgs(byte[] data, int bytesRead) {
            this.Data = data;
            this.BytesRead = bytesRead;
        }

        public byte[] Data {
            get;
            set;
        }

        public int BytesRead
        {
            get;
            set;
        }
    }

    public delegate void DataReceiveEventHandler(object sender, DataReceiveEventArgs args);
}
