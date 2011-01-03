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
        public GameWindow(IGameProtocol gameProtocol) {
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
        }
    }
}