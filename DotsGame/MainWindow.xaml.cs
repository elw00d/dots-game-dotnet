using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DotsGame {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private const string NICKNAME_PROPERTY = "nickname";
        private HostedGameWindow hostedGameWindow;

        public MainWindow() {
            InitializeComponent();
            this.textboxNickname.Text = (string) Properties.Settings.Default[NICKNAME_PROPERTY];
        }

        private bool isNicknameValid() {
            String nickname = textboxNickname.Text;
            if (string.IsNullOrEmpty(nickname) || nickname.Trim().Length == 0) {
                MessageBox.Show("Для игры необходимо ввести своё имя.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            return true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            GameClient.Instance.OnGameObserved += (o, args) => {
                Application.Current.Dispatcher.Invoke(new Func<int>(() => {
                    foreach (object item in this.listViewGames.Items) {
                        if (item is GameInfo) {
                            GameInfo game = item as GameInfo;
                            if (game.OwnerIP.Equals(args.Game.OwnerIP)) {
                                game.Name = args.Game.Name;
                                game.PlayersConnectedCount = args.Game.PlayersConnectedCount;
                                return 0;
                            }
                        }
                    }
                    // Показываем только те игры, которые не созданы самим собой
                    if (args.Game.UniqueHostId != GameServer.Instance.UniqueId) {
                        this.listViewGames.Items.Add(args.Game);
                    }
                    return 0;
                }));
            };
            GameClient.Instance.StartClient();
        }

        private void buttonCreate_Click(object sender, RoutedEventArgs e) {
            if (!isNicknameValid()) {
                return;
            }
            //
            string name = textboxGameName.Text;
            if (string.IsNullOrEmpty(name)) {
                name = "Игра";
            }
            GameServer.Instance.CreateGame(name, textboxNickname.Text);
            this.Hide();
            hostedGameWindow = new HostedGameWindow(new GameProtocol(GameServer.Instance));
            hostedGameWindow.Closed += (o, args) => {
                this.Show();
            };
            hostedGameWindow.ShowDialog();
        }

        private void Window_Closed(object sender, EventArgs e) {
            GameClient.Instance.StopClient();
        }

        private void buttonJoin_Click(object sender, RoutedEventArgs e) {
            if (null != this.listViewGames.SelectedItem) {
                if (!isNicknameValid()) {
                    return;
                }
                //
                GameInfo gameInfoSelected = (GameInfo) this.listViewGames.SelectedItem;
                hostedGameWindow = new HostedGameWindow(new GameProtocol(GameClient.Instance));
                try {
                    GameClient.Instance.ConnectToGame(gameInfoSelected, textboxNickname.Text);
                    hostedGameWindow.Closed += (o, args) => {
                        this.Show();
                    };
                    this.Hide();
                    hostedGameWindow.ShowDialog();
                    //
                } catch (SocketException) {
                    MessageBox.Show("Не удалось зайти в игру.");
                    hostedGameWindow.Close();
                    //
                    refreshGamesList();
                }
            }
        }

        private void buttonExit_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void buttonRefresh_Click(object sender, RoutedEventArgs e) {
            refreshGamesList();
        }

        private void refreshGamesList() {
            this.listViewGames.Items.Clear();
        }

        private void textboxNickname_TextChanged(object sender, TextChangedEventArgs e) {
            Properties.Settings.Default[NICKNAME_PROPERTY] = ((TextBox) sender).Text;
        }

        private void textboxNickname_LostFocus(object sender, RoutedEventArgs e) {
            Properties.Settings.Default.Save();
        }
    }
}