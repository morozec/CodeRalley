using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.AStar
{
    /// <summary>
    /// Матрица распростарения
    /// </summary>
    public class ExpansionMatrixConteiner
    {
        /// <summary>
        /// Стоимости прохода до точек сети
        /// </summary>
        public IDictionary<Point, double> ExpansionMatrix { get; set; }
        ///// <summary>
        ///// Оптимальные пути прохода до точек сети
        ///// </summary>
        //public IDictionary<Point, IList<Point>> Path { get; set; } 
    }
}
