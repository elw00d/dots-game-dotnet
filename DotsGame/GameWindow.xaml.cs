using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window {

        private IGameProtocol protocol;

        public GameWindow(IGameProtocol gameProtocol) {
            protocol = gameProtocol;
            InitializeComponent();
            //
            bool isOwner = gameProtocol.GetCommunicator().IsOwner;
            mapControl1.CurrentPlayer = isOwner ? PointType.FirstPlayer : PointType.SecondPlayer;
            mapControl1.isCurrentPlayerMove = isOwner;
            //
            this.mapControl1.Map = new Map();
            this.mapControl1.OnMoveReceived += (sender, args) => {
                gameProtocol.sendMove(args.X, args.Y);
                this.labelMove.Content = mapControl1.isCurrentPlayerMove ? "Сейчас Ваш ход" : "Ход противника..";
                this.labelMyPoints.Content = isOwner
                                                 ? mapControl1.Map.ScoresFirstPlayer
                                                 : mapControl1.Map.ScoresSecondPlayer;
                this.labelEnemyPoints.Content = isOwner
                                                    ? mapControl1.Map.ScoresSecondPlayer
                                                    : mapControl1.Map.ScoresFirstPlayer;
            };

            this.Title = isOwner ? "Красные начинают.. и выигрывают" : "Я подключен к игре.";
            this.labelMove.Content = mapControl1.isCurrentPlayerMove ? "Сейчас Ваш ход" : "Ход противника..";
            //
            protocol.OnLeave += onProtocolOnOnLeave;
        }

        private void onProtocolOnOnLeave(object o, EventArgs eventArgs) {
            Application.Current.Dispatcher.BeginInvoke(new Func<int>(() => {
                MessageBox.Show("Ваш противник покинул игру.", "Сообщение", MessageBoxButton.OK,
                                MessageBoxImage.Information);
                this.Close();
                return 0;
            }));
        }

        private void buttonLeaveGame_Click(object sender, RoutedEventArgs e)
        {
            this.protocol.GetCommunicator().LeaveGame();
            this.Close();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            protocol.OnLeave -= onProtocolOnOnLeave;
        }
    }
}