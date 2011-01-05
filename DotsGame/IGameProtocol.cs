using System;

namespace DotsGame {
    public interface IGameProtocol {
        void sendMessage(string message);
        void sendMove(int x, int y);
        void sendStartSignal();
        event MessageReceivedEventHandler OnMessageReceived;
        event MoveReceivedEventHandler OnMoveReceived;
        event EventHandler OnDisconnected;
        event EventHandler OnConnected;
        event EventHandler OnGameStarted;
        event EventHandler OnLeave;
        ICommunicator GetCommunicator();
    }

    public delegate void MessageReceivedEventHandler(object sender, MessageReceivedEventArgs args);

    public delegate void MoveReceivedEventHandler(object sender, MoveReceivedEventArgs args);

    public class MessageReceivedEventArgs : EventArgs {
        public string Message {
            get;
            set;
        }

        public MessageReceivedEventArgs(string message) {
            Message = message;
        }
    }

    public class MoveReceivedEventArgs : EventArgs {
        public int X {
            get;
            set;
        }

        public int Y {
            get;
            set;
        }

        public MoveReceivedEventArgs(int x, int y) {
            X = x;
            Y = y;
        }
    }
}