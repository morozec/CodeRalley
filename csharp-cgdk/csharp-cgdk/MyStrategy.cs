using System;
using System.Collections.Generic;
using System.Diagnostics;
using Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk.AStar;
using Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk.Model;
using IPA.AStar;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk
{
    public sealed class MyStrategy : IStrategy
    {
        bool _isFirstHardCodeDone = false;
        bool _isSecondCodeDone = false;
        bool _isThirdCodeDone = false;
        bool _isForthCodeDone = false;
        const double eps = 1E-6;

        private Square _lastSquare = null;
        private Square _currentStartSquare = null;

        //static MyStrategy()
        //{
        //    Debug.connect("localhost", 13579);
        //}

        private enum NextTilPosition
        {
            Horizontal,
            Vertical
        }

        private enum MovingType
        {
            Horizontal,
            Vertical
        }

        private enum NextTileType
        {
            LastHorizontal,
            LastVertical,
            NotLastHorizontal,
            NotLastVertical
        }

        private enum TurnType
        {
            DownLeft,
            DownRight,
            UpLeft,
            UpRight,
            LeftUp,
            LeftDown,
            RightUp,
            RightDown
        }

        public void Move(Car self, World world, Game game, Move move)
        {
            //Debug.beginPost();

            
            #region Константы

            const double g = 9.81;
            var mu = 0.025;
            const double maxAngularSpeed = 10d;
            const double angleToGoBack = Math.PI/4;
            var angleMultiplier = 5d;
            var borderDistToGoBack = self.Width/2 + 150;
            var carDistToGoBack = 350;

            #endregion

            #region Костыль, чтоб не падало из-за TileType.Unknown

            for (int i = 0; i < world.Width; ++i)
            {
                for (int j = 0; j < world.Height; ++j)
                {
                    var type = world.TilesXY[i][j];
                    if (type == TileType.Unknown)
                    {
                        move.EnginePower = 1d;
                        return;
                    }
                }
            }

            #endregion

            var selfWayPointX = GetWayPointCoord(self.X, game);
            var selfWayPointY = GetWayPointCoord(self.Y, game);

            #region A*

            Square startSquare = null;
            var table = new Square[world.Width, world.Height];
            var squares = new List<Square>();

            for (int i = 0; i < world.Width; ++i)
            {
                for (int j = 0; j < world.Height; ++j)
                {
                    var type = world.TilesXY[i][j];
                    double weight;
                    if (type == TileType.Unknown)
                    {
                        weight = 9999999;
                    }
                    else if (type == TileType.Horizontal || type == TileType.Vertical)
                    {
                        weight = 1d;
                    }
                    else
                    {
                        weight = 2d;
                    }

                    if (world.MapName.Equals("map14") && i == 10 && j == 8 && self.NextWaypointIndex == 4)
                    {
                        weight = 1000;
                    }

                    if (world.MapName.Equals("map14") && i == 4 && j == 4 && self.NextWaypointIndex == 9)
                    {
                        weight = 1000;
                    }
                   
                    var square = new Square(
                        game.TrackTileSize,
                        game.TrackTileSize*i,
                        game.TrackTileSize*j,
                        type,
                        weight,
                        i + ":" + j);
                    squares.Add(square);

                    if (i == selfWayPointX && j == selfWayPointY)
                    {
                        startSquare = square;
                    }

                    table[i, j] = square;
                }
            }


            for (int i = 0; i < world.Width; ++i)
            {
                for (int j = 0; j < world.Height; ++j)
                {
                    var thisTileType = table[i, j].Type;
                    TileType neighborTileType;
                    var neighbors = new List<Square>();
                    if (i != 0)
                    {
                        neighborTileType = table[i - 1, j].Type;
                        if (thisTileType != TileType.Empty && thisTileType != TileType.Vertical &&
                            thisTileType != TileType.LeftBottomCorner && thisTileType != TileType.LeftTopCorner &&
                            thisTileType != TileType.RightHeadedT &&
                            neighborTileType != TileType.Empty && neighborTileType != TileType.Vertical &&
                            neighborTileType != TileType.RightBottomCorner && neighborTileType != TileType.RightTopCorner &&
                            neighborTileType != TileType.LeftHeadedT)
                        {
                            neighbors.Add(table[i - 1, j]);
                        }
                    }
                    if (i != world.Width - 1)
                    {
                        neighborTileType = table[i + 1, j].Type;
                        if (thisTileType != TileType.Empty && thisTileType != TileType.Vertical &&
                            thisTileType != TileType.RightBottomCorner && thisTileType != TileType.RightTopCorner &&
                            thisTileType != TileType.LeftHeadedT &&
                            neighborTileType != TileType.Empty && neighborTileType != TileType.Vertical &&
                            neighborTileType != TileType.LeftBottomCorner && neighborTileType != TileType.LeftTopCorner &&
                            neighborTileType != TileType.RightHeadedT)
                        {
                            neighbors.Add(table[i + 1, j]);
                        }
                    }
                    if (j != 0)
                    {
                        neighborTileType = table[i, j - 1].Type;

                        if (thisTileType != TileType.Empty && thisTileType != TileType.Horizontal &&
                            thisTileType != TileType.LeftTopCorner && thisTileType != TileType.RightTopCorner &&
                            thisTileType != TileType.BottomHeadedT &&
                            neighborTileType != TileType.Empty && neighborTileType != TileType.Horizontal &&
                            neighborTileType != TileType.LeftBottomCorner && neighborTileType != TileType.RightBottomCorner &&
                            neighborTileType != TileType.TopHeadedT)
                        {
                            neighbors.Add(table[i, j - 1]);
                        }
                    }
                    if (j != world.Height - 1)
                    {
                        neighborTileType = table[i, j + 1].Type;

                        if (thisTileType != TileType.Empty && thisTileType != TileType.Horizontal &&
                            thisTileType != TileType.LeftBottomCorner && thisTileType != TileType.RightBottomCorner &&
                            thisTileType != TileType.TopHeadedT &&
                            neighborTileType != TileType.Empty && neighborTileType != TileType.Horizontal &&
                            neighborTileType != TileType.LeftTopCorner && neighborTileType != TileType.RightTopCorner &&
                            neighborTileType != TileType.BottomHeadedT)
                        {
                            neighbors.Add(table[i, j + 1]);
                        }
                    }

                    table[i, j].Neighbors = neighbors;
                }
            }

            #endregion
        

            #region определение координат цели

            var selfSquare = table[selfWayPointX, selfWayPointY];

        
            //foreach (var neighbour in selfSquare.Neighbors)
            //{
            //    var tempCar = new Car(
            //        0,
            //        0,
            //        self.X,
            //        self.Y,
            //        0,
            //        0,
            //        self.Angle,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        false,
            //        CarType.Buggy,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        0,
            //        false);

            //    var angle = tempCar.GetAngleTo(neighbour.X + game.TrackTileSize / 2, neighbour.Y + game.TrackTileSize / 2);
            //    double startSquareAngle = 0;
            //    if (neighbour.X == startSquare.X && neighbour.Y < startSquare.Y)
            //    {
            //        startSquareAngle = -Math.PI / 2;
            //    }
            //    else if (neighbour.X == startSquare.X && neighbour.Y > startSquare.Y)
            //    {
            //        startSquareAngle = Math.PI / 2;
            //    }
            //    else if (neighbour.Y == startSquare.Y && neighbour.X < startSquare.X)
            //    {
            //        startSquareAngle = -Math.PI;
            //    }
            //    else if (neighbour.Y == startSquare.Y && neighbour.X > startSquare.X)
            //    {
            //        startSquareAngle = 0d;
            //    }


            //    neighbour.Angles.Add(startSquare, startSquareAngle);

            //    if (Math.Abs(angle) > Math.PI / 2 + eps)
            //    {
            //        neighbour.AdditionalAngleCoeffs.Add(startSquare, 1000d);
            //    }               
            //    else 
            //    {
            //        neighbour.AdditionalAngleCoeffs.Add(startSquare, 0d);
            //    }              
              
                
            //}

            //foreach (var neighbour in selfSquare.Neighbors)
            //{
            //    SetAngleCost(neighbour, startSquare);
            //}

            if (world.MapName.Equals("map20") && self.NextWaypointIndex == 2 && Equals(selfSquare, table[6, 6]))
            {
                _isFirstHardCodeDone = true;
                _isSecondCodeDone = false;
            }

            if (world.MapName.Equals("map20") && self.NextWaypointIndex == 3 && Equals(selfSquare, table[14, 0]))
            {
                _isSecondCodeDone = true;
                _isThirdCodeDone = false;              
            }

            if (world.MapName.Equals("map20") && self.NextWaypointIndex == 4 && Equals(selfSquare, table[8, 8]))
            {
                _isThirdCodeDone = true;
                _isForthCodeDone = false;
            }

            if (world.MapName.Equals("map20") && self.NextWaypointIndex == 0 && Equals(selfSquare, table[0, 14]))
            {
                _isForthCodeDone = true;
                _isFirstHardCodeDone = false;
            }

            if (world.MapName.Equals("_tyamgin") && self.NextWaypointIndex == 0 && Equals(selfSquare, table[4, 0]))
            {               
                _isFirstHardCodeDone = true;
            }
            if (world.MapName.Equals("_tyamgin") && self.NextWaypointIndex == 1)
            {
                _isFirstHardCodeDone = false;
            }

            if (world.MapName.Equals("map18") && self.NextWaypointIndex == 1 && Equals(selfSquare, table[13, 6]))
            {
                _isFirstHardCodeDone = true;
            }
            if (world.MapName.Equals("map18") && self.NextWaypointIndex == 2)
            {
                _isFirstHardCodeDone = false;
            }

            //if (world.MapName.Equals("map18") && self.NextWaypointIndex == 0 && Equals(selfSquare, table[7, 8]))
            //{                
            //    _isFirstHardCodeDone = true;
            //}

            Point resPoint;
            if (world.MapName.Equals("map20") && !_isFirstHardCodeDone)
            {
                resPoint = table[6, 6];                
            }
            else if (world.MapName.Equals("map20") && !_isSecondCodeDone)
            {
                resPoint = table[14, 0];
            }
            else if (world.MapName.Equals("map20") && !_isThirdCodeDone)
            {
                resPoint = table[8, 8];
            }
            else if (world.MapName.Equals("map20") && !_isForthCodeDone)
            {
                resPoint = table[0, 14];
            }
            else if (world.MapName.Equals("_tyamgin") && (self.NextWaypointIndex == 6 || self.NextWaypointIndex == 0) && !_isFirstHardCodeDone)
            {
                resPoint = table[4, 0];
            }
            else if (world.MapName.Equals("map18") && self.NextWaypointIndex == 1 && !_isFirstHardCodeDone)
            {
                resPoint = table[13, 6];
            }

            else
            {
                resPoint = table[self.NextWaypointX, self.NextWaypointY];
            }
            var path = Calculator.GetPath(startSquare, resPoint, squares, self, game);

            //for (int i = 0; i < world.Width; ++i)
            //{
            //    for (int j = 0; j < world.Height; ++j)
            //    {
            //        var square = table[i, j] as Square;
            //        var resStr = (square as Square).X + " " + (square as Square).Y;
            //        Debug.print(
            //                  i * game.TrackTileSize + game.TrackTileSize / 2,
            //                  j * game.TrackTileSize,
            //                  resStr, 0xFF0000);

            //        var counter = 1;
            //        foreach (var key in square.AdditionalAngleCoeffs.Keys)
            //        {
            //            resStr = (key as Square).X + " " + (key as Square).Y + ":" + square.Angles[key] + " / " + square.AdditionalAngleCoeffs[key];

            //              Debug.print(
            //                  i * game.TrackTileSize + game.TrackTileSize / 2 ,
            //                  j * game.TrackTileSize + counter * 100,
            //                  resStr);

            //            counter++;
            //        }
            //    }

            //}


        

            var lastSquare = path.Last() as Square;

            for (int i = self.NextWaypointIndex + 1; i < world.Waypoints.Length; ++i)
            {
                //var angle = (lastSquare as Square).Angles[path[path.Count - 2] as Square];

                //for (int k = 0; k < world.Width; ++k)
                //{
                //    for (int j = 0; j < world.Height; ++j)
                //    {
                //        (table[k, j] as Square).Angles.Clear();
                //        (table[k, j] as Square).AdditionalAngleCoeffs.Clear();
                //    }
                //}

                //foreach (var neighbour in (lastSquare as Square).Neighbors)
                //{                    
                //    double startSquareAngle = 0;
                //    if (neighbour.X == lastSquare.X && neighbour.Y < lastSquare.Y)
                //    {
                //        startSquareAngle = -Math.PI / 2;
                //    }
                //    else if (neighbour.X == lastSquare.X && neighbour.Y > lastSquare.Y)
                //    {
                //        startSquareAngle = Math.PI / 2;
                //    }
                //    else if (neighbour.Y == lastSquare.Y && neighbour.X < lastSquare.X)
                //    {
                //        startSquareAngle = -Math.PI;
                //    }
                //    else if (neighbour.Y == lastSquare.Y && neighbour.X > lastSquare.X)
                //    {
                //        startSquareAngle = 0d;
                //    }


                //    neighbour.Angles.Add(lastSquare, startSquareAngle);

                //    if (Math.Abs(startSquareAngle) > Math.PI / 2 + eps)
                //    {
                //        neighbour.AdditionalAngleCoeffs.Add(lastSquare, 1000d);
                //    }
                //    else
                //    {
                //        neighbour.AdditionalAngleCoeffs.Add(lastSquare, 0d);
                //    }
                //}

                //foreach (var neighbour in lastSquare.Neighbors)
                //{
                //    SetAngleCost(neighbour, lastSquare);
                //}


                var waypoint = world.Waypoints[i];
                var waypointX = waypoint[0];
                var waypointY = waypoint[1];
                var tempPath = Calculator.GetPath(lastSquare, table[waypointX, waypointY], squares, self, game);
                for (int j = 1; j < tempPath.Count; ++j)
                {
                    path.Add(tempPath[j]);
                }
                lastSquare = tempPath.Last() as Square;
            }

            for (int i = 0; i < world.Waypoints.Length; ++i)
            {
                var waypoint = world.Waypoints[i];
                var waypointX = waypoint[0];
                var waypointY = waypoint[1];
                var tempPath = Calculator.GetPath(lastSquare, table[waypointX, waypointY], squares, self, game);
                for (int j = 1; j < tempPath.Count; ++j)
                {
                    path.Add(tempPath[j]);
                }
                lastSquare = tempPath.Last() as Square;
            }

            if (self.NextWaypointIndex == world.Waypoints.Length - 1)
            {
                var waypoint = world.Waypoints[0];
                var waypointX = waypoint[0];
                var waypointY = waypoint[1];
                var tempPath = Calculator.GetPath(lastSquare, table[waypointX, waypointY], squares, self, game);
                for (int j = 1; j < tempPath.Count; ++j)
                {
                    path.Add(tempPath[j]);
                }
                lastSquare = tempPath.Last() as Square;
            }

            //var path = Calculator.GetPath(startSquare, table[self.NextWaypointX, self.NextWaypointY], squares, self, game);

            var lastHorizontalSquareIndex = GetLastHorizontalSquareIndex(self, path, game);
            var lastVerticalSquareindex = GetLastVerticalSquareIndex(self, path, game);
            var nextPointNumber = path.Count == 1 ? 0 : 1;
            var afterNextSquare = path.Count < 3 ? null : path[2] as Square;


            var movingType = lastHorizontalSquareIndex == 0 ? MovingType.Vertical : MovingType.Horizontal;

            var lastHorizontalSquare = path[lastHorizontalSquareIndex];
            var lastVerticalSquare = path[lastVerticalSquareindex];

            var lastInLineIndex = movingType == MovingType.Horizontal
                ? path.IndexOf(lastHorizontalSquare)
                : path.IndexOf(lastVerticalSquare);

            var beforeLastInLineSquare = lastInLineIndex > 0 ? path[lastInLineIndex - 1] as Square : null;
            var afterLastInLineSquare = lastInLineIndex < path.Count - 1 ? path[lastInLineIndex + 1] as Square : null;


            NextTileType nextTileType;

            //координата Y не залазеет за выступы (Margin) тайла
            var isCorrectY = (self.Y - game.CarHeight / 2 > (path[nextPointNumber] as Square).Y + game.TrackTileMargin) &&
                (self.Y + game.CarHeight / 2 < (path[nextPointNumber] as Square).Y + game.TrackTileSize - game.TrackTileMargin);
            //координата X не залазеет за выступы (Margin) тайла
            var isCorrectX = (self.X - game.CarHeight / 2 > (path[nextPointNumber] as Square).X + game.TrackTileMargin) &&
                (self.X + game.CarHeight / 2 < (path[nextPointNumber] as Square).X + game.TrackTileSize - game.TrackTileMargin);


            double destX, destY, stopX = 0, stopY = 0;
            bool isDiagonalPath = IsDiagonalPath(path);
            bool isDiagonalExit = IsDiagonalExit(path);
            TurnType turnType;

            var isOtherTurnClose = false;

            var lastlineSquare = movingType == MovingType.Horizontal ? lastHorizontalSquare : lastVerticalSquare;
            var is180Turn = Is180DegreesTurn(path, lastlineSquare);

            if (movingType == MovingType.Horizontal)
            {
                isOtherTurnClose = (path[lastInLineIndex] as Square).X == (path[lastInLineIndex + 1] as Square).X &&
                    (path[lastInLineIndex + 1] as Square).X == (path[lastInLineIndex + 2] as Square).X &&
                    ((path[lastInLineIndex + 3] as Square).X - (path[lastInLineIndex + 2] as Square).X) *
                    ((path[lastInLineIndex] as Square).X - self.X) > 0;


                stopY = (lastHorizontalSquare as Square).Y + game.TrackTileSize / 2;
                if ((lastHorizontalSquare as Square).X > (path[0] as Square).X)//go right
                {
                    stopX = (lastHorizontalSquare as Square).X + game.TrackTileSize - game.TrackTileMargin - self.Height / 2 - 50;
                    if (afterLastInLineSquare.Y > (path[0] as Square).Y) // go down
                    {
                        turnType = TurnType.RightDown;
                        destX = (lastHorizontalSquare as Square).X + game.TrackTileMargin + self.Height / 2;
                        destY = (lastHorizontalSquare as Square).Y + game.TrackTileSize - game.TrackTileMargin - self.Width / 2;
                    }
                    else // go up
                    {
                        turnType = TurnType.RightUp;
                        destX = (lastHorizontalSquare as Square).X + game.TrackTileMargin + self.Height / 2;
                        destY = (lastHorizontalSquare as Square).Y + game.TrackTileMargin + self.Width / 2;
                    }
                }
                else //go left
                {
                    stopX = (lastHorizontalSquare as Square).X + game.TrackTileMargin + self.Height / 2 + 50;
                    if (afterLastInLineSquare.Y > (path[0] as Square).Y) // go down
                    {
                        turnType = TurnType.LeftDown;
                        destX = (lastHorizontalSquare as Square).X + game.TrackTileSize - game.TrackTileMargin - self.Height / 2;
                        destY = (lastHorizontalSquare as Square).Y + game.TrackTileSize - game.TrackTileMargin - self.Width / 2;
                    }
                    else // go up
                    {
                        turnType = TurnType.LeftUp;
                        destX = (lastHorizontalSquare as Square).X + game.TrackTileSize - game.TrackTileMargin - self.Height / 2;
                        destY = (lastHorizontalSquare as Square).Y + game.TrackTileMargin + self.Width / 2;
                    }
                }

                if (isOtherTurnClose)
                {
                    stopX = (lastHorizontalSquare as Square).X + game.TrackTileSize / 2;
                }
            }
            else //vertical
            {
                isOtherTurnClose = (path[lastInLineIndex] as Square).Y == (path[lastInLineIndex + 1] as Square).Y &&
                    (path[lastInLineIndex + 1] as Square).Y == (path[lastInLineIndex + 2] as Square).Y &&
                    ((path[lastInLineIndex + 3] as Square).Y - (path[lastInLineIndex + 2] as Square).Y) *
                    ((path[lastInLineIndex] as Square).Y - self.Y) > 0;

                stopX = (lastVerticalSquare as Square).X + game.TrackTileSize / 2;
                
                if ((lastVerticalSquare as Square).Y > (path[0] as Square).Y)//go down
                {
                    
                    stopY = (lastVerticalSquare as Square).Y + game.TrackTileSize - game.TrackTileMargin - self.Height / 2 - 50;
                   
                    if (afterLastInLineSquare.X > (path[0] as Square).X) // go right
                    {
                        turnType = TurnType.DownRight;
                        destX = (lastVerticalSquare as Square).X + game.TrackTileSize - game.TrackTileMargin - self.Width / 2;
                        destY = (lastVerticalSquare as Square).Y + game.TrackTileMargin + self.Height / 2;
                    }
                    else // go left
                    {
                        turnType = TurnType.DownLeft;
                        destX = (lastVerticalSquare as Square).X + game.TrackTileMargin + self.Width / 2;
                        destY = (lastVerticalSquare as Square).Y + game.TrackTileMargin + self.Height / 2;
                    }
                }
                else //go up
                {
                    stopY = (lastVerticalSquare as Square).Y + game.TrackTileMargin + self.Height / 2 + 50;
                    if (afterLastInLineSquare.X > (path[0] as Square).X) // go right
                    {
                        turnType = TurnType.UpRight;
                        //destX = game.TrackTileSize;
                        //destY = game.TrackTileSize - game.TrackTileMargin - self.Height / 2 - 100;

                        destX = (lastVerticalSquare as Square).X + game.TrackTileSize - game.TrackTileMargin - self.Width / 2;
                        destY = (lastVerticalSquare as Square).Y + game.TrackTileSize - game.TrackTileMargin - self.Height / 2;
                    }
                    else // go left
                    {
                        turnType = TurnType.UpLeft;
                        destX = (lastVerticalSquare as Square).X + game.TrackTileMargin + self.Width / 2;
                        destY = (lastVerticalSquare as Square).Y + game.TrackTileSize - game.TrackTileMargin - self.Height / 2;
                    }
                }

                if (isOtherTurnClose)
                {
                    stopY = (lastVerticalSquare as Square).Y + game.TrackTileSize / 2;
                }
            }

           
            move.EnginePower = 1;

            //Debug.circleFill(
            //    game.TrackTileSize * self.NextWaypointX + game.TrackTileSize / 2,
            //    game.TrackTileSize * self.NextWaypointY + game.TrackTileSize / 2,
            //    100,
            //    0x0000FF);

            var fi = -self.Angle;
            var x = self.X;
            var y = self.Y;
            var vx = self.SpeedX;
            var vy = self.SpeedY;
            var omega = self.AngularSpeed;
            var wheelTurn = self.WheelTurn;


            var isRightTurn = turnType == TurnType.UpRight || turnType == TurnType.LeftUp || turnType == TurnType.DownLeft ||
                              turnType == TurnType.RightDown;

            var turnCoeff = isRightTurn ? -1 : 1;
          

            var canTurn = false;
            var needStop = false;
            var isCollisionDetected = false;

            double diagEndX = 0, diagEndY = 0, maxDiagX = 0, maxDiagY = 0, minDiagX = 0, minDiagY = 0;
            if (isDiagonalPath || isDiagonalExit)
              {
                var diagonalLastSquare = GetLastDiagonalSquare(path);
                //TODO!!! stopX, stopY

                if (turnType == TurnType.UpRight || turnType == TurnType.LeftDown)
                {
                    diagEndX = (lastlineSquare as Square).X + game.TrackTileSize * 3 / 4;
                    diagEndY = (lastlineSquare as Square).Y + game.TrackTileSize * 3 / 4;

                }
                else if (turnType == TurnType.RightUp || turnType == TurnType.DownLeft)
                {
                    diagEndX = (lastlineSquare as Square).X + game.TrackTileSize * 1 / 4;
                    diagEndY = (lastlineSquare as Square).Y + game.TrackTileSize * 1 / 4;

                }
                else if (turnType == TurnType.UpLeft || turnType == TurnType.RightDown)
                {
                    diagEndX = (lastlineSquare as Square).X + game.TrackTileSize * 1 / 4;
                    diagEndY = (lastlineSquare as Square).Y + game.TrackTileSize * 3 / 4;

                }
                else if (turnType == TurnType.DownRight || turnType == TurnType.LeftUp)
                {
                    diagEndX = (lastlineSquare as Square).X + game.TrackTileSize * 3 / 4;
                    diagEndY = (lastlineSquare as Square).Y + game.TrackTileSize * 1 / 4;
                }
                else
                {
                    throw new Exception();
                }

                if (turnType == TurnType.UpRight || turnType == TurnType.DownRight)
                {
                    if (self.X > (lastlineSquare as Square).X + game.TrackTileSize * 3 / 4 && isCorrectX)
                    {
                        diagEndX = self.X;
                    }
                }
                else if (turnType == TurnType.UpLeft || turnType == TurnType.DownLeft)
                {
                    if (self.X < (lastlineSquare as Square).X + game.TrackTileSize * 1 / 4 && isCorrectX)
                    {
                        diagEndX = self.X;
                    }
                }
                else if (turnType == TurnType.RightUp || turnType == TurnType.LeftUp)
                {
                    if (self.Y < (lastlineSquare as Square).Y + game.TrackTileSize * 1 / 4 && isCorrectY)
                    {
                        diagEndY = self.Y;
                    }
                }
                else if (turnType == TurnType.LeftDown || turnType == TurnType.RightDown)
                {
                    if (self.Y > (lastlineSquare as Square).Y + game.TrackTileSize * 3 / 4 && isCorrectY)
                    {
                        diagEndY = self.Y;
                    }
                }
            }

            for (int i = 0; i < 100; ++i)
            {
                x += vx;
                y += vy;

                if (turnType == TurnType.UpRight)
                {
                    if (!isDiagonalPath && !isDiagonalExit && y < stopY)
                    {
                        needStop = true;
                    }
                   
                    
                    if (x > destX && y > destY)
                    {
                        isCollisionDetected = true;
                    }
                    else if (x > destX && y < destY && !isCollisionDetected)
                    {
                        canTurn = true;
                        //break;
                    } 

                }
                else if (turnType == TurnType.UpLeft)
                {
                    if (!isDiagonalPath && !isDiagonalExit && y < stopY)
                    {
                        needStop = true;
                    }

                    if (x < destX && y > destY)
                    {
                        isCollisionDetected = true;
                    }
                    else if (x < destX && y < destY && !isCollisionDetected)
                    {
                        canTurn = true;
                    }
                }
                else if (turnType == TurnType.DownLeft)
                {
                    if (!isDiagonalPath && !isDiagonalExit && y > stopY)
                    {
                        needStop = true;
                    }

                 
                    if (x < destX && y < destY)
                    {
                        isCollisionDetected = true;
                    } 
                    else if (x < destX && y > destY && !isCollisionDetected)
                    {
                        canTurn = true;
                    }

                }
                else if (turnType == TurnType.DownRight)
                {
                    if (!isDiagonalPath && !isDiagonalExit && y > stopY)
                    {
                        needStop = true;
                    }

                   
                    if (x > destX && y < destY)
                    {
                        isCollisionDetected = true;
                    }
                    else if (x > destX && y > destY && !isCollisionDetected)
                    {
                        canTurn = true;
                    }
                }
                else if (turnType == TurnType.RightDown)
                {
                    if (!isDiagonalPath && !isDiagonalExit && x > stopX)
                    {
                        needStop = true;
                    }

                    
                    if (y > destY && x < destX)
                    {
                        isCollisionDetected = true;
                    }
                    else if (x > destX && y > destY && !isCollisionDetected)
                    {
                        canTurn = true;
                    }
                }
                else if (turnType == TurnType.RightUp)
                {
                     if (!isDiagonalPath && !isDiagonalExit && x > stopX)
                    {
                        needStop = true;
                    }

                   if (y < destY && x < destX)
                    {
                        isCollisionDetected = true;
                    }
                   else if (x > destX && y < destY && !isCollisionDetected)
                    {
                        canTurn = true;
                    }
                }
                else if (turnType == TurnType.LeftDown)
                {
                    if (!isDiagonalPath && !isDiagonalExit && x < stopX)
                    {
                        needStop = true;
                    }

                   
                    if (y > destY && x > destX)
                    {
                        isCollisionDetected = true;
                    }
                    else if (x < destX && y > destY && !isCollisionDetected)
                    {
                        canTurn = true;
                    }
                }
                else if (turnType == TurnType.LeftUp)
                {
                     if (!isDiagonalPath && !isDiagonalExit && x < stopX)
                    {
                        needStop = true;
                    }
                    if (y < destY && x > destX)
                    {
                        isCollisionDetected = true;
                    }
                     if (x < destX && y < destY && !isCollisionDetected)
                    {
                        canTurn = true;
                       
                    }
                }
              


                var vt = vx * Math.Cos(fi) - vy * Math.Sin(fi);
                double vn;
                if (!isRightTurn)
                {
                    vn = vx*Math.Sin(fi) + vy*Math.Cos(fi);
                }
                else
                {
                    vn = -vx*Math.Sin(fi) - vy*Math.Cos(fi);
                }

                if (!isDiagonalPath && !isDiagonalExit)
                 {
                    if (Math.Abs(wheelTurn) < 1)
                    {

                        wheelTurn += game.CarWheelTurnChangePerTick * (-turnCoeff);
                    }
                }
                else
                {
                    var tempPos = new Car(0, 0, x, y, 0, 0, -fi, 0, 0, 0, 0, 0, false, CarType.Buggy, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false);
                    var angleToEndPoint = tempPos.GetAngleTo(diagEndX, diagEndY);
                    //var turn = Math.Abs(angleToEndPoint) < game.CarWheelTurnChangePerTick ? Math.Abs(angleToEndPoint) : game.CarWheelTurnChangePerTick;
                    
                    wheelTurn = angleToEndPoint;

                    if (wheelTurn > 1) wheelTurn = 1;
                    if (wheelTurn < -1) wheelTurn = -1;         
                }


                omega = wheelTurn * game.CarAngularSpeedFactor * vt;

                var at = -Math.Abs(vt)*game.CarMovementAirFrictionFactor +
                         (move.IsBrake
                             ? -game.CarCrosswiseMovementFrictionFactor
                             : -game.CarLengthwiseMovementFrictionFactor +
                               game.BuggyEngineForwardPower/self.Mass*self.EnginePower);

                vt = FrictionCalculator.ApplyA(vt, at);

                var an = -Math.Abs(vn) * game.CarMovementAirFrictionFactor - game.CarCrosswiseMovementFrictionFactor;
                vn = FrictionCalculator.ApplyA(vn, an);

                
                if (!isRightTurn)
                {
                    vx = vt * Math.Cos(fi) + vn * Math.Sin(fi);
                    vy = -vt*Math.Sin(fi) + vn*Math.Cos(fi);
                }
                else
                {
                    vx = vt * Math.Cos(fi) - vn * Math.Sin(fi);
                    vy = -vt * Math.Sin(fi) - vn * Math.Cos(fi);
                }
                fi += Math.Abs(omega) * turnCoeff;

                //Debug.circle(x, y, 50, 0xFF0000);
            }


          


           

            if (isDiagonalPath || isDiagonalExit)
            {
                double betta;
                if (self.SpeedX == 0)
                {
                    if (self.SpeedY >= 0)
                    {
                        betta = Math.PI / 2;
                    }
                    else
                    {
                        betta = -Math.PI / 2;
                    }
                }
                else if (self.SpeedY == 0)
                {
                    if (self.SpeedX >= 0)
                    {
                        betta = 0d;
                    }
                    else
                    {
                        betta = Math.PI;
                    }
                }
                else
                {
                    var alpha = Math.Atan(Math.Abs(self.SpeedY / self.SpeedX));
                    if (self.SpeedX > 0 && self.SpeedY > 0)
                    {
                        betta = alpha;
                    }
                    else if (self.SpeedX < 0 && self.SpeedY > 0)
                    {
                        betta = Math.PI - alpha;
                    }
                    else if (self.SpeedX < 0 && self.SpeedY < 0)
                    {
                        betta = Math.PI + alpha;
                    }
                    else
                    {
                        betta = 2 * Math.PI - alpha;
                    }
                }

                var ax = -mu * g * Math.Cos(betta);
                var ay = -mu * g * Math.Sin(betta);
                var a = -mu * g;
                var selfSpeed = Math.Sqrt(self.SpeedX * self.SpeedX + self.SpeedY * self.SpeedY);

                double stopTime;
                var needBreak = false;

                stopTime = Math.Abs(selfSpeed / a);

                var xPath = self.SpeedX * stopTime + ax * stopTime * stopTime / 2;
                var yPath = self.SpeedY * stopTime + ay * stopTime * stopTime / 2;

                needStop = !IsInPath(self.X + xPath, self.Y + yPath, path, game, self);             
            }            

            if (needStop) 
            {
                if (Math.Sqrt(self.SpeedX * self.SpeedX + self.SpeedY * self.SpeedY) < 5)
                {
                    canTurn = true;
                }
                else
                {
                    //if (!isDiagonalPath)
                        move.IsBrake = true;
                }
            }

            else
            {
                #region Включить ли нитро

                bool canUseNitro = false;
                if (Math.Abs(self.WheelTurn) > Math.PI / 60)
                {
                    canUseNitro = false;
                }
                else
                {
                    double xBorder, yBorder;

                    if (isDiagonalPath || isDiagonalExit)
                    {
                        var lastNotDiagonalSquare = GetLastDiagonalSquare(path, self) as Square;

                        xBorder = self.SpeedX < 0 ? lastNotDiagonalSquare.X : lastNotDiagonalSquare.X + game.TrackTileSize;
                        yBorder = self.SpeedY < 0 ? lastNotDiagonalSquare.Y : lastNotDiagonalSquare.Y + game.TrackTileSize;

                        var xBorderDist = Math.Abs(xBorder - self.X) - self.Width/2;
                        var yBorderDist = Math.Abs(yBorder - self.Y) - self.Width/2;

                        if (xBorderDist < 0) xBorderDist = 0d;
                        if (yBorderDist < 0) yBorderDist = 0d;

                        canUseNitro =
                            Math.Abs(
                                self.GetAngleTo(
                                    lastNotDiagonalSquare.X + game.TrackTileSize/2,
                                    lastNotDiagonalSquare.Y + game.TrackTileSize/2)) < Math.PI/8 &&
                            xBorderDist > 4.5*game.TrackTileSize && yBorderDist > 4.5*game.TrackTileSize;
                    }
                    else
                    {
                        if (movingType == MovingType.Horizontal)
                        {
                            if ((lastlineSquare as Square).X > self.X)
                            {
                                xBorder = (lastlineSquare as Square).X + game.TrackTileSize;
                            }
                            else
                            {
                                xBorder = (lastlineSquare as Square).X;
                            }
                            yBorder = (lastlineSquare as Square).Y + game.TrackTileSize/2;

                            var xBorderDist = Math.Abs(xBorder - self.X) - self.Width/2;

                            canUseNitro =
                            Math.Abs(self.GetAngleTo(xBorder,yBorder)) < Math.PI / 8 && xBorderDist > 3.5* game.TrackTileSize;
                        }
                        else //vertical
                        {
                            if ((lastlineSquare as Square).Y > self.Y)
                            {
                                yBorder = (lastlineSquare as Square).Y + game.TrackTileSize;
                            }
                            else
                            {
                                yBorder = (lastlineSquare as Square).Y;
                            }
                            xBorder = (lastlineSquare as Square).X + game.TrackTileSize / 2;

                            var yBorderDist = Math.Abs(yBorder - self.Y) - self.Width / 2;

                            canUseNitro =
                            Math.Abs(self.GetAngleTo(xBorder, yBorder)) < Math.PI / 8 && yBorderDist > 3.5 * game.TrackTileSize;
                        }
                    }
                    //else if (Math.Abs(self.SpeedX) > Math.Abs(self.SpeedY))
                    //{
                    //    canUseNitro = xBorderDist > 3.5 * game.TrackTileSize;
                    //}
                    //else
                    //{
                    //    canUseNitro = yBorderDist > 3.5 * game.TrackTileSize;
                    //}
                }

                if (world.Tick > game.InitialFreezeDurationTicks && canUseNitro)
                    move.IsUseNitro = true;

                #endregion
            }


            if (isDiagonalPath)
            {           

                move.WheelTurn = self.GetAngleTo(diagEndX, diagEndY) * 4;
               
            }
            else if (isDiagonalExit)
            {
                move.EnginePower = 0d;
                move.WheelTurn = self.GetAngleTo(diagEndX, diagEndY) * 2.5;
            }


            else
            {
                if (canTurn)
                {
                    move.WheelTurn = -turnCoeff;
                }
                else
                {
                    double endX = 0, endY = 0;
                    if (is180Turn && self.GetDistanceTo((lastlineSquare as Square).X, (lastlineSquare as Square).Y) < 5 * game.TrackTileSize)
                    {

                        if (turnType == TurnType.UpRight || turnType == TurnType.RightUp)
                        {
                            endX = (lastlineSquare as Square).X;
                            endY = (lastlineSquare as Square).Y + game.TrackTileSize;
                        }
                        else if (turnType == TurnType.LeftUp || turnType == TurnType.UpLeft)
                        {
                            endX = (lastlineSquare as Square).X + game.TrackTileSize;
                            endY = (lastlineSquare as Square).Y + game.TrackTileSize;
                        }
                        else if (turnType == TurnType.DownRight || turnType == TurnType.RightDown)
                        {
                            endX = (lastlineSquare as Square).X;
                            endY = (lastlineSquare as Square).Y;
                        }
                        else if (turnType == TurnType.DownLeft || turnType == TurnType.LeftDown)
                        {
                            endX = (lastlineSquare as Square).X + game.TrackTileSize;
                            endY = (lastlineSquare as Square).Y;
                        }
                        else
                        {
                            throw new Exception();
                        }
                        move.WheelTurn = self.GetAngleTo(endX, endY) * 5;
                    }
                  
                    else
                    {

                        if (world.Tick <= game.InitialFreezeDurationTicks + game.NitroDurationTicks)
                        {
                            if (movingType == MovingType.Vertical)
                            {
                                endX = self.X;
                                endY = (lastVerticalSquare as Square).Y + game.TrackTileSize/2;
                            }
                            else if (movingType == MovingType.Horizontal)
                            {
                                endY = self.Y;
                                endX = (lastHorizontalSquare as Square).X + game.TrackTileSize/2;
                            }

                        }


                        else
                        {
                            var acceptableBonuses = new List<Bonus>();
                            if (movingType == MovingType.Horizontal)
                            {
                                var scoreNitroBonuses =
                                    world.Bonuses.Where(
                                        b =>
                                            b.Type == BonusType.PureScore ||
                                            b.Type == BonusType.NitroBoost && self.EnginePower < 2).ToList();
                                if (self.Durability < 0.5)
                                    scoreNitroBonuses.AddRange(world.Bonuses.Where(b => b.Type == BonusType.RepairKit));
                             
                                foreach (var bonus in scoreNitroBonuses)
                                {
                                    if ((lastHorizontalSquare as Square).X > self.X)
                                    {
                                        if (bonus.Y > selfSquare.Y + game.TrackTileMargin &&
                                            bonus.Y < selfSquare.Y + game.TrackTileSize - game.TrackTileMargin &&
                                            bonus.X >= self.X && bonus.X <= (lastHorizontalSquare as Square).X
                                            && Math.Abs(self.GetAngleTo(bonus)) < Math.PI/12)
                                        {
                                            acceptableBonuses.Add(bonus);
                                        }
                                    }
                                    else
                                    {
                                        if (bonus.Y > selfSquare.Y + game.TrackTileMargin &&
                                            bonus.Y < selfSquare.Y + game.TrackTileSize - game.TrackTileMargin &&
                                            bonus.X <= self.X &&
                                            bonus.X >= (lastHorizontalSquare as Square).X + game.TrackTileSize &&
                                            Math.Abs(self.GetAngleTo(bonus)) < Math.PI/12)
                                        {
                                            acceptableBonuses.Add(bonus);
                                        }
                                    }
                                }


                                if (acceptableBonuses.Any())
                                {
                                    var nearestBonus = acceptableBonuses[0];
                                    foreach (var bonus in acceptableBonuses)
                                    {
                                        if (self.GetDistanceTo(bonus) < self.GetDistanceTo(nearestBonus))
                                        {
                                            nearestBonus = bonus;
                                        }
                                    }

                                    endX = nearestBonus.X;
                                    if (self.Y < nearestBonus.Y - self.Height/2)
                                    {
                                        endY = nearestBonus.Y - self.Height/2;
                                    }
                                    else if (self.Y > nearestBonus.Y + self.Height/2)
                                    {
                                        endY = nearestBonus.Y + self.Height/2;
                                    }
                                    else
                                    {
                                        endY = self.Y;
                                    }
                                }

                            }

                            else if (movingType == MovingType.Vertical)
                            {
                                var scoreNitroBonuses = world.Bonuses.Where(b => b.Type == BonusType.PureScore || b.Type == BonusType.NitroBoost && self.EnginePower < 2).ToList();
                                if (self.Durability < 0.5) scoreNitroBonuses.AddRange(world.Bonuses.Where(b => b.Type == BonusType.RepairKit));
                                foreach (var bonus in scoreNitroBonuses)
                                {
                                    if ((lastVerticalSquare as Square).Y > self.Y)
                                    {
                                        if (bonus.X > selfSquare.X + game.TrackTileMargin &&
                                            bonus.X < selfSquare.X + game.TrackTileSize - game.TrackTileMargin &&
                                            bonus.Y >= self.Y && bonus.Y <= (lastVerticalSquare as Square).Y
                                            && Math.Abs(self.GetAngleTo(bonus)) < Math.PI / 12)
                                        {
                                            acceptableBonuses.Add(bonus);
                                        }
                                    }
                                    else
                                    {
                                        if (bonus.X > selfSquare.X + game.TrackTileMargin &&
                                            bonus.X < selfSquare.X + game.TrackTileSize - game.TrackTileMargin &&
                                            bonus.Y <= self.Y && bonus.Y >= (lastVerticalSquare as Square).Y + game.TrackTileSize &&
                                            Math.Abs(self.GetAngleTo(bonus)) < Math.PI / 12)
                                        {
                                            acceptableBonuses.Add(bonus);
                                        }
                                    }
                                }

                                if (acceptableBonuses.Any())
                                {
                                    var nearestBonus = acceptableBonuses[0];
                                    foreach (var bonus in acceptableBonuses)
                                    {
                                        if (self.GetDistanceTo(bonus) < self.GetDistanceTo(nearestBonus))
                                        {
                                            nearestBonus = bonus;
                                        }
                                    }
                                    endY = nearestBonus.Y;
                                    if (self.X < nearestBonus.X - self.Height / 2)
                                    {
                                        endX = nearestBonus.X - self.Height / 2;
                                    }
                                    else if (self.X > nearestBonus.X + self.Height / 2)
                                    {
                                        endX = nearestBonus.X + self.Height / 2;
                                    }
                                    else
                                    {
                                        endX = self.X;
                                    }
                                }
                            }

                            if (!acceptableBonuses.Any())
                            {
                                if (turnType == TurnType.DownLeft || turnType == TurnType.UpLeft)
                                {
                                    endY = (lastlineSquare as Square).Y + game.TrackTileSize/2;
                                    endX = self.X > (lastlineSquare as Square).X + game.TrackTileSize/2 && isCorrectX
                                        ? self.X
                                        : (lastlineSquare as Square).X + game.TrackTileSize/2;
                                }
                                else if (turnType == TurnType.DownRight || turnType == TurnType.UpRight)
                                {
                                    endY = (lastlineSquare as Square).Y + game.TrackTileSize/2;
                                    endX = self.X < (lastlineSquare as Square).X + game.TrackTileSize/2 && isCorrectX
                                        ? self.X
                                        : (lastlineSquare as Square).X + game.TrackTileSize/2;
                                }
                                else if (turnType == TurnType.RightUp || turnType == TurnType.LeftUp)
                                {
                                    endX = (lastlineSquare as Square).X + game.TrackTileSize/2;
                                    endY = self.Y > (lastlineSquare as Square).Y + game.TrackTileSize/2 && isCorrectY
                                        ? self.Y
                                        : (lastlineSquare as Square).Y + game.TrackTileSize/2;
                                }
                                else if (turnType == TurnType.RightDown || turnType == TurnType.LeftDown)
                                {
                                    endX = (lastlineSquare as Square).X + game.TrackTileSize/2;
                                    endY = self.Y < (lastlineSquare as Square).Y + game.TrackTileSize/2 && isCorrectY
                                        ? self.Y
                                        : (lastlineSquare as Square).Y + game.TrackTileSize/2;
                                }
                                else
                                {
                                    throw new Exception();
                                }
                            }
                        }
                        move.WheelTurn = self.GetAngleTo(endX, endY) * 5;
                    }

                    

                }
            }

            //if (!canTurn && needStop)
            //{
            //    move.IsBrake = true;
            //}

            //if (needStop)
            //{
            //    move.IsBrake = true;
            //}

            //Debug.endPost();

            //if (movingType == MovingType.Horizontal)
            //{
            //    nextTileType = Equals(path[nextPointNumber], lastHorizontalSquare) ? NextTileType.LastHorizontal : NextTileType.NotLastHorizontal;
            //    if (nextTileType == NextTileType.NotLastHorizontal)
            //    {


            //        var scoreNitroBonuses = world.Bonuses.Where(x => x.Type == BonusType.PureScore || x.Type == BonusType.NitroBoost && self.EnginePower < 2).ToList();
            //        if (self.Durability < 0.5) scoreNitroBonuses.AddRange(world.Bonuses.Where(x => x.Type == BonusType.RepairKit));
            //        var acceptableBonuses = new List<Bonus>();
            //        foreach (var bonus in scoreNitroBonuses)
            //        {
            //            if ((lastHorizontalSquare as Square).X > self.X)
            //            {
            //                if (bonus.Y > selfSquare.Y + game.TrackTileMargin &&
            //                    bonus.Y < selfSquare.Y + game.TrackTileSize - game.TrackTileMargin &&
            //                    bonus.X >= self.X && bonus.X <= (lastHorizontalSquare as Square).X
            //                    && Math.Abs(self.GetAngleTo(bonus)) < Math.PI / 12)
            //                {
            //                    acceptableBonuses.Add(bonus);
            //                }
            //            }
            //            else
            //            {
            //                if (bonus.Y > selfSquare.Y + game.TrackTileMargin &&
            //                    bonus.Y < selfSquare.Y + game.TrackTileSize - game.TrackTileMargin &&
            //                    bonus.X <= self.X && bonus.X >= (lastHorizontalSquare as Square).X + game.TrackTileSize &&
            //                    Math.Abs(self.GetAngleTo(bonus)) < Math.PI / 12)
            //                {
            //                    acceptableBonuses.Add(bonus);
            //                }
            //            }
            //            }
            //        }

            //        if (world.Tick <= game.InitialFreezeDurationTicks + game.NitroDurationTicks)
            //        {
            //            destX = (lastHorizontalSquare as Square).X;
            //            destY = self.Y;
            //        }

            //        else if (acceptableBonuses.Any())
            //        {
            //            var nearestBonus = acceptableBonuses[0];
            //            foreach (var bonus in acceptableBonuses)
            //            {
            //                if (self.GetDistanceTo(bonus) < self.GetDistanceTo(nearestBonus))
            //                {
            //                    nearestBonus = bonus;
            //                }
            //            }

            //            destX = nearestBonus.X;
            //            if (self.Y < nearestBonus.Y - self.Height / 2)
            //            {
            //                destY = nearestBonus.Y - self.Height / 2;
            //            }
            //            else if (self.Y > nearestBonus.Y + self.Height / 2)
            //            {
            //                destY = nearestBonus.Y + self.Height / 2;
            //            }
            //            else
            //            {
            //                destY = self.Y;
            //            }
            //        }

            //        else // нет бонусов на пути
            //        {
            //            var angle = self.GetAngleTo(afterLastInLineSquare.X + game.TrackTileSize / 2, afterLastInLineSquare.Y + game.TrackTileSize / 2);
            //            if ((afterLastInLineSquare.Y < self.Y && self.X > afterLastInLineSquare.X || afterLastInLineSquare.Y > self.Y && self.X < afterLastInLineSquare.X)
            //                && angle > Math.PI / 6
            //                && isCorrectY
            //                && self.GetDistanceTo((lastHorizontalSquare as Square).X + game.TrackTileSize / 2, (lastHorizontalSquare as Square).Y + game.TrackTileSize / 2) < game.TrackTileSize * 3)
            //            {

            //                destX = (lastHorizontalSquare as Square).X + game.TrackTileSize / 2d;
            //                destY = self.Y;

            //            }
            //            else if ((afterLastInLineSquare.Y < self.Y && self.X > afterLastInLineSquare.X || afterLastInLineSquare.Y > self.Y && self.X < afterLastInLineSquare.X)
            //                && angle < -Math.PI / 6
            //                && isCorrectY
            //                && self.GetDistanceTo((lastHorizontalSquare as Square).X + game.TrackTileSize / 2, (lastHorizontalSquare as Square).Y + game.TrackTileSize / 2) < game.TrackTileSize * 3)
            //            {
            //                destX = (lastHorizontalSquare as Square).X + game.TrackTileSize / 2d;
            //                destY = self.Y;
            //            }
            //            else
            //            {
            //                switch ((lastHorizontalSquare as Square).Type)
            //                {
            //                    case TileType.Crossroads:
            //                    case TileType.TopHeadedT:
            //                    case TileType.BottomHeadedT:
            //                    case TileType.RightHeadedT:
            //                    case TileType.LeftHeadedT:
            //                    case TileType.Horizontal:
            //                    case TileType.LeftBottomCorner:
            //                    case TileType.RightBottomCorner:
            //                    case TileType.LeftTopCorner:
            //                    case TileType.RightTopCorner:
            //                        if (beforeLastInLineSquare != null && afterLastInLineSquare != null)
            //                        {
            //                            if (afterLastInLineSquare.Y > (lastHorizontalSquare as Square).Y)
            //                            {
            //                                if (self.Y > (lastHorizontalSquare as Square).Y + game.TrackTileSize / 2d)
            //                                {
            //                                    destX = (beforeLastInLineSquare as Square).X + game.TrackTileSize / 2d;
            //                                    destY = (beforeLastInLineSquare as Square).Y + game.TrackTileSize / 2d;
            //                                }
            //                                else
            //                                {
            //                                    destX = (lastHorizontalSquare as Square).X + game.TrackTileSize / 2d;
            //                                    destY = (lastHorizontalSquare as Square).Y + game.TrackTileSize / 2d;
            //                                }
            //                            }
            //                            else
            //                            {
            //                                if (self.Y < (lastHorizontalSquare as Square).Y + game.TrackTileSize / 2d)
            //                                {
            //                                    destX = (beforeLastInLineSquare as Square).X + game.TrackTileSize / 2d;
            //                                    destY = (beforeLastInLineSquare as Square).Y + game.TrackTileSize / 2d;
            //                                }
            //                                else
            //                                {
            //                                    destX = (lastHorizontalSquare as Square).X + game.TrackTileSize / 2d;
            //                                    destY = (lastHorizontalSquare as Square).Y + game.TrackTileSize / 2d;
            //                                }
            //                            }
            //                        }
            //                        else
            //                        {
            //                            destX = (lastHorizontalSquare as Square).X + game.TrackTileSize / 2d;
            //                            destY = (lastHorizontalSquare as Square).Y + game.TrackTileSize / 2d;
            //                        }

            //                        break;
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {

            //        isDiagonalPath = IsDiagonalPath(path);

            //        var leftTopNearBorder = game.TrackTileMargin + self.Height / 2;
            //        //if (!Is180DegreesTurn(path)) leftTopNearBorder += self.Height / 2;
            //        var leftTopFarBorder = game.TrackTileSize * 0.25;
            //        var rightBottomNearBorder = game.TrackTileSize - game.TrackTileMargin - self.Height / 2;
            //        //if (!Is180DegreesTurn(path)) rightBottomNearBorder -= self.Height / 2;
            //        var rightBottomFarBorder = game.TrackTileSize * 0.75;

            //        var leftTopBorder = isDiagonalPath ? leftTopFarBorder : leftTopNearBorder;
            //        var rightBottomBorder = isDiagonalPath ? rightBottomFarBorder : rightBottomNearBorder;

            //        switch ((lastHorizontalSquare as Square).Type)
            //        {
            //            case TileType.Crossroads:
            //            case TileType.RightHeadedT:
            //            case TileType.LeftHeadedT:
            //            case TileType.Horizontal:

            //                if (afterNextSquare != null && afterNextSquare.Y > (path[nextPointNumber] as Square).Y)
            //                {
            //                    if (self.Y > (path[nextPointNumber] as Square).Y + rightBottomBorder && isCorrectY)
            //                    {
            //                        destY = self.Y;
            //                    }
            //                    else
            //                    {
            //                        destY = (path[nextPointNumber] as Square).Y + rightBottomBorder;
            //                    }

            //                    if (selfSquare.X < (path[nextPointNumber] as Square).X)
            //                    // невозможно для TileType.LeftTopCorner
            //                    {
            //                        destX = (path[nextPointNumber] as Square).X + leftTopBorder;
            //                    }
            //                    else // невозможно для TileType.RightTopCorner
            //                    {
            //                        destX = (path[nextPointNumber] as Square).X + rightBottomBorder;
            //                    }
            //                }
            //                else if (afterNextSquare != null && afterNextSquare.Y < (path[nextPointNumber] as Square).Y)
            //                {
            //                    if (self.Y < (path[nextPointNumber] as Square).Y + leftTopBorder && isCorrectY)
            //                    {
            //                        destY = self.Y;
            //                    }
            //                    else
            //                    {
            //                        destY = (path[nextPointNumber] as Square).Y + leftTopBorder;
            //                    }

            //                    if (selfSquare.X < (path[nextPointNumber] as Square).X)
            //                    // невозможно для TileType.LeftBottomCorner
            //                    {
            //                        destX = (path[nextPointNumber] as Square).X + leftTopBorder;
            //                    }
            //                    else // невозможно для TileType.RightBottomCorner
            //                    {
            //                        destX = (path[nextPointNumber] as Square).X + rightBottomBorder;
            //                    }
            //                }
            //                else //движение по горизонтали
            //                {
            //                    destX = (path[nextPointNumber] as Square).X + game.TrackTileSize / 2d;
            //                    destY = (path[nextPointNumber] as Square).Y + game.TrackTileSize / 2d;
            //                }
            //                break;
            //            case TileType.BottomHeadedT:
            //            case TileType.RightTopCorner:
            //            case TileType.LeftTopCorner:
            //                if (self.Y > (path[nextPointNumber] as Square).Y + rightBottomBorder && isCorrectY)
            //                {
            //                    destY = self.Y;
            //                }
            //                else
            //                {
            //                    destY = (path[nextPointNumber] as Square).Y + rightBottomBorder;
            //                }

            //                if (selfSquare.X < (path[nextPointNumber] as Square).X) // невозможно для TileType.LeftTopCorner
            //                {
            //                    destX = (path[nextPointNumber] as Square).X + leftTopBorder;
            //                }
            //                else // невозможно для TileType.RightTopCorner
            //                {
            //                    destX = (path[nextPointNumber] as Square).X + rightBottomBorder;
            //                }

            //                break;
            //            case TileType.TopHeadedT:
            //            case TileType.RightBottomCorner:
            //            case TileType.LeftBottomCorner:
            //                if (self.Y < (path[nextPointNumber] as Square).Y + leftTopBorder && isCorrectY)
            //                {
            //                    destY = self.Y;
            //                }
            //                else
            //                {
            //                    destY = (path[nextPointNumber] as Square).Y + leftTopBorder;
            //                }

            //                if (selfSquare.X < (path[nextPointNumber] as Square).X)
            //                // невозможно для TileType.LeftBottomCorner
            //                {
            //                    destX = (path[nextPointNumber] as Square).X + leftTopBorder;
            //                }
            //                else // невозможно для TileType.RightBottomCorner
            //                {
            //                    destX = (path[nextPointNumber] as Square).X + rightBottomBorder;
            //                }
            //                break;
            //        }
            //        //}
            //    }
            //}

            //else //вертикальное движение
            //{
            //    nextTileType = Equals(path[nextPointNumber], lastVerticalSquare) ? NextTileType.LastVertical : NextTileType.NotLastVertical;
            //    if (nextTileType == NextTileType.NotLastVertical)
            //    {

            //        var scoreNitroBonuses = world.Bonuses.Where(x => x.Type == BonusType.PureScore || x.Type == BonusType.NitroBoost && self.EnginePower < 2).ToList();
            //        if (self.Durability < 0.5) scoreNitroBonuses.AddRange(world.Bonuses.Where(x => x.Type == BonusType.RepairKit));
            //        var acceptableBonuses = new List<Bonus>();
            //        foreach (var bonus in scoreNitroBonuses)
            //        {
            //            if ((lastVerticalSquare as Square).Y > self.Y)
            //            {
            //                if (bonus.X > selfSquare.X + game.TrackTileMargin &&
            //                    bonus.X < selfSquare.X + game.TrackTileSize - game.TrackTileMargin &&
            //                    bonus.Y >= self.Y && bonus.Y <= (lastVerticalSquare as Square).Y
            //                    && Math.Abs(self.GetAngleTo(bonus)) < Math.PI / 12)
            //                {
            //                    acceptableBonuses.Add(bonus);
            //                }
            //            }
            //            else
            //            {
            //                if (bonus.X > selfSquare.X + game.TrackTileMargin &&
            //                    bonus.X < selfSquare.X + game.TrackTileSize - game.TrackTileMargin &&
            //                    bonus.Y <= self.Y && bonus.Y >= (lastVerticalSquare as Square).Y + game.TrackTileSize &&
            //                    Math.Abs(self.GetAngleTo(bonus)) < Math.PI / 12)
            //                {
            //                    acceptableBonuses.Add(bonus);
            //                }
            //            }
            //        }

            //        if (world.Tick <= game.InitialFreezeDurationTicks + game.NitroDurationTicks)
            //        {
            //            destX = self.X;
            //            destY = (lastVerticalSquare as Square).Y;
            //        }

            //        else if (acceptableBonuses.Any())
            //        {
            //            var nearestBonus = acceptableBonuses[0];
            //            foreach (var bonus in acceptableBonuses)
            //            {
            //                if (self.GetDistanceTo(bonus) < self.GetDistanceTo(nearestBonus))
            //                {
            //                    nearestBonus = bonus;
            //                }
            //            }
            //            destY = nearestBonus.Y;
            //            if (self.X < nearestBonus.X - self.Height / 2)
            //            {
            //                destX = nearestBonus.X - self.Height / 2;
            //            }
            //            else if (self.X > nearestBonus.X + self.Height / 2)
            //            {
            //                destX = nearestBonus.X + self.Height / 2;
            //            }
            //            else
            //            {
            //                destX = self.X;
            //            }
            //        }

            //        else //no bonuses
            //        {
            //            var angle = self.GetAngleTo(afterLastInLineSquare.X + game.TrackTileSize / 2, afterLastInLineSquare.Y + game.TrackTileSize / 2);

            //            if ((afterLastInLineSquare.X < self.X && self.Y > afterLastInLineSquare.Y || afterLastInLineSquare.X > self.X && self.Y < afterLastInLineSquare.Y)
            //                && angle < -Math.PI / 6
            //                && isCorrectX
            //                && self.GetDistanceTo((lastVerticalSquare as Square).X + game.TrackTileSize / 2, (lastVerticalSquare as Square).Y + game.TrackTileSize / 2) < game.TrackTileSize * 3)
            //            {
            //                destY = (lastVerticalSquare as Square).Y + game.TrackTileSize / 2d;
            //                destX = self.X;

            //            }
            //            else if ((afterLastInLineSquare.X > self.X && self.Y > afterLastInLineSquare.Y || afterLastInLineSquare.X < self.X && self.Y < afterLastInLineSquare.Y)
            //                && angle > Math.PI / 6
            //                && isCorrectX
            //                && self.GetDistanceTo((lastVerticalSquare as Square).X + game.TrackTileSize / 2, (lastVerticalSquare as Square).Y + game.TrackTileSize / 2) < game.TrackTileSize * 3)
            //            {

            //                destY = (lastVerticalSquare as Square).Y + game.TrackTileSize / 2d;
            //                destX = self.X;

            //            }
            //            else
            //            {
            //                switch ((lastVerticalSquare as Square).Type)
            //                {
            //                    case TileType.Crossroads:
            //                    case TileType.TopHeadedT:
            //                    case TileType.BottomHeadedT:
            //                    case TileType.RightHeadedT:
            //                    case TileType.LeftHeadedT:
            //                    case TileType.Vertical:
            //                    case TileType.RightTopCorner:
            //                    case TileType.RightBottomCorner:
            //                    case TileType.LeftTopCorner:
            //                    case TileType.LeftBottomCorner:
            //                        if (beforeLastInLineSquare != null && afterLastInLineSquare != null)
            //                        {
            //                            if (afterLastInLineSquare.X > (lastHorizontalSquare as Square).X)
            //                            {
            //                                if (self.X > (lastHorizontalSquare as Square).X + game.TrackTileSize / 2d)
            //                                {
            //                                    destX = (beforeLastInLineSquare as Square).X + game.TrackTileSize / 2d;
            //                                    destY = (beforeLastInLineSquare as Square).Y + game.TrackTileSize / 2d;
            //                                }
            //                                else
            //                                {
            //                                    destX = (lastVerticalSquare as Square).X + game.TrackTileSize / 2d;
            //                                    destY = (lastVerticalSquare as Square).Y + game.TrackTileSize / 2d;
            //                                }
            //                            }
            //                            else
            //                            {
            //                                if (self.X < (lastHorizontalSquare as Square).X + game.TrackTileSize / 2d)
            //                                {
            //                                    destX = (beforeLastInLineSquare as Square).X + game.TrackTileSize / 2d;
            //                                    destY = (beforeLastInLineSquare as Square).Y + game.TrackTileSize / 2d;
            //                                }
            //                                else
            //                                {
            //                                    destX = (lastVerticalSquare as Square).X + game.TrackTileSize / 2d;
            //                                    destY = (lastVerticalSquare as Square).Y + game.TrackTileSize / 2d;
            //                                }
            //                            }
            //                        }
            //                        else
            //                        {
            //                            destX = (lastVerticalSquare as Square).X + game.TrackTileSize / 2d;
            //                            destY = (lastVerticalSquare as Square).Y + game.TrackTileSize / 2d;
            //                        }
            //                        break;
            //                }
            //            }
            //        }
            //    }
            //    else
            //    {

            //        isDiagonalPath = IsDiagonalPath(path);

            //        var leftTopNearBorder = game.TrackTileMargin + self.Height / 2;
            //        var leftTopFarBorder = game.TrackTileSize * 0.25;
            //        var rightBottomNearBorder = game.TrackTileSize - game.TrackTileMargin - self.Height / 2;
            //        var rightBottomFarBorder = game.TrackTileSize * 0.75;

            //        var leftTopBorder = isDiagonalPath ? leftTopFarBorder : leftTopNearBorder;
            //        var rightBottomBorder = isDiagonalPath ? rightBottomFarBorder : rightBottomNearBorder;


            //        switch ((lastVerticalSquare as Square).Type)
            //        {
            //            case TileType.Crossroads:
            //            case TileType.TopHeadedT:
            //            case TileType.BottomHeadedT:
            //            case TileType.Vertical:
            //                if (afterNextSquare != null && afterNextSquare.X > (path[nextPointNumber] as Square).X)
            //                {
            //                    if (self.X > (path[nextPointNumber] as Square).X + rightBottomBorder && isCorrectX)
            //                    {
            //                        destX = self.X;
            //                    }
            //                    else
            //                    {
            //                        destX = (path[nextPointNumber] as Square).X + rightBottomBorder;
            //                    }

            //                    if (selfSquare.Y < (path[nextPointNumber] as Square).Y)
            //                    // невозможно для TileType.LeftTopCorner
            //                    {
            //                        destY = (path[nextPointNumber] as Square).Y + leftTopBorder;
            //                    }
            //                    else // невозможно для TileType.LeftBottomCorner
            //                    {
            //                        destY = (path[nextPointNumber] as Square).Y + rightBottomBorder;
            //                    }
            //                }
            //                else if (afterNextSquare != null && afterNextSquare.X < (path[nextPointNumber] as Square).X)
            //                {
            //                    if (self.X < (path[nextPointNumber] as Square).X + leftTopBorder && isCorrectX)
            //                    {
            //                        destX = self.X;
            //                    }
            //                    else
            //                    {
            //                        destX = (path[nextPointNumber] as Square).X + leftTopBorder;
            //                    }

            //                    if (selfSquare.Y < (path[nextPointNumber] as Square).Y)
            //                    // невозможно для TileType.RightTopCorner
            //                    {
            //                        destY = (path[nextPointNumber] as Square).Y + leftTopBorder;
            //                    }
            //                    else // невозможно для TileType.RightBottomCorner
            //                    {
            //                        destY = (path[nextPointNumber] as Square).Y + rightBottomBorder;
            //                    }
            //                }
            //                else //движение по вертикали
            //                {
            //                    destX = (path[nextPointNumber] as Square).X + game.TrackTileSize / 2d;
            //                    destY = (path[nextPointNumber] as Square).Y + game.TrackTileSize / 2d;
            //                }
            //                break;
            //            case TileType.LeftHeadedT:
            //            case TileType.RightTopCorner:
            //            case TileType.RightBottomCorner:
            //                if (self.X < (path[nextPointNumber] as Square).X + leftTopBorder && isCorrectX)
            //                {
            //                    destX = self.X;
            //                }
            //                else
            //                {
            //                    destX = (path[nextPointNumber] as Square).X + leftTopBorder;
            //                }

            //                if (selfSquare.Y < (path[nextPointNumber] as Square).Y) // невозможно для TileType.RightTopCorner
            //                {
            //                    destY = (path[nextPointNumber] as Square).Y + leftTopBorder;
            //                }
            //                else // невозможно для TileType.RightBottomCorner
            //                {
            //                    destY = (path[nextPointNumber] as Square).Y + rightBottomBorder;
            //                }

            //                break;
            //            case TileType.RightHeadedT:
            //            case TileType.LeftTopCorner:
            //            case TileType.LeftBottomCorner:
            //                if (self.X > (path[nextPointNumber] as Square).X + rightBottomBorder && isCorrectX)
            //                {
            //                    destX = self.X;
            //                }
            //                else
            //                {
            //                    destX = (path[nextPointNumber] as Square).X + rightBottomBorder;
            //                }

            //                if (selfSquare.Y < (path[nextPointNumber] as Square).Y) // невозможно для TileType.LeftTopCorner
            //                {
            //                    destY = (path[nextPointNumber] as Square).Y + leftTopBorder;
            //                }
            //                else // невозможно для TileType.LeftBottomCorner
            //                {
            //                    destY = (path[nextPointNumber] as Square).Y + rightBottomBorder;
            //                }
            //                break;
            //        }
            //        //}
            //    }
            //}


            #endregion


            #region Определение необходимости торможения

            //var isDiagonalMoving = Math.Abs(self.Angle) > Math.PI / 6 && Math.Abs(self.Angle) < 2 * Math.PI / 6 ||
            //                       Math.Abs(self.Angle) > 4 * Math.PI / 6 && Math.Abs(self.Angle) < 5 * Math.PI / 6;


            //double betta;
            //if (self.SpeedX == 0)
            //{
            //    if (self.SpeedY >= 0)
            //    {
            //        betta = Math.PI / 2;
            //    }
            //    else
            //    {
            //        betta = -Math.PI / 2;
            //    }
            //}
            //else if (self.SpeedY == 0)
            //{
            //    if (self.SpeedX >= 0)
            //    {
            //        betta = 0d;
            //    }
            //    else
            //    {
            //        betta = Math.PI;
            //    }
            //}
            //else
            //{
            //    var alpha = Math.Atan(Math.Abs(self.SpeedY / self.SpeedX));
            //    if (self.SpeedX > 0 && self.SpeedY > 0)
            //    {
            //        betta = alpha;
            //    }
            //    else if (self.SpeedX < 0 && self.SpeedY > 0)
            //    {
            //        betta = Math.PI - alpha;
            //    }
            //    else if (self.SpeedX < 0 && self.SpeedY < 0)
            //    {
            //        betta = Math.PI + alpha;
            //    }
            //    else
            //    {
            //        betta = 2 * Math.PI - alpha;
            //    }
            //}      

            //var ax = -mu * g * Math.Cos(betta);
            //var ay = -mu * g * Math.Sin(betta);
            //var a = -mu * g;
            //var selfSpeed = Math.Sqrt(self.SpeedX * self.SpeedX + self.SpeedY * self.SpeedY);

            //double stopTime;
            //var needBreak = false;

            //stopTime = Math.Abs(selfSpeed / a);

            //var xPath = self.SpeedX * stopTime + ax * stopTime * stopTime / 2;
            //var yPath = self.SpeedY * stopTime + ay * stopTime * stopTime / 2;


            //double xBorder, yBorder;
            //double xBorderDist, yBorderDist;

            //if (isDiagonalMoving)
            //{
            //    needBreak = !IsInPath(self.X + xPath, self.Y + yPath, path, game, self);

            //    if (needBreak)
            //    {
            //        xBorderDist = 0d;
            //        yBorderDist = 0d;
            //    }
            //    else
            //    {
            //        var lastNotDiagonalSquare = GetLastDiagonalSquare(path, self) as Square;

            //        xBorder = self.SpeedX < 0 ? lastNotDiagonalSquare.X : lastNotDiagonalSquare.X + game.TrackTileSize;
            //        yBorder = self.SpeedY < 0 ? lastNotDiagonalSquare.Y : lastNotDiagonalSquare.Y + game.TrackTileSize;

            //        xBorderDist = Math.Abs(xBorder - self.X) - self.Width / 2;
            //        yBorderDist = Math.Abs(yBorder - self.Y) - self.Width / 2;

            //        if (xBorderDist < 0) xBorderDist = 0d;
            //        if (yBorderDist < 0) yBorderDist = 0d;
            //    }
            //}
            //else
            //{

            //    if (movingType == MovingType.Horizontal)
            //    {
            //        if (self.SpeedX > 0)
            //        {
            //            xBorder = (lastHorizontalSquare as Square).X + game.TrackTileSize;
            //        }
            //        else
            //        {
            //            xBorder = (lastHorizontalSquare as Square).X;
            //        }
            //        yBorder = GetYBoder(selfSquare, self, world, game);
            //    }
            //    else
            //    {
            //        if (self.SpeedY > 0)
            //        {
            //            yBorder = (lastVerticalSquare as Square).Y + game.TrackTileSize;
            //        }
            //        else
            //        {
            //            yBorder = (lastVerticalSquare as Square).Y;
            //        }
            //        xBorder = GetXBoder(selfSquare, self, world, game);
            //    }


            //    xBorderDist = Math.Abs(xBorder - self.X) - self.Width / 2;
            //    yBorderDist = Math.Abs(yBorder - self.Y) - self.Width / 2;

            //    if (xBorderDist < 0) xBorderDist = 0d;
            //    if (yBorderDist < 0) yBorderDist = 0d;

            //    if (Math.Abs(yPath) > yBorderDist)
            //    {
            //        needBreak = true;
            //    }

            //    if (Math.Abs(xPath) > xBorderDist)
            //    {
            //        needBreak = true;
            //    }
            //}          


            #endregion

            move.EnginePower = 1d;

            #region Определение угла на цель

            //if (isDiagonalPath) angleMultiplier = 4;
            //double angleToWaypoint = self.GetAngleTo(destX, destY) * angleMultiplier;

            //move.WheelTurn = self.EnginePower >= 0 ? angleToWaypoint : -angleToWaypoint;

            #endregion

            #region Надо ли ехать назад

            var needGoBack = CanGoBack(borderDistToGoBack, angleToGoBack, selfWayPointX, selfWayPointY, world, game, self) &&
                (NeedBorderGoBack(borderDistToGoBack, angleToGoBack, selfWayPointX, selfWayPointY, world, game, self) ||
                NeedCarGoBack(carDistToGoBack, angleToGoBack, self, world) || NeedPositionGoBack(self, path[nextPointNumber] as Square, game));

            if (needGoBack)
            {
                move.EnginePower = -1;               
               
            }

            var vtCurr = self.SpeedX * Math.Cos(-self.Angle) - self.SpeedY * Math.Sin(-self.Angle);
            if (needGoBack || vtCurr < 0)
            {
                move.WheelTurn *= -1;
            }

            #endregion



            //if (needBreak)
            //{
            //    move.IsBrake = true;
            //}

            //else
            //{
            //    move.IsBrake = false;

            //    #region Включить ли нитро

            //    bool canUseNitro;
            //    if (Math.Abs(angleToWaypoint) >= Math.PI / 8 || self.WheelTurn > Math.PI / 60)
            //    {
            //        canUseNitro = false;
            //    }
            //    else
            //    {
            //        if (isDiagonalMoving)
            //        {
            //            canUseNitro = xBorderDist > 4.5 * game.TrackTileSize && yBorderDist > 4.5 * game.TrackTileSize;
            //        }
            //        else if (Math.Abs(self.SpeedX) > Math.Abs(self.SpeedY))
            //        {
            //            canUseNitro = xBorderDist > 3.5 * game.TrackTileSize;
            //        }
            //        else
            //        {
            //            canUseNitro = yBorderDist > 3.5 * game.TrackTileSize;
            //        }
            //    }

            //    if (world.Tick > game.InitialFreezeDurationTicks && canUseNitro)
            //        move.IsUseNitro = true;

            //    #endregion
            //}


            move.IsThrowProjectile = CanShoot(self, world, game, startSquare, table, squares);
            move.IsSpillOil = NeedOil(self, path, world);

            if (_currentStartSquare == null || (path[0] as Square).X != _currentStartSquare.X || (path[0] as Square).Y != _currentStartSquare.Y)
            {
                _lastSquare = _currentStartSquare;
                _currentStartSquare = path[0] as Square;
            }

            //Debug.circleFill(destX, destY, 25, 0XFF0000);
            //Debug.endPost();
        }

        private int GetWayPointCoord(double coord, Game game)
        {
            return (int) (coord/game.TrackTileSize);
        }

        private bool NeedPositionGoBack(Car self, Square destSquare, Game game)
        {
            if (Math.Abs(self.GetAngleTo(destSquare.X + game.TrackTileSize/2d, destSquare.Y + game.TrackTileSize/2d)) > Math.PI/2)
            {
                return true;
            }
            return false;
        }

        private bool NeedCarGoBack(double distToGoBack, double angleToGoBac, Car self, World world)
        {
            var speed = Math.Sqrt(self.SpeedX*self.SpeedX + self.SpeedY*self.SpeedY);

            foreach (var car in world.Cars)
            {
                if (car.IsTeammate || car.IsFinishedTrack) continue;
                if (self.GetDistanceTo(car) < distToGoBack && Math.Abs(self.GetAngleTo(car)) < Math.PI/4 && speed < 4d)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanGoBack(
            double distToGoBack,
            double angleToGoBack,
            int selfWayPointX,
            int selfWayPointY,
            World world,
            Game game,
            Car self)
        {
            var type = world.TilesXY[selfWayPointX][selfWayPointY];
            var hasLeftBorder = type == TileType.LeftBottomCorner || type == TileType.LeftTopCorner ||
                                type == TileType.RightHeadedT || type == TileType.Vertical;
            var hasRightBorder = type == TileType.RightBottomCorner || type == TileType.RightTopCorner ||
                                 type == TileType.LeftHeadedT || type == TileType.Vertical;
            var hasTopBorder = type == TileType.BottomHeadedT || type == TileType.Horizontal || type == TileType.LeftTopCorner ||
                               type == TileType.RightTopCorner;
            var hasBottomBorder = type == TileType.TopHeadedT || type == TileType.Horizontal || type == TileType.LeftBottomCorner ||
                                  type == TileType.RightBottomCorner;

            var leftBorderX = game.TrackTileSize*selfWayPointX;
            var rightBorderX = game.TrackTileSize*(1 + selfWayPointX);
            var topBorderY = (game.TrackTileSize)*selfWayPointY;
            var bottomBorderY = game.TrackTileSize*(1 + selfWayPointY);

            if (self.X - leftBorderX <= self.Width/2 + 30 && self.Y - topBorderY <= self.Width/2 + 30 &&
                Math.Abs(self.GetAngleTo(leftBorderX, topBorderY)) > Math.PI/2 + Math.PI/6 ||
                rightBorderX - self.X <= self.Width/2 + 30 && self.Y - topBorderY <= self.Width/2 + 30 &&
                Math.Abs(self.GetAngleTo(rightBorderX, topBorderY)) > Math.PI/2 + Math.PI/6 ||
                self.X - leftBorderX <= self.Width/2 + 30 && bottomBorderY - self.Y <= self.Width/2 + 30 &&
                Math.Abs(self.GetAngleTo(leftBorderX, bottomBorderY)) > Math.PI/2 + Math.PI/6 ||
                rightBorderX - self.X <= self.Width/2 + 30 && bottomBorderY - self.Y <= self.Width/2 + 30 &&
                Math.Abs(self.GetAngleTo(rightBorderX, bottomBorderY)) > Math.PI/2 + Math.PI/6)
            {
                return false;
            }

            if (hasLeftBorder)
            {
                var borderX = game.TrackTileSize*selfWayPointX + game.TrackTileMargin;
                if (self.X - borderX <= self.Width/2 + 30 && Math.Abs(self.GetAngleTo(borderX, self.Y)) > Math.PI/2 + Math.PI/6)
                {
                    return false;
                }
            }

            if (hasRightBorder)
            {
                var borderX = game.TrackTileSize*(1 + selfWayPointX) - game.TrackTileMargin;
                if (borderX - self.X <= self.Width/2 + 30 && Math.Abs(self.GetAngleTo(borderX, self.Y)) > Math.PI/2 + Math.PI/6)
                {
                    return false;
                }
            }

            if (hasTopBorder)
            {
                var borderY = (game.TrackTileSize)*selfWayPointY + game.TrackTileMargin;
                if (self.Y - borderY <= self.Width/2 + 30 && Math.Abs(self.GetAngleTo(self.X, borderY)) > Math.PI/2 + Math.PI/6)
                {
                    return false;
                }
            }

            if (hasBottomBorder)
            {
                var borderY = game.TrackTileSize*(1 + selfWayPointY) - game.TrackTileMargin;
                if (borderY - self.Y <= self.Width/2 + 30 && Math.Abs(self.GetAngleTo(self.X, borderY)) > Math.PI/2 + Math.PI/6)
                {
                    return false;
                }
            }

            return true;
        }

        private bool NeedBorderGoBack(
            double distToGoBack,
            double angleToGoBack,
            int selfWayPointX,
            int selfWayPointY,
            World world,
            Game game,
            Car self)
        {
            //if (self.EnginePower < 0)
            //{            
            //    if (selfSpeed < maxSpeedToGoForward)    
            //        return true;
            //    else
            //    {
            //        return false;
            //    }

            //}

            //if (selfSpeed > maxSpeedToGoBack)
            //{
            //    return false;
            //}
            var type = world.TilesXY[selfWayPointX][selfWayPointY];
            var hasLeftBorder = type == TileType.LeftBottomCorner || type == TileType.LeftTopCorner ||
                                type == TileType.RightHeadedT || type == TileType.Vertical;
            var hasRightBorder = type == TileType.RightBottomCorner || type == TileType.RightTopCorner ||
                                 type == TileType.LeftHeadedT || type == TileType.Vertical;
            var hasTopBorder = type == TileType.BottomHeadedT || type == TileType.Horizontal || type == TileType.LeftTopCorner ||
                               type == TileType.RightTopCorner;
            var hasBottomBorder = type == TileType.TopHeadedT || type == TileType.Horizontal || type == TileType.LeftBottomCorner ||
                                  type == TileType.RightBottomCorner;

            var leftBorderX = game.TrackTileSize*selfWayPointX;
            var rightBorderX = game.TrackTileSize*(1 + selfWayPointX);
            var topBorderY = (game.TrackTileSize)*selfWayPointY;
            var bottomBorderY = game.TrackTileSize*(1 + selfWayPointY);

            if (self.X - leftBorderX < distToGoBack && self.Y - topBorderY < distToGoBack &&
                Math.Abs(self.GetAngleTo(leftBorderX, topBorderY)) < Math.PI/6 ||
                rightBorderX - self.X < distToGoBack && self.Y - topBorderY < distToGoBack &&
                Math.Abs(self.GetAngleTo(rightBorderX, topBorderY)) < Math.PI/6 ||
                self.X - leftBorderX < distToGoBack && bottomBorderY - self.Y < distToGoBack &&
                Math.Abs(self.GetAngleTo(leftBorderX, bottomBorderY)) < Math.PI/6 ||
                rightBorderX - self.X < distToGoBack && bottomBorderY - self.Y < distToGoBack &&
                Math.Abs(self.GetAngleTo(rightBorderX, bottomBorderY)) < Math.PI/6)
            {
                return true;
            }


            if (hasLeftBorder)
            {
                var borderX = game.TrackTileSize*selfWayPointX + game.TrackTileMargin;
                if (self.X - borderX < distToGoBack && Math.Abs(self.GetAngleTo(borderX, self.Y)) < angleToGoBack)
                {
                    return true;
                }
            }

            if (hasRightBorder)
            {
                var borderX = game.TrackTileSize*(1 + selfWayPointX) - game.TrackTileMargin;
                if (borderX - self.X < distToGoBack && Math.Abs(self.GetAngleTo(borderX, self.Y)) < angleToGoBack)
                {
                    return true;
                }
            }

            if (hasTopBorder)
            {
                var borderY = (game.TrackTileSize)*selfWayPointY + game.TrackTileMargin;
                if (self.Y - borderY < distToGoBack && Math.Abs(self.GetAngleTo(self.X, borderY)) < angleToGoBack)
                {
                    return true;
                }
            }

            if (hasBottomBorder)
            {
                var borderY = game.TrackTileSize*(1 + selfWayPointY) - game.TrackTileMargin;
                if (borderY - self.Y < distToGoBack && Math.Abs(self.GetAngleTo(self.X, borderY)) < angleToGoBack)
                {
                    return true;
                }
            }

            return false;
        }

        private bool CanShoot(Car self, World world, Game game, Square selfSquare, Square[,] table, IEnumerable<Point> allSquares)
        {
            foreach (var car in world.Cars)
            {
                

                if (car.IsTeammate || car.IsFinishedTrack || car.Durability == 0d) continue;
                if (Math.Abs(self.AngularSpeed) > 1) continue;

                if (self.GetDistanceTo(car) < game.TrackTileSize / 2 &&
                    Math.Abs(self.GetAngleTo(car)) < Math.PI / 60 &&
                    (Math.Sqrt(car.SpeedX * car.SpeedX + car.SpeedY * car.SpeedY) < 7
                    || Math.Abs(self.Angle - car.Angle) <= Math.PI / 12 || Math.Abs(self.Angle - car.Angle) >= 11 * Math.PI / 12))
                {
                    return true;
                }

                if (Math.Abs(self.GetAngleTo(car)) > Math.PI/90) continue;
                if (self.GetDistanceTo(car) > 2*game.TrackTileSize) continue;
                if (Math.Abs(self.Angle - car.Angle) > Math.PI/12 && Math.Abs(self.Angle - car.Angle) < 11 * Math.PI / 12 ) continue;
                return true;
            }

            return false;
        }

        private double GetXBoder(Square selfSquare, Car self, World world, Game game)
        {
            var currentWayPointX = (int) (selfSquare.X/game.TrackTileSize);
            if (self.SpeedX > 0)
            {
                var isStopTile = selfSquare.Type == TileType.LeftHeadedT || selfSquare.Type == TileType.Vertical ||
                                 selfSquare.Type == TileType.RightTopCorner || selfSquare.Type == TileType.RightBottomCorner;

                if (!isStopTile)
                {
                    var selfWayPointY = (int) (selfSquare.Y/game.TrackTileSize);
                    currentWayPointX++;
                    var nextTileType = world.TilesXY[currentWayPointX][selfWayPointY];

                    while (nextTileType != TileType.LeftHeadedT && nextTileType != TileType.Vertical &&
                           nextTileType != TileType.RightTopCorner && nextTileType != TileType.RightBottomCorner)
                    {
                        currentWayPointX++;
                        nextTileType = world.TilesXY[currentWayPointX][selfWayPointY];
                    }
                }
            }

            else
            {
                var isStopTile = selfSquare.Type == TileType.RightHeadedT || selfSquare.Type == TileType.Vertical ||
                                 selfSquare.Type == TileType.LeftTopCorner || selfSquare.Type == TileType.LeftBottomCorner;

                if (!isStopTile)
                {
                    var selfWayPointY = (int) (selfSquare.Y/game.TrackTileSize);
                    currentWayPointX--;
                    var nextTileType = world.TilesXY[currentWayPointX][selfWayPointY];

                    while (nextTileType != TileType.RightHeadedT && nextTileType != TileType.Vertical &&
                           nextTileType != TileType.LeftTopCorner && nextTileType != TileType.LeftBottomCorner)
                    {
                        currentWayPointX--;
                        nextTileType = world.TilesXY[currentWayPointX][selfWayPointY];
                    }
                }
            }

            return self.SpeedX > 0 ? (currentWayPointX + 1)*game.TrackTileSize : currentWayPointX*game.TrackTileSize;
        }

        private double GetYBoder(Square selfSquare, Car self, World world, Game game)
        {
            var currentWayPointY = (int) (selfSquare.Y/game.TrackTileSize);
            if (self.SpeedY > 0)
            {
                var isStopTile = selfSquare.Type == TileType.TopHeadedT || selfSquare.Type == TileType.Horizontal ||
                                 selfSquare.Type == TileType.RightBottomCorner || selfSquare.Type == TileType.LeftBottomCorner;

                if (!isStopTile)
                {
                    var selfWayPointX = (int) (selfSquare.X/game.TrackTileSize);
                    currentWayPointY++;
                    var nextTileType = world.TilesXY[selfWayPointX][currentWayPointY];

                    while (nextTileType != TileType.TopHeadedT && nextTileType != TileType.Horizontal &&
                           nextTileType != TileType.RightBottomCorner && nextTileType != TileType.LeftBottomCorner)
                    {
                        currentWayPointY++;
                        nextTileType = world.TilesXY[selfWayPointX][currentWayPointY];
                    }
                }
            }

            else
            {
                var isStopTile = selfSquare.Type == TileType.BottomHeadedT || selfSquare.Type == TileType.Horizontal ||
                                 selfSquare.Type == TileType.RightTopCorner || selfSquare.Type == TileType.LeftTopCorner;

                if (!isStopTile)
                {
                    var selfWayPointX = (int) (selfSquare.X/game.TrackTileSize);
                    currentWayPointY--;
                    var nextTileType = world.TilesXY[selfWayPointX][currentWayPointY];

                    while (nextTileType != TileType.BottomHeadedT && nextTileType != TileType.Horizontal &&
                           nextTileType != TileType.RightTopCorner && nextTileType != TileType.LeftTopCorner)
                    {
                        currentWayPointY--;
                        nextTileType = world.TilesXY[selfWayPointX][currentWayPointY];
                    }
                }
            }

            return self.SpeedY > 0 ? (currentWayPointY + 1)*game.TrackTileSize : currentWayPointY*game.TrackTileSize;
        }

        private Point GetLastDiagonalSquare(IList<Point> path, Car self)
        {
            var startWaypointX = (path[0] as Square).X;
            var startWaypointY = (path[0] as Square).Y;

            if (path.Count < 2) return path[0];

            var counter = 1;
            var nextWaypointX = (path[counter] as Square).X;
            var nextWaypointY = (path[counter] as Square).Y;
            var nextTilePosition = nextWaypointX == startWaypointX ? NextTilPosition.Vertical : NextTilPosition.Horizontal;
            counter++;

            while (counter < path.Count)
            {
                var newWaypointX = (path[counter] as Square).X;
                var newWaypointY = (path[counter] as Square).Y;
                var newTilePosition = nextWaypointX == newWaypointX ? NextTilPosition.Vertical : NextTilPosition.Horizontal;

                if (newTilePosition == nextTilePosition || (newWaypointX - nextWaypointX)*self.SpeedX < 0 ||
                    (newWaypointY - nextWaypointY)*self.SpeedY < 0) break;

                nextWaypointX = newWaypointX;
                nextWaypointY = newWaypointY;
                nextTilePosition = newTilePosition;

                counter++;
            }

            return path[counter - 1];
        }

        private int GetLastHorizontalSquareIndex(Car self, IList<Point> path, Game game)
        {
            var startWaypointY = (path[0] as Square).Y;
            int firstDifferntYWaypointIndex = 1;

            while (firstDifferntYWaypointIndex < path.Count)
            {
                if ((path[firstDifferntYWaypointIndex] as Square).Y != startWaypointY) break;
                if (firstDifferntYWaypointIndex >= 2)
                {
                    if (((path[firstDifferntYWaypointIndex] as Square).X - (path[firstDifferntYWaypointIndex - 1] as Square).X)*
                        ((path[firstDifferntYWaypointIndex - 1] as Square).X - (path[firstDifferntYWaypointIndex - 2] as Square).X) <
                        0)
                        break;
                }

                firstDifferntYWaypointIndex++;
            }

            return firstDifferntYWaypointIndex - 1;
            //if ((path.First() as Square).X < (path.Last() as Square).X)
            //{
            //    return (lastSameYWaypoint as Square).X + game.TrackTileSize;
            //}
            //else
            //{
            //    return (lastSameYWaypoint as Square).X;
            //}
        }

        private int GetLastVerticalSquareIndex(Car self, IList<Point> path, Game game)
        {
            var startWaypointX = (path[0] as Square).X;
            int firstDifferntXWaypointIndex = 1;

            while (firstDifferntXWaypointIndex < path.Count)
            {
                if ((path[firstDifferntXWaypointIndex] as Square).X != startWaypointX) break;
                if (firstDifferntXWaypointIndex >= 2)
                {
                    if (((path[firstDifferntXWaypointIndex] as Square).Y - (path[firstDifferntXWaypointIndex - 1] as Square).Y)*
                        ((path[firstDifferntXWaypointIndex - 1] as Square).Y - (path[firstDifferntXWaypointIndex - 2] as Square).Y) <
                        0)
                        break;
                }

                firstDifferntXWaypointIndex++;
            }

            return firstDifferntXWaypointIndex - 1;
            //if ((path.First() as Square).Y < (path.Last() as Square).Y)
            //{
            //    return (lastSameXWaypoint as Square).Y + game.TrackTileSize;
            //}
            //else
            //{
            //    return (lastSameXWaypoint as Square).Y;
            //}        
        }

        private IList<Point> GetExtendedPath(IList<Point> path, MovingType movingType)
        {
            var extendedPath = new List<Point>(path);
            var lastPoint = path.Last();
            var newNeighbours = (lastPoint as Square).Neighbors.Where(x => !extendedPath.Contains(x));
            var neighbour = newNeighbours.Count() == 1 ? newNeighbours.Single() : null;
            if (movingType == MovingType.Horizontal)
            {
                while (neighbour != null && (neighbour as Square).Y == (lastPoint as Square).Y)
                {
                    extendedPath.Add(neighbour);
                    lastPoint = neighbour;
                    newNeighbours = (lastPoint as Square).Neighbors.Where(x => !extendedPath.Contains(x));
                    neighbour = newNeighbours.Count() == 1 ? newNeighbours.Single() : null;
                }
            }
            else //вертикальное движение
            {
                while (neighbour != null && (neighbour as Square).X == (lastPoint as Square).X)
                {
                    extendedPath.Add(neighbour);
                    lastPoint = neighbour;
                    newNeighbours = (lastPoint as Square).Neighbors.Where(x => !extendedPath.Contains(x));
                    neighbour = newNeighbours.Count() == 1 ? newNeighbours.Single() : null;
                }
            }
            return extendedPath;
        }


        private bool Is180DegreesTurn(IList<Point> path, Point lastLinePoint)
        {
            var index = path.IndexOf(lastLinePoint);
            if (index == 0) return false;
            if ((path[index - 1] as Square).Y == (path[index + 2] as Square).Y)
            {
                if ((path[index] as Square).Y != (path[index - 1] as Square).Y && (path[index + 1] as Square).Y != (path[index - 1] as Square).Y)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if ((path[index - 1] as Square).X == (path[index + 2] as Square).X)
            {
                if ((path[index] as Square).X != (path[index - 1] as Square).X && (path[index + 1] as Square).X != (path[index - 1] as Square).X)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            return false;
        }

        private bool IsInPath(double x, double y, IList<Point> path, Game game, Car self)
        {
            return path.Any(p => IsInSquare(p as Square, x, y, game, self));
        }

        private bool IsInSquare(Square square, double x, double y, Game game, Car self)
        {
            var topBorder = square.Y;
            var leftBorder = square.X;
            var rightBorder = square.X + game.TrackTileSize;
            var bottomBorder = square.Y + game.TrackTileSize;

            switch (square.Type)
            {
                case TileType.BottomHeadedT:
                    topBorder = square.Y + game.TrackTileMargin + game.CarHeight/2;
                    break;
                case TileType.Crossroads:
                    var isInLeftTopBorder = (x - game.CarHeight/2 - square.X)*(x - game.CarHeight/2 - square.X) +
                                            (y - game.CarHeight/2 - square.Y)*(y - game.CarHeight/2 - square.Y) <=
                                            game.TrackTileMargin*game.TrackTileMargin &&
                                            Math.Abs(self.GetAngleTo(square.X, square.Y)) < Math.PI/6;
                    var isInRightTopBorder = (x + game.CarHeight/2 - square.X - game.TrackTileSize)*
                                             (x + game.CarHeight/2 - square.X - game.TrackTileSize) +
                                             (y - game.CarHeight/2 - square.Y)*(y - game.CarHeight/2 - square.Y) <=
                                             game.TrackTileMargin*game.TrackTileMargin &&
                                             Math.Abs(self.GetAngleTo(square.X + game.TrackTileSize, square.Y)) < Math.PI/6;
                    var isInLeftBottomBorder = (x - game.CarHeight/2 - square.X)*(x - game.CarHeight/2 - square.X) +
                                               (y + game.CarHeight/2 - square.Y - game.TrackTileSize)*
                                               (y + game.CarHeight/2 - square.Y - game.TrackTileSize) <=
                                               game.TrackTileMargin*game.TrackTileMargin &&
                                               Math.Abs(self.GetAngleTo(square.X, square.Y + game.TrackTileSize)) < Math.PI/6;
                    var isInRightBottomBorder = (x + game.CarHeight/2 - square.X - game.TrackTileSize)*
                                                (x + game.CarHeight/2 - square.X - game.TrackTileSize) +
                                                (y + game.CarHeight/2 - square.Y - game.TrackTileSize)*
                                                (y + game.CarHeight/2 - square.Y - game.TrackTileSize) <=
                                                game.TrackTileMargin*game.TrackTileMargin &&
                                                Math.Abs(
                                                    self.GetAngleTo(square.X + game.TrackTileSize, square.Y + game.TrackTileSize)) <
                                                Math.PI/6;
                    return x >= leftBorder && x <= rightBorder && y >= topBorder && y <= bottomBorder &&
                           !isInLeftTopBorder && !isInRightTopBorder && !isInLeftBottomBorder && !isInRightBottomBorder;

                case TileType.Empty:
                    return false;
                case TileType.Horizontal:
                    topBorder = square.Y + game.TrackTileMargin + game.CarHeight/2;
                    bottomBorder = square.Y + game.TrackTileSize - game.TrackTileMargin - game.CarHeight/2;
                    break;
                case TileType.LeftBottomCorner:
                    bottomBorder = square.Y + game.TrackTileSize - game.TrackTileMargin - game.CarHeight/2;
                    leftBorder = square.X + game.TrackTileMargin + game.CarHeight/2;
                    break;
                case TileType.LeftHeadedT:
                    rightBorder = square.X + game.TrackTileSize - game.TrackTileMargin - game.CarHeight/2;
                    break;
                case TileType.LeftTopCorner:
                    leftBorder = square.X + game.TrackTileMargin + game.CarHeight/2;
                    topBorder = square.Y + game.TrackTileMargin + game.CarHeight/2;
                    break;
                case TileType.RightBottomCorner:
                    rightBorder = square.X + game.TrackTileSize - game.TrackTileMargin - game.CarHeight/2;
                    bottomBorder = square.Y + game.TrackTileSize - game.TrackTileMargin - game.CarHeight/2;
                    break;
                case TileType.RightHeadedT:
                    leftBorder = square.X + game.TrackTileMargin + game.CarHeight/2;
                    break;
                case TileType.RightTopCorner:
                    rightBorder = square.X + game.TrackTileSize - game.TrackTileMargin - game.CarHeight/2;
                    topBorder = square.Y + game.TrackTileMargin + game.CarHeight/2;
                    break;
                case TileType.TopHeadedT:
                    bottomBorder = square.Y + game.TrackTileSize - game.TrackTileMargin - game.CarHeight/2;
                    break;
                case TileType.Unknown:
                    return false;
                case TileType.Vertical:
                    leftBorder = square.X + game.TrackTileMargin + game.CarHeight/2;
                    rightBorder = square.X + game.TrackTileSize - game.TrackTileMargin - game.CarHeight/2;
                    break;
            }

            return x >= leftBorder && x <= rightBorder && y >= topBorder && y <= bottomBorder;
        }

        private bool IsDiagonalPath(IList<Point> path, int startIndex = 0)
        {
            return (path[startIndex] as Square).X != (path[startIndex + 2] as Square).X && (path[startIndex] as Square).Y != (path[startIndex + 2] as Square).Y &&
                   (path[startIndex + 1] as Square).X != (path[startIndex + 3] as Square).X && (path[startIndex + 1] as Square).Y != (path[startIndex + 3] as Square).Y &&
                   (path[startIndex] as Square).X != (path[startIndex + 3] as Square).X && (path[startIndex] as Square).Y != (path[startIndex + 3] as Square).Y;

            //_lastSquare != null &&
            //(_lastSquare as Square).X != (path[1] as Square).X && (_lastSquare as Square).Y != (path[1] as Square).Y &&
            //(path[0] as Square).X != (path[2] as Square).X && (path[0] as Square).Y != (path[2] as Square).Y &&
            //(_lastSquare as Square).X != (path[2] as Square).X && (_lastSquare as Square).Y != (path[2] as Square).Y;
        }

        private bool IsDiagonalExit (IList<Point> path)
        {
            return !IsDiagonalPath(path, 0) && _lastSquare != null &&
            (_lastSquare as Square).X != (path[1] as Square).X && (_lastSquare as Square).Y != (path[1] as Square).Y &&
            (path[0] as Square).X != (path[2] as Square).X && (path[0] as Square).Y != (path[2] as Square).Y &&
            (_lastSquare as Square).X != (path[2] as Square).X && (_lastSquare as Square).Y != (path[2] as Square).Y;
        }

        private bool NeedOil(Car self, IList<Point> path, World world)
        {
            if (_lastSquare == null) return false;
            if (_lastSquare.X != (path[1] as Square).X && _lastSquare.Y != (path[1] as Square).Y
                && world.Cars.Any(x => !x.IsTeammate && Math.Abs(self.GetAngleTo(x)) > Math.PI/2 + Math.PI/4))
            {
                return true;
            }

            return false;
        }

        private void SetAngleCost(Square startSquare, Square prevSqaure)
        {         
            foreach (var neighbour in startSquare.Neighbors.Where(n => !Equals(prevSqaure, n) && !n.Angles.ContainsKey(startSquare)))
            {
                var tempCar = new Car(
                    0,
                    0,
                    startSquare.X,
                    startSquare.Y,
                    0,
                    0,
                    startSquare.Angles[prevSqaure],
                    0,
                    0,
                    0,
                    0,
                    0,
                    false,
                    CarType.Buggy,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    false);

                var angle = tempCar.GetAngleTo(neighbour.X, neighbour.Y);
                if (Math.Abs(angle) > Math.PI / 2 + eps)
                {
                    neighbour.AdditionalAngleCoeffs.Add(startSquare, 1000d);
                }
                else if (Math.Abs(Math.Abs(angle)) < Math.PI / 2 - eps)
                {
                    neighbour.AdditionalAngleCoeffs.Add(startSquare, 0d);
                }
                else
                {
                    neighbour.AdditionalAngleCoeffs.Add(startSquare, 2d);
                }

                var resAngle = angle + startSquare.Angles[prevSqaure];
                if (resAngle > Math.PI + eps)
                {
                    resAngle = -2 * Math.PI + resAngle;
                }
                else if (resAngle < - Math.PI - eps)
                {
                    resAngle = 2 * Math.PI + resAngle;
                }

                neighbour.Angles.Add(startSquare, resAngle);            

                SetAngleCost(neighbour, startSquare);
            }           

            
        }

        private Square GetLastDiagonalSquare (IList<Point> path)
        {
            var isDiagonalPath = true;
            var index = -1;
            while (isDiagonalPath)
            {
                index++;
                isDiagonalPath = IsDiagonalPath(path, index);
            }

            return path[index + 1] as Square;
        }

        private bool HasRightBorder (TileType tileType )
        {
            var res = false;
            switch (tileType)
            {
                case TileType.Empty:
                case TileType.LeftHeadedT:
                case TileType.RightBottomCorner:
                case TileType.RightTopCorner:
                case TileType.Vertical:
                    res = true;
                    break;
            }
            return res;
        }

        private bool HasLeftBorder(TileType tileType)
        {
            var res = false;
            switch (tileType)
            {
                case TileType.Empty:
                case TileType.RightHeadedT:
                case TileType.LeftBottomCorner:
                case TileType.LeftTopCorner:
                case TileType.Vertical:
                    res = true;
                    break;
            }
            return res;
        }

        private bool HasTopBorder(TileType tileType)
        {
            var res = false;
            switch (tileType)
            {
                case TileType.Empty:
                case TileType.BottomHeadedT:
                case TileType.LeftTopCorner:
                case TileType.RightTopCorner:
                case TileType.Horizontal:
                    res = true;
                    break;
            }
            return res;
        }

        private bool HasBottomBorder(TileType tileType)
        {
            var res = false;
            switch (tileType)
            {
                case TileType.Empty:
                case TileType.TopHeadedT:
                case TileType.LeftBottomCorner:
                case TileType.RightBottomCorner:
                case TileType.Horizontal:
                    res = true;
                    break;
            }
            return res;
        }
    }

    
}