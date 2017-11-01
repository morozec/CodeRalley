using Com.CodeGame.CodeRacing2015.DevKit.CSharpCgdk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.AStar
{
    public abstract class Point : IComparable<Point>
    {
        public double G { get; set; }
        public double H { get; set; }

        public double F
        {
            get
            {
                return G + H;
            }
        }

        public Point CameFromPoint { get; set; }

        public abstract IEnumerable<Point> GetNeighbors(IEnumerable<Point> points);

        public abstract double GetHeuristicCost(Point goal);

        public abstract double GetCost(Point goal, Car self, Game game);

        public int CompareTo(Point other)
        {
            return -F.CompareTo(other.F);
        }
        
    }
}
