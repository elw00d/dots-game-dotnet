using System;
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
            Protocol.OnMessageReceived += (sender, args) => {
                Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                    appendMessageToChat(Protocol.GetCommunicator().IsOwner
                                        ? Protocol.GetCommunicator().SecondPlayerName
                                        : Protocol.GetCommunicator().FirstPlayerName,
                                    args.Message);
                    return 0;
                }));
            };
            Protocol.OnGameStarted += (o, eventArgs) => {
                Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                    gameWindow = new GameWindow(Protocol);
                    gameWindow.Closed += (oo, args) => {
                        this.Show();
                    };
                    this.Hide();
                    gameWindow.ShowDialog();
                    return 0;
                }));
            };
            Protocol.OnMoveReceived += (sender1, receivedEventArgs) => {
                Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                    gameWindow.mapControl1.AddPoint(receivedEventArgs.X, receivedEventArgs.Y,
                                                    Protocol.GetCommunicator().IsOwner
                                                        ? PointType.SecondPlayer
                                                        : PointType.FirstPlayer);
                    return 0;
                }));
            };
            Protocol.OnConnected += (sender2, eventArgs1) => {
                Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                    if (Protocol.GetCommunicator().IsOwner) {
                        labelStatus.Content = "Игрок зашел в игру. Можете нажимать старт.";
                    } else {
                        labelStatus.Content = "Вы зашли в игру. Ждите старта хоста.";
                        buttonStart.Visibility = Visibility.Hidden;
                    }
                    this.Title = Protocol.GetCommunicator().GameName;
                    return 0;
                }));
            };
            Protocol.OnDisconnected += (o1, args1) => {
                Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                    if (Protocol.GetCommunicator().IsOwner) {
                        labelStatus.Content = "Ожидание подключения игрока...";
                    } else {
                        labelStatus.Content = "Ожидание подключения к хосту...";
                        buttonStart.Visibility = Visibility.Hidden;
                    }
                    return 0;
                }));
            };
            Protocol.GetCommunicator().OnPlayerNameReceived += (o2, args2) => {
                Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                    //
                    ICommunicator _communicator = (ICommunicator) o2;
                    this.labelFirstPlayerName.Content = _communicator.FirstPlayerName;
                    this.labelSecondPlayerName.Content = _communicator.SecondPlayerName;
                    return 0;
                    //
                }));
            };
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
        }

        public IGameProtocol Protocol {
            get;
            set;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Protocol.GetCommunicator().LeaveGame();
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
                this.Show();
            };
            this.Hide();
            gameWindow.ShowDialog();
        }
    }
}