using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace DotsGame {
    public sealed class GameObservedEventArgs : EventArgs {
        public GameInfo Game {
            get;
            private set;
        }

        public GameObservedEventArgs(GameInfo game) {
            this.Game = game;
        }
    }

    public delegate void GameObservedEventHandler(object sender, GameObservedEventArgs args);

    /// <summary>
    /// Представляет собой UDP сервер, принимающий широковещательные сообщения о созданных играх
    /// плюс TCP клиент для подключения к хосту созданной игры.
    /// </summary>
    public sealed class GameClient : ICommunicator {
        public static int[] UDP_PORTS = new[] {
            1435,
            1436,
        };

        private int usedUdpPort;

        private GameClient() {
        }

        private static GameClient instance;

        public static GameClient Instance {
            get {
                return instance ?? (instance = new GameClient());
            }
        }

        public event GameObservedEventHandler OnGameObserved;

        private void invokeOnInfoUpdated(GameObservedEventArgs e) {
            GameObservedEventHandler handler = this.OnGameObserved;
            if (handler != null) {
                handler(this, e);
            }
        }

        private Thread listeningThread;
        private bool isStopped = false;
        private readonly object locker = new object();
        private readonly EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        public void StartClient() {
            // Пробуем занять один из портов, на которые будут присылаться широковещательные оповещения
            UdpClient client = null;
            foreach (int port in UDP_PORTS) {
                try {
                    client = new UdpClient(port);
                    usedUdpPort = port;
                    break;
                } catch (SocketException) {
                    //
                }
            }
            if (client == null) {
                throw new InvalidOperationException("Ну все порты заняты, чо."); // Провал
            }
            listeningThread = new Thread(Start);
            listeningThread.Start(client);
        }

        public void StopClient() {
            if (listeningThread != null) {
                lock (locker) {
                    isStopped = true;
                }
                waitHandle.Set();
                listeningThread.Join();
            }
        }

        private TcpClient connectedClient;
        private Thread connectedClientThread;

        private readonly EventWaitHandle connectedClientThreadWaitHandle = new EventWaitHandle(false,
                                                                                               EventResetMode.AutoReset);

        private bool connectedClientThreadStopped;

        /// <summary>
        /// Извлекает IP адрес хоста игры и пытается с ним соединиться.
        /// При успешном соединении запускает поток, который обрабатывает TCP соединения, по кд
        /// посылая туда накопившиеся данные и забирая то, что приходит со стороны сервера.
        /// </summary>
        /// <param name="game">Один из объектов, полученных при прослушивании UDP.</param>
        /// <param name="playerName"></param>
        public void ConnectToGame(GameInfo game, string playerName) {
            try {
                connectedClient = new TcpClient();
                connectedClient.Connect(new IPEndPoint(game.OwnerIP, GameServer.TCP_PORT));
                // Отсылаем имя подключившегося игрока
                connectedClient.Client.Send(Encoding.UTF8.GetBytes("player:" + playerName));
                // Стартуем поток, обрабатывающий TCP соединение с сервером игры
                connectedClientThread = new Thread(connectedClientThreadFunc);
                connectedClientThread.IsBackground = false;
                connectedClientThread.Start(connectedClient);
                //
                firstPlayerName = game.OwnerName;
                secondPlayerName = playerName;
                EventHandler eventHandler = OnPlayerNameReceived;
                if (eventHandler != null) {
                    eventHandler.Invoke(this, EventArgs.Empty);
                }
                //
                this.gameName = game.Name;
                //
                EventHandler handler = OnConnect;
                if (handler != null)
                {
                    handler.Invoke(this, EventArgs.Empty);
                }
                //
            } catch (Exception) {
                connectedClient = null;
                connectedClientThread = null;
                firstPlayerName = null;
                secondPlayerName = null;
                gameName = null;
                //
                throw;
            }
        }

        public void DisconnectFromGame() {
            if (connectedClient != null) {
                if (connectedClientThread.IsAlive) {
                    lock (locker) {
                        connectedClientThreadStopped = true;
                    }
                    connectedClientThreadWaitHandle.Set();
                    connectedClientThread.Join();
                    //
                    connectedClient = null;
                    connectedClientThread = null;
                    connectedClientThreadStopped = false;
                    //
                    firstPlayerName = null;
                    secondPlayerName = null;
                    EventHandler eventHandler = OnPlayerNameReceived;
                    if (eventHandler != null) {
                        eventHandler.Invoke(this, EventArgs.Empty);
                    }
                    //
                    gameName = null;
                }
            }
        }

        private void connectedClientThreadFunc(object param) {
            TcpClient client = (TcpClient) param;
            bool receivingStarted = false;
            //
            try {
                while (client.Connected) {
                    //
                    if (!receivingStarted) {
                        receivingStarted = true;
                        Byte[] bytes = new byte[256];
                        client.GetStream().BeginRead(bytes, 0, bytes.Length, ar => {
                            try
                            {
                                int readed = client.GetStream().EndRead(ar);
                                // Raise event about new data readed
                                DataReceiveEventArgs args = new DataReceiveEventArgs(bytes, readed);
                                DataReceiveEventHandler handler = OnDataReceive;
                                if (null != handler)
                                {
                                    handler.Invoke(this, args);
                                }
                                //
                                Debug.Print("Readed {0} bytes : {1}", readed, Encoding.UTF8.GetString(bytes));
                                Logger.Log("GAME CLIENT : Readed " + readed + " bytes : " +
                                           Encoding.UTF8.GetString(bytes));
                                receivingStarted = false;
                            }
                            catch (ObjectDisposedException)
                            {
                            }
                            catch (IOException)
                            {
                                lock (locker)
                                {
                                    connectedClientThreadStopped = true;
                                }
                            }
                            catch (InvalidOperationException) {
                                // TODO : Пофиксить. Если много раз заходить/перезаходить в игру, то здесь падает
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
                        client.Client.Send(bytesToWrite);
                        Logger.Log("GAME CLIENT : Sent : " + Encoding.UTF8.GetString(bytesToWrite));
                    }
                    //
                    lock (locker) {
                        if (connectedClientThreadStopped && queue.Count == 0) {
                            break;
                        }
                    }
                    connectedClientThreadWaitHandle.WaitOne(200);
                }
            } catch (Exception exc) {
                Debug.Print("Unexpected exception in connectedClientThreadFunc : {0}", exc);
            } finally {
                client.Close();
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

        private void Start(object o) {
            Debug.Print("Listening thread begin.");
            UdpClient client = (UdpClient) o;
            bool clientIsListening = false;
            while (true) {
                waitHandle.WaitOne(500);
                lock (locker) {
                    if (isStopped) {
                        break;
                    }
                }
                if (!clientIsListening) {
                    clientIsListening = true;
                    client.BeginReceive(ar => {
                        try {
                            UdpClient udpClient = (UdpClient) ar.AsyncState;
                            // В эту переменную запишется endpoint хоста игры, которую мы нашли
                            // Потом мы будем пытаться законнектиться по TCP на этот IP
                            IPEndPoint gameHostEndPoint = new IPEndPoint(IPAddress.Any, usedUdpPort);
                            byte[] receivedBytes = udpClient.EndReceive(ar, ref gameHostEndPoint);
                            GameInfo gameInfo = getGameInfoFromData(receivedBytes);
                            gameInfo.OwnerIP = gameHostEndPoint.Address;
                            //
                            invokeOnInfoUpdated(new GameObservedEventArgs(gameInfo));
                            //
                        } catch (Exception) {
                            Debug.Print("Unexpected exception in UDP listening callback.");
                        } finally {
                            clientIsListening = false;
                        }
                    }, client);
                }
            }
            Debug.Print("Listening thread finished.");
        }

        private static GameInfo getGameInfoFromData(byte[] data) {
            GameInfo gameInfo = new GameInfo();
            string gameInfoString = Encoding.UTF8.GetString(data);
            string[] gameInfoStringParts = gameInfoString.Split(new[] {
                ":"
            }, StringSplitOptions.None);
            gameInfo.UniqueHostId = new Guid(gameInfoStringParts[0]);
            gameInfo.Name = gameInfoStringParts[1];
            gameInfo.OwnerName = gameInfoStringParts[2];
            gameInfo.PlayersConnectedCount = Int32.Parse(gameInfoStringParts[3]);
            return gameInfo;
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
            DisconnectFromGame();
            //
            firstPlayerName = null;
            secondPlayerName = null;
            EventHandler handler = OnPlayerNameReceived;
            if (null != handler) {
                handler.Invoke(this, EventArgs.Empty);
            }
        }

        private const string UNKNOWN_PLAYER_NAME = "Неизвестный игрок";
        private string firstPlayerName;

        public string FirstPlayerName {
            get {
                if (string.IsNullOrEmpty(firstPlayerName)) {
                    return UNKNOWN_PLAYER_NAME;
                }
                return firstPlayerName;
            }
        }

        private string secondPlayerName;

        public string SecondPlayerName {
            get {
                if (string.IsNullOrEmpty(secondPlayerName)) {
                    return UNKNOWN_PLAYER_NAME;
                }
                return secondPlayerName;
            }
        }

        public event EventHandler OnPlayerNameReceived;

        public bool IsOwner {
            get {
                return false;
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