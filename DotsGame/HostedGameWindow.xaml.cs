﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DotsGame {
    /// <summary>
    /// Interaction logic for HostedGameWindow.xaml
    /// </summary>
    public partial class HostedGameWindow : Window {
        private GameWindow gameWindow;

        public HostedGameWindow(IGameProtocol gameProtocol) {
            Protocol = gameProtocol;
            //
            Protocol.OnMessageReceived += onProtocolOnOnMessageReceived;
            Protocol.OnGameStarted += onProtocolOnOnGameStarted;
            Protocol.OnMoveReceived += onProtocolOnOnMoveReceived;
            Protocol.OnConnected += onProtocolOnOnConnected;
            Protocol.OnDisconnected += onProtocolOnOnDisconnected;
            Protocol.GetCommunicator().OnPlayerNameReceived += onOnPlayerNameReceived;
            //
            InitializeComponent();
            //
            ICommunicator communicator = Protocol.GetCommunicator();
            if (communicator.IsOwner) {
                labelStatus.Content = "Ожидание подключения игрока...";
            } else {
                labelStatus.Content = "Ожидание подключения к хосту...";
                buttonStart.Visibility = Visibility.Hidden;
            }
            //
            this.labelFirstPlayerName.Content = communicator.FirstPlayerName;
            this.labelSecondPlayerName.Content = communicator.SecondPlayerName;
            //
            string gameName = Protocol.GetCommunicator().GameName;
            if (!string.IsNullOrEmpty(gameName)) {
                this.Title = gameName;
            }
            //
            this.buttonStart.IsEnabled = false;
        }

        private void onOnPlayerNameReceived(object o2, EventArgs args2) {
            Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                //
                ICommunicator _communicator = (ICommunicator) o2;
                this.labelFirstPlayerName.Content = _communicator.FirstPlayerName;
                this.labelSecondPlayerName.Content = _communicator.SecondPlayerName;
                return 0;
                //
            }));
        }

        private void onProtocolOnOnDisconnected(object o1, EventArgs args1) {
            Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                if (Protocol.GetCommunicator().IsOwner) {
                    this.buttonStart.IsEnabled = false;
                    labelStatus.Content = "Ожидание подключения игрока...";
                } else {
                    labelStatus.Content = "Ожидание подключения к хосту...";
                    buttonStart.Visibility = Visibility.Hidden;
                }
                return 0;
            }));
        }

        private void onProtocolOnOnConnected(object sender2, EventArgs eventArgs1) {
            Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                if (Protocol.GetCommunicator().IsOwner) {
                    labelStatus.Content = "Игрок зашел в игру. Можете нажимать старт.";
                    this.buttonStart.IsEnabled = true;
                } else {
                    labelStatus.Content = "Вы зашли в игру. Ждите старта хоста.";
                    buttonStart.Visibility = Visibility.Hidden;
                }
                this.Title = Protocol.GetCommunicator().GameName;
                return 0;
            }));
        }

        private void onProtocolOnOnMoveReceived(object sender1, MoveReceivedEventArgs receivedEventArgs) {
            Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                gameWindow.mapControl1.AddPoint(receivedEventArgs.X, receivedEventArgs.Y, Protocol.GetCommunicator().IsOwner ? PointType.SecondPlayer : PointType.FirstPlayer);
                return 0;
            }));
        }

        private void onProtocolOnOnGameStarted(object o, EventArgs eventArgs) {
            Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                gameWindow = new GameWindow(Protocol);
                gameWindow.Closed += (oo, args) => {
                    this.Close();
                };
                this.Hide();
                gameWindow.ShowDialog();
                return 0;
            }));
        }

        private void onProtocolOnOnMessageReceived(object sender, MessageReceivedEventArgs args) {
            Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                appendMessageToChat(Protocol.GetCommunicator().IsOwner ? Protocol.GetCommunicator().SecondPlayerName : Protocol.GetCommunicator().FirstPlayerName, args.Message);
                return 0;
            }));
        }

        public IGameProtocol Protocol {
            get;
            set;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Protocol.GetCommunicator().LeaveGame();
            //
            Protocol.OnMessageReceived -= onProtocolOnOnMessageReceived;
            Protocol.OnGameStarted -= onProtocolOnOnGameStarted;
            Protocol.OnMoveReceived -= onProtocolOnOnMoveReceived;
            Protocol.OnConnected -= onProtocolOnOnConnected;
            Protocol.OnDisconnected -= onProtocolOnOnDisconnected;
            Protocol.GetCommunicator().OnPlayerNameReceived -= onOnPlayerNameReceived;
        }

        private void appendMessageToChat(string playerName, string message) {
            string displayedMessage = string.Format("{0}: {1}", playerName, message);
            if (string.IsNullOrEmpty(textBlockChat.Text)) {
                textBlockChat.Text = displayedMessage;
            } else {
                textBlockChat.Text = textBlockChat.Text + "\r\n" + displayedMessage;
            }
            textBoxChat.Text = "";

            if (scc.ExtentHeight - scc.VerticalOffset == scc.Height) {
                scc.ScrollToBottom();
            }
        }

        private void textBoxChat_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                Protocol.sendMessage(textBoxChat.Text);
                appendMessageToChat(Protocol.GetCommunicator().IsOwner
                                        ? Protocol.GetCommunicator().FirstPlayerName
                                        : Protocol.GetCommunicator().SecondPlayerName,
                                    textBoxChat.Text);
            }
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e) {
            Protocol.sendStartSignal();
            gameWindow = new GameWindow(Protocol);
            gameWindow.Closed += (o, args) => {
                this.Close();
            };
            this.Hide();
            gameWindow.ShowDialog();
        }
    }
}