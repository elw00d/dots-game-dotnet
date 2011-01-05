using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DotsGame {
    /// <summary>
    /// Представляет собой UDP клиент, рассылающий по таймеру broadcast-сообщения
    /// с информацией о созданной игре плюс TCP сервер, принимающий соединения от игроков и судей. 
    /// </summary>
    public sealed class GameServer : ICommunicator {
        /// <summary>
        /// Уникальный номер, идентифицирующий этот экземпляр программы, создающей игры.
        /// При получении информации о созданной игре мы проверяем, не мы ли собственно сами ее же и создали.
        /// Если да, то не выводим в список игр свою игру.
        /// Генерируется в конструкторе класса.
        /// </summary>
        public Guid UniqueId {
            get;
            private set;
        }

        private GameServer() {
            UniqueId = Guid.NewGuid();
        }

        private static GameServer instance;

        public static GameServer Instance {
            get {
                return instance ?? (instance = new GameServer());
            }
        }

        public static int TCP_PORT = 1234;

        private Timer timer;

        public void CreateGame(string name, string owner) {
            // Создаем коннектор UDP, который начинает рассылать широковещательные сообщения о созданной игре
            IPAddress broadcastIp = IPAddress.Broadcast;
            List<IPEndPoint> broadcastEndPoint = new List<IPEndPoint>(GameClient.UDP_PORTS.Length);
            foreach (int port in GameClient.UDP_PORTS) {
                broadcastEndPoint.Add(new IPEndPoint(broadcastIp, port));
            }
            UdpClient client = new UdpClient();

            timer = new Timer(state => {
                if (this.clientsAcceptedCount == 0) {
                    byte[] bytes =
                        Encoding.UTF8.GetBytes(UniqueId + ":" + name + ":" + owner + ":" + clientsAcceptedCount + 1);
                    foreach (IPEndPoint endPoint in broadcastEndPoint) {
                        client.Send(bytes, bytes.Length, endPoint);
                    }
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            // Создаем TCP сервер, который будет принимать соединения игроков, желающих поиграть
            tcpListener = new TcpListener(new IPEndPoint(IPAddress.Any, TCP_PORT));
            tcpListener.Start();
            //
            tcpListenerThread = new Thread(tcpListenerThreadFunc);
            tcpListenerThread.IsBackground = false;
            tcpListenerThread.Start(tcpListener);
            //
            this.firstPlayerName = owner;
            EventHandler handler = OnPlayerNameReceived;
            if (null != handler) {
                handler.Invoke(this, EventArgs.Empty);
            }
            //
            gameName = name;
        }

        private TcpListener tcpListener;

        private Thread tcpListenerThread;
        private readonly object locker = new object();

        private readonly EventWaitHandle tcpListenerThreadWaitHandle = new EventWaitHandle(false,
                                                                                           EventResetMode.AutoReset);

        private bool tcpListenerThreadStopped = false;
        private int clientsAcceptedCount = 0;

        private Thread acceptedClientThread;

        private readonly EventWaitHandle acceptedClientThreadWaitHandle = new EventWaitHandle(false,
                                                                                              EventResetMode.AutoReset);

        private bool acceptedClientThreadStopped;

        private void tcpListenerThreadFunc(object param) {
            Debug.Print("TCP Listener thread has started.");
            TcpListener listener = (TcpListener) param;
            bool anyListeningStarted = false;
            while (true) {
                if (!anyListeningStarted) {
                    anyListeningStarted = true;
                    listener.BeginAcceptTcpClient(ar => {
                        try {
                            TcpClient acceptedClient = listener.EndAcceptTcpClient(ar);
                            if (clientsAcceptedCount > 0) {
                                acceptedClient.Close();
                            } else {
                                clientsAcceptedCount++;
                                //
                                startAcceptedClientThread(acceptedClient);
                            }
                        } catch (Exception) {
                            Debug.Print("Unexpected exception in TCP Listener BeginAccept callback.");
                        } finally {
                            anyListeningStarted = false;
                        }
                    }, null);
                }
                tcpListenerThreadWaitHandle.WaitOne(200);
                lock (locker) {
                    if (tcpListenerThreadStopped) {
                        break;
                    }
                }
            }
            Debug.Print("TCP Listener thread has finished.");
        }

        private void startAcceptedClientThread(TcpClient acceptedClient) {
            acceptedClientThreadStopped = false;
            acceptedClientThread = new Thread(acceptedClientThreadFunc);
            acceptedClientThread.IsBackground = false;
            acceptedClientThread.Start(acceptedClient);
            EventHandler handler = OnConnect;
            if (handler != null) {
                handler.Invoke(this, EventArgs.Empty);
            }
        }
        
        /// <summary>
        /// Этот поток постоянно прослушивает клиентский сокет.
        /// Если от клиента приходят какие-либо данные, то вызывается событие.
        /// Если же клиенту нужно отправить данные, они сначала складываются в очередь и потом во время
        /// работы потока отправляются.
        /// </summary>
        /// <param name="param"></param>
        private void acceptedClientThreadFunc(object param) {
            TcpClient acceptedClient = (TcpClient) param;
            //
            bool receivingStarted = false;
            try {
                while (acceptedClient.Connected) {
                    if (!receivingStarted) {
                        receivingStarted = true;
                        Byte[] bytes = new Byte[256];
                        acceptedClient.GetStream().BeginRead(bytes, 0, bytes.Length, ar => {
                            try {
                                int readed = acceptedClient.GetStream().EndRead(ar);
                                // Raise event about new data readed
                                DataReceiveEventArgs args = new DataReceiveEventArgs(bytes, readed);
                                DataReceiveEventHandler handler = OnDataReceive;
                                if (null != handler) {
                                    handler.Invoke(this, args);
                                }
                                //
                                string s = Encoding.UTF8.GetString(bytes, 0, readed);
                                if (s.StartsWith("player:")) {
                                    secondPlayerName = s.Split(new[] {
                                        ":"
                                    }, StringSplitOptions.None)[1];
                                    //
                                    EventHandler eventHandler = OnPlayerNameReceived;
                                    if (null != eventHandler) {
                                        eventHandler.Invoke(this, EventArgs.Empty);
                                    }
                                }
                                //
                                if (s.StartsWith("leave:")) {
                                    acceptedClientThreadStopped = true;
                                }
                                Debug.Print("Readed {0} bytes : {1}", readed, Encoding.UTF8.GetString(bytes));
                                Logger.Log("GAME SERVER : Readed " + readed + " bytes : " +
                                           Encoding.UTF8.GetString(bytes));
                                receivingStarted = false;
                            } catch (ObjectDisposedException) {
                            } catch (IOException) {
                                lock (locker) {
                                    acceptedClientThreadStopped = true;
                                }
                            }
                            catch (InvalidOperationException)
                            {
                                // TODO : Пофиксить. Если ливать из игры, иногда падает тут (исключение аналогично
                                // TODO : исключению, возникающему на клиенте в GameClient)
                            }
                        }, null);
                    }
                    // Записываем все данные из очереди в поток tcp client'а
                    while (true) {
                        lock (locker) {
                            if (queue.Count == 0) {
                                break;
                            }
                        }
                        //
                        byte[] bytesToWrite;
                        lock (locker) {
                            bytesToWrite = queue[0];
                            queue.RemoveAt(0);
                        }
                        acceptedClient.Client.Send(bytesToWrite);
                        Logger.Log("GAME SERVER : Sent : " + Encoding.UTF8.GetString(bytesToWrite));
                    }
                    //
                    acceptedClientThreadWaitHandle.WaitOne(200);
                    lock (locker) {
                        if (acceptedClientThreadStopped && queue.Count == 0) {
                            break;
                        }
                    }
                }
            } catch (Exception) {
                Debug.Print("An exception in acceptedClientThreadFunc. May be client lost the connection.");
            } finally {
                clientsAcceptedCount--;
                acceptedClient.Close();
                try {
                    EventHandler handler = this.OnDisconnect;
                    if (null != handler) {
                        handler.Invoke(this, EventArgs.Empty);
                    }
                } catch (Exception) {
                    Debug.Print("Unexpected exception while handler of OnDisconnected been invoked.");
                }
            }
        }

        public void CancelGame() {
            // Стопим таймер
            if (timer != null) {
                timer.Dispose();
                timer = null;
            }
            tcpListener.Stop();
            // Стопим поток, прослушивающий TCP
            lock (locker) {
                tcpListenerThreadStopped = true;
            }
            tcpListenerThreadWaitHandle.Set();
            tcpListenerThread.Join();
            // Стопим поток, обслуживающий клиента, если кто-то был подключен
            if (null != acceptedClientThread) {
                lock (locker) {
                    acceptedClientThreadStopped = true;
                }
                acceptedClientThreadWaitHandle.Set();
                acceptedClientThread.Join();
            }
            //
            firstPlayerName = null;
            secondPlayerName = null;
            EventHandler handler = OnPlayerNameReceived;
            if (handler != null) {
                handler.Invoke(this, EventArgs.Empty);
            }
            //
            gameName = null;
            //
            Debug.Print("Game cancelled.");
        }

        private readonly List<byte[]> queue = new List<byte[]>();

        public void sendData(byte[] data) {
            lock (locker) {
                queue.Add(data);
            }
        }

        public event DataReceiveEventHandler OnDataReceive;
        public event EventHandler OnDisconnect;
        public event EventHandler OnConnect;

        public void LeaveGame() {
            sendData(Encoding.UTF8.GetBytes("leave:"));
            //
            CancelGame();
        }

        private const string UNKNOWN_PLAYER_NAME = "Неизвестный игрок";

        private string firstPlayerName;
        public string FirstPlayerName {
            get {
                if (string.IsNullOrEmpty(firstPlayerName))
                {
                    return UNKNOWN_PLAYER_NAME;
                }
                return firstPlayerName;
            }
        }

        private string secondPlayerName;
        public string SecondPlayerName {
            get {
                if (string.IsNullOrEmpty(secondPlayerName))
                {
                    return UNKNOWN_PLAYER_NAME;
                }
                return secondPlayerName;
            }
        }

        public event EventHandler OnPlayerNameReceived;

        public bool IsOwner {
            get {
                return true;
            }
        }

        private string gameName;
        public string GameName {
            get {
                return gameName;
            }
        }
    }
}