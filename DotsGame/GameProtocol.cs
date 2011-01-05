using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace DotsGame
{
    public sealed class GameProtocol : IGameProtocol
    {
        private readonly ICommunicator communicator;

        public GameProtocol(ICommunicator communicator) {
            if (communicator == null) {
                throw new ArgumentNullException("communicator");
            }
            this.communicator = communicator;
            communicator.OnDataReceive += CommunicatorOnOnDataReceive;
            communicator.OnDisconnect += CommunicatorOnOnDisconnect;
            communicator.OnConnect += CommunicatorOnOnConnect;
        }

        private void fireOnLeave() {
            EventHandler handler = OnLeave;
            if (null != handler) {
                handler.Invoke(this, EventArgs.Empty);
            }
        }

        private void CommunicatorOnOnConnect(object sender, EventArgs eventArgs) {
            EventHandler handler = OnConnected;
            if (null != handler) {
                handler.Invoke(this, EventArgs.Empty);
            }
        }

        private void CommunicatorOnOnDisconnect(object sender, EventArgs eventArgs) {
            var handler = OnDisconnected;
            if (null != handler) {
                handler.Invoke(this, EventArgs.Empty);
            }
        }

        private void CommunicatorOnOnDataReceive(object sender, DataReceiveEventArgs args) {
            string s = Encoding.UTF8.GetString(args.Data, 0, args.BytesRead);
            if (s.StartsWith("message:")) {
                string message = s.Substring("message:".Length);
                var handler = OnMessageReceived;
                if (null != handler) {
                    handler.Invoke(this, new MessageReceivedEventArgs(message));
                }
            } else if (s.StartsWith("move:")) {
                Debug.Print("Received info about move : " + s.Substring("move:".Length));
                //
                int x = int.Parse(s.Substring("move:".Length).Split(new[] {
                    "-"
                }, StringSplitOptions.RemoveEmptyEntries)[0]);
                int y = int.Parse(s.Substring("move:".Length).Split(new[] {
                    "-"
                }, StringSplitOptions.RemoveEmptyEntries)[1]);
                var handler = OnMoveReceived;
                if (null != handler) {
                    handler.Invoke(this, new MoveReceivedEventArgs(x, y));
                }
            } else if (s.StartsWith("start:")) {
                var handler = OnGameStarted;
                if (null != handler) {
                    handler.Invoke(this, EventArgs.Empty);
                }
            } else if (s.StartsWith("leave:")) {
                fireOnLeave();
            }
        }

        public void sendMessage(string message) {
            communicator.sendData(Encoding.UTF8.GetBytes("message:" + message));
        }

        public void sendMove(int x, int y) {
            Debug.Print("Sent move : " + x + "-" + "y");
            communicator.sendData(Encoding.UTF8.GetBytes("move:" + x + "-" + y));
        }

        public void sendStartSignal() {
            communicator.sendData(Encoding.UTF8.GetBytes("start:"));
        }

        public event MessageReceivedEventHandler OnMessageReceived;
        public event MoveReceivedEventHandler OnMoveReceived;
        public event EventHandler OnDisconnected;
        public event EventHandler OnConnected;
        public event EventHandler OnGameStarted;
        public event EventHandler OnLeave;

        public ICommunicator GetCommunicator() {
            return communicator;
        }
    }
}
