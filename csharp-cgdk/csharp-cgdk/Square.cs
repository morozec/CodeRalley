using System;
using System.Collections.Generic;
using IPA.AStar;
using Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk.Model;

namespace Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk.AStar
{
    public class Square : Point
    {

        public Dictionary<Square, double> AdditionalAngleCoeffs { get; set; }

        public Dictionary<Square, double> Angles { get; set; }

        private const double Eps = 1E-6;

        /// <summary>
        /// Длина стороны квадрата
        /// </summary>
        public double Side { get; set; }
        /// <summary>
        /// Координата x левого верхнего угла квадрата
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Координата y левого верхнего улга квадрата
        /// </summary>
        public double Y { get; set; }
        /// <summary>
        /// "Вес" квадрата
        /// </summary>
        public double Weight { get; set; }
        /// <summary>
        /// Имя квадрата (для удобства идентификации)
        /// </summary>
        public string Name { get; set; }

        public TileType Type { get; set; }

        public IEnumerable<Square> Neighbors { get; set; } 
       

        public Square(double side, double x, double y, TileType type, double weight, string name)
        {
            Side = side;
            X = x;
            Y = y;
            Type = type;
            Weight = weight;
            Name = name;
            AdditionalAngleCoeffs = new Dictionary<Square, double>();
            Angles = new Dictionary<Square, double>();
        }

        public override IEnumerable<Point> GetNeighbors(IEnumerable<Point> points)
        {
            return Neighbors;
        }

        public override double GetHeuristicCost(Point goal)
        {
            //return GetManhattanDistance(this, (Square)goal);
            return GetEuclidDistance(this, (Square)goal);
            //if (AdditionalAngleCoeffs.ContainsKey(goal as Square))
            //{
            //    res += AdditionalAngleCoeffs[goal as Square];
            //}
            //return res;


        }

        private static double GetEuclidDistance(Square a, Square b)
        {
            return Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
        }

        private static double GetManhattanDistance(Square a, Square b)
        {
            var dx = Math.Abs(a.X - b.X);
            var dy = Math.Abs(a.Y - b.Y);
            return dx + dy;
        }

        public override double GetCost(Point goal, Car self, Game game)
        {           
            var dist = GetEuclidDistance(this, goal as Square);
          

            var angle =
                Math.Abs(
                    self.GetAngleTo((goal as Square).X + game.TrackTileSize/2, (goal as Square).Y + game.TrackTileSize/2));

            if (self.X >= X && self.X <= X + game.TrackTileSize && self.Y >= Y && self.Y <= Y + game.TrackTileSize)
            {
                if (angle > 3 * Math.PI/2) dist *= 100;
                
            }

            dist +=  dist * (1 + angle/2);



            //var cost = (goal as Square).Weight + this.Weight;
            //cost += (goal as Square).AdditionalAngleCoeffs[this];

            return (Weight + (goal as Square).Weight)*dist;           
        }

        public override string ToString()
        {
            return "X: " + X + "; Y: " + Y;
        } 
    }
}
