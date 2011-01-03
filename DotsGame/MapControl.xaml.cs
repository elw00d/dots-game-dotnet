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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DotsGame {
    /// <summary>
    /// Interaction logic for MapControl.xaml
    /// </summary>
    public partial class MapControl : UserControl {
        public MapControl() {
            InitializeComponent();
            //
            Map = new Map();
        }

        public event MoveReceivedEventHandler OnMoveReceived;

        public Map Map {
            get;
            set;
        }

        public PointType CurrentPlayer
        {
            get;
            set;
        }

        public bool isCurrentPlayerMove = true;

        public bool AddPoint(int x, int y, PointType pointType) {
            if (Map.AddPoint(x, y, pointType))
            {
                isCurrentPlayerMove = !isCurrentPlayerMove;
                MoveReceivedEventArgs args = new MoveReceivedEventArgs(x, y);
                if (OnMoveReceived != null)
                {
                    OnMoveReceived.Invoke(this, args);
                }
                this.InvalidateVisual();
                return true;
            }
            return false;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e) {
            System.Windows.Point position = e.GetPosition(this);
            int X = (int) position.X;
            int Y = (int) position.Y;
            bool yes = true;
            int modX = (X - 8 + 16)%16;
            int modY = (Y - 8 + 16)%16;
            int leftX = (X - 8 + 16)/16;
            int leftY = (Y - 8 + 16)/16;
            if (modX > 11) leftX++;
            if (modY > 11) leftY++;
            if ((modY <= 11) && (modY >= 4)) yes = false;
            if ((modX <= 11) && (modX >= 4)) yes = false;
            if (yes && isCurrentPlayerMove) {
                this.AddPoint(leftX - 1, leftY - 1, CurrentPlayer);
                //if (Map.AddPoint(leftX - 1, leftY - 1, CurrentPlayer))
                //{
                //    MoveReceivedEventArgs args = new MoveReceivedEventArgs(leftX - 1, leftY - 1);
                //    if (OnMoveReceived != null) {
                //        OnMoveReceived.Invoke(this, args);
                //    }
                //    isCurrentPlayerMove = !isCurrentPlayerMove;
                //}
                //this.InvalidateVisual();
            }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            // Create a rectangle and draw it in the DrawingContext.
            Rect rect = new Rect(new System.Windows.Point(0, 0), new Size(this.ActualWidth, this.ActualHeight));
            drawingContext.DrawRectangle(Brushes.LightBlue, null, rect);
            //
            // Сначала рисуем сетку на игровом поле
            Pen pen1 = new Pen(Brushes.Black, 1.0);
            pen1.DashStyle = DashStyles.Dot;
            for (int x = 0; x < 31; x++) {
                if (x*16 + 8 <= this.ActualWidth) {
                    drawingContext.DrawLine(pen1, new System.Windows.Point(x*16 + 8, 0),
                                            new System.Windows.Point(x*16 + 8, this.ActualHeight));
                }
            }

            for (int y = 0; y < 25; y++) {
                if (y*16 + 8 <= this.ActualHeight) {
                    drawingContext.DrawLine(pen1, new System.Windows.Point(0, y*16 + 8),
                                            new System.Windows.Point(this.ActualWidth, y*16 + 8));
                }
            }

            // Прорисовка зон
            Pen pen = new Pen(Brushes.Black, 1.0);
            pen.DashStyle = DashStyles.Solid;

            for (int x = 0; x < 31; x++) {
                for (int y = 0; y < 25; y++) {
                    Point info = Map.GetPointInfo(x, y);
                    pen.Thickness = info.IsBlocked ? 1 : 2;
                    int X = 8 + 16*x;
                    int Y = 8 + 16*y;
                    //
                    System.Windows.Point point1 = new System.Windows.Point(X, Y);
                    if (info.Lines.ConnectedRight) {
                        drawingContext.DrawLine(pen, point1, new System.Windows.Point(X + 16, Y));
                    }
                    if (info.Lines.ConnectedDown) {
                        drawingContext.DrawLine(pen, point1, new System.Windows.Point(X, Y + 16));
                    }
                    if (info.Lines.ConnectedDownRight) {
                        drawingContext.DrawLine(pen, point1, new System.Windows.Point(X + 16, Y + 16));
                    }
                    if (info.Lines.ConnectedDownLeft) {
                        drawingContext.DrawLine(pen, point1, new System.Windows.Point(X - 16, Y + 16));
                    }
                }
            }

            // Прорисовка точек
            for (int x = 0; x < 31; x++) {
                for (int y = 0; y < 25; y++) {
                    Point info = Map.GetPointInfo(x, y);
                    int X = 8 + 16*x;
                    int Y = 8 + 16*y;
                    pen.Brush = Brushes.Black;
                    if (info.IsBlocked) {
                        pen.Brush = Brushes.GreenYellow;
                    }
                    pen.Thickness = info.IsBlocked ? 1 : 2;
                    if (info.PointType == PointType.FirstPlayer) {
                        pen.Brush = Brushes.Red;
                        drawingContext.DrawEllipse(pen.Brush, pen, new System.Windows.Point(X, Y), 3, 3);
                    }
                    if (info.PointType == PointType.SecondPlayer) {
                        pen.Brush = Brushes.Blue;
                        drawingContext.DrawEllipse(pen.Brush, pen, new System.Windows.Point(X, Y), 3, 3);
                    }
                    if ((info.PointType == PointType.Empty) && (info.IsBlocked)) {
                        //
                    }
                }
            }
        }
    }
}