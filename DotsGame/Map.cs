using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DotsGame {
    public enum PointType {
        Empty,
        FirstPlayer,
        SecondPlayer
    }

    public struct Lines {
        public bool ConnectedRight;

        public bool ConnectedDownRight;

        public bool ConnectedDown;

        public bool ConnectedDownLeft;
    }

    public struct Point {
        public PointType PointType;

        public bool IsBlocked;

        public Lines Lines, OldLines;
    }

    public sealed class Map {
        public int ScoresFirstPlayer {
            get {
                int res = 0;
                for (int x = 0; x < width; ++x) {
                    for (int y = 0; y < height; ++y) {
                        if (points[x, y].PointType == PointType.SecondPlayer && points[x, y].IsBlocked) {
                            ++res;
                        }
                    }
                }
                return res;
            }
        }

        public int ScoresSecondPlayer {
            get {
                int res = 0;
                for (int x = 0; x < width; ++x) {
                    for (int y = 0; y < height; ++y) {
                        if (points[x, y].PointType == PointType.FirstPlayer && points[x, y].IsBlocked) {
                            ++res;
                        }
                    }
                }
                return res;
            }
        }

        private const int width = 31;
        private const int height = 25;

        public Point[,] points = new Point[31,25];

        private PointType getPoint(int x, int y) {
            return points[x, y].PointType;
        }

        private void setPoint(int x, int y, PointType type) {
            points[x, y].PointType = type;
        }

        private bool isBlocked(int x, int y) {
            return points[x, y].IsBlocked;
        }

        private void setBlocked(int x, int y, bool blocked) {
            points[x, y].IsBlocked = blocked;
        }

        private Lines GetPointConnections(int x, int y) {
            return points[x, y].Lines;
        }

        private void setPointConnections(int x, int y, Lines lines) {
            points[x, y].Lines = lines;
        }

        private void ConnectPoints(int x1, int y1, int x2, int y2) {
            bool point;
            if (x2 >= x1) {
                if (y2 >= y1) {
                    point = false; // По 1ой точке
                } else {
                    point = true; // По 2ой точке
                }
            } else {
                if (y2 > y1) {
                    point = false;
                } else {
                    point = true;
                }
            }

            if (point) {
                x1 = x1 + x2; // Меняем координаты точек при необходимости
                x2 = x1 - x2;
                x1 = x1 - x2;
                y1 = y1 + y2;
                y2 = y1 - y2;
                y1 = y1 - y2;
            }

            if (x2 < x1)
                points[x1, y1].Lines.ConnectedDownLeft = true;
            if (x1 == x2)
                points[x1, y1].Lines.ConnectedDown = true;
            if (x2 > x1)
                if (y1 == y2)
                    points[x1, y1].Lines.ConnectedRight = true;
                else
                    points[x1, y1].Lines.ConnectedDownRight = true;
        }


        private int GetNextX(int x, int counter) {
            switch (counter) {
                case 1:
                case 2:
                case 3: {
                    x++;
                    break;
                }
                case 5:
                case 6:
                case 7: {
                    x--;
                    break;
                }
            }
            return x;
        }

        private int GetNextY(int y, int counter) {
            switch (counter) {
                case 7:
                case 0:
                case 1: {
                    y--;
                    break;
                }
                case 3:
                case 4:
                case 5: {
                    y++;
                    break;
                }
            }
            return y;
        }

        public bool AddPoint(int x, int y, PointType pl) {
            if ((getPoint(x, y) != PointType.Empty) || (isBlocked(x, y))) {
                return false;
            }
            setPoint(x, y, pl);
            searchBlocked(true);
            searchNewZones(x, y);
            return true;
        }

        public Point GetPointInfo(int x, int y) {
            return points[x, y];
        }

        private int findZone_j = 0;

        // Рекурсивная функция поиска циклов в неориентированном графе
        private void findZone(int x, int y, bool[,] already,
                              int[] sp) {
            //static int findZone_j = 0;   // Индекс последнего элемента списка
            for (int z = 0; (z + 4) < findZone_j; z += 2) // Ищем окружение (цикл в графе)
                if ((sp[z] == x) && (sp[z + 1] == y)) {
                    // Нашли !
                    for (int k = z; (k + 2) < findZone_j; k += 2) // Соединяем точки из списка
                        ConnectPoints(sp[k], sp[k + 1], sp[k + 2], sp[k + 3]);
                    // Соединяем последнюю точку окружения с первой
                    ConnectPoints(sp[findZone_j - 2], sp[findZone_j - 1], x, y);
                    return; // Возвращаемся в родительскую функцию
                }
            sp[findZone_j++] = x;
            sp[findZone_j++] = y; // Помещаем координаты обрабатываемой точки в список
            for (int k = 0; k < 8; k++) {
                // Для каждой точки, смежной с данной
                int newX = GetNextX(x, k);
                int newY = GetNextY(y, k);
                // Проверка на валидность координат
                if (((newX >= 0) && (newX < width)) && ((newY >= 0) && (newY < height))) {
                    if (findZone_j >= 4) // Чтобы не попадать на предыдущую точку
                        if ((newX == sp[findZone_j - 4]) && (newY == sp[findZone_j - 3]))
                            continue;
                    // Точка должна быть незаблокированной (неокружённой),
                    // должна быть того же цвета, плюс она не должна быть уже проверенной
                    if (!isBlocked(newX, newY))
                        if (getPoint(newX, newY) == getPoint(x, y))
                            if (!already[newX, newY])
                                findZone(newX, newY, already, sp);
                }
            }
            findZone_j -= 2; // Убираем свои координаты из списка
            already[x, y] = true;
        }

        private void searchZones() {
            bool[,] already = new bool[width,height];
            int[] sp = new int[width*height*2 + 2]; // Чтобы всем хватило
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if ((getPoint(x, y) != PointType.Empty) &&
                        (!already[x, y]) && (!isBlocked(x, y)))
                        findZone(x, y, already, sp);
        }

        private void searchNewZones(int x, int y) {
            bool[,] already = new bool[width,height];
            int[] sp = new int[width*height*2 + 2]; // Чтобы всем хватило
            findZone(x, y, already, sp);
        }

        private int findBlocked_j = 0;
        private bool findBlocked_blocked = false;

        // Используется вариация на тему поиска в глубину
        private void findBlocked(int x, int y, PointType color,
                                 bool[,] already, bool[,] visited, int[] sp) {
            //static int findBlocked_j = 0;
            //static bool findBlocked_blocked;
            if (findBlocked_j == 0) findBlocked_blocked = true;
            sp[findBlocked_j++] = x;
            sp[findBlocked_j++] = y;
            if ((x == 0) || (y == 0) || (x == (width - 1)) || (y == (height - 1))) {
                for (int z = 0; z < findBlocked_j; z += 2)
                    if (getPoint(sp[z], sp[z + 1]) == color)
                        if (!already[sp[z], sp[z + 1]]) // Чтобы точки не "освобождались"
                            setBlocked(sp[z], sp[z + 1], false);
                // Если map[sp[0], sp[1]] == EMPTY, это нужно
                setBlocked(sp[0], sp[1], false);
                findBlocked_blocked = false;
            }
            if (findBlocked_blocked) {
                visited[x, y] = true;
                for (int k = 0; k < 7; k += 2) {
                    if (!findBlocked_blocked)
                        break;
                    int newX = GetNextX(x, k);
                    int newY = GetNextY(y, k);
                    if (!visited[newX, newY])
                        if (getPoint(newX, newY) == color)
                            findBlocked(newX, newY, color, already, visited, sp);
                        else if (getPoint(newX, newY) == PointType.Empty)
                            findBlocked(newX, newY, color, already, visited, sp);
                        else if (isBlocked(newX, newY) && (already[newX, newY]))
                            findBlocked(newX, newY, color, already, visited, sp);
                }
            }
            findBlocked_j -= 2;
        }

        private void searchBlocked(bool verifyEmpties)
            // При verifyEmties = false функция работает в среднем в ~50 раз быстрее (!)
        {
            bool[,] already = new bool[width,height];
            bool[,] visited = new bool[width,height];
            int[] sp = new int[width*height*2 + 2];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (isBlocked(x, y))
                        already[x, y] = true;
                    else if ((verifyEmpties) || (getPoint(x, y) != PointType.Empty))
                        setBlocked(x, y, true);
            // Проверка непустых ячеек
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    if (!already[x, y])
                        if (getPoint(x, y) != PointType.Empty) {
                            fillVisitedMatrix(visited, false); // Для поиска в глубину
                            findBlocked(x, y, getPoint(x, y), already, visited, sp);
                        }
            // Проверка пустых клеток (если нужно)
            if (verifyEmpties)
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        if (!already[x, y])
                            if (getPoint(x, y) == PointType.Empty) {
                                fillVisitedMatrix(visited, false);
                                findBlocked(x, y, PointType.FirstPlayer, already, visited, sp);
                                if (!isBlocked(x, y)) {
                                    setBlocked(x, y, true);
                                    fillVisitedMatrix(visited, false);
                                    findBlocked(x, y, PointType.SecondPlayer, already, visited, sp);
                                }
                                already[x, y] = true;
                            }
        }

        private void fillVisitedMatrix(bool[,] matrix, bool value) {
            for (int i = 0; i < width; i++) {
                for (int j = 0; j < height; j++) {
                    matrix[i, j] = value;
                }
            }
        }
    }
}