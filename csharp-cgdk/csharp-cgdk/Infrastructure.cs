using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IPA.AStar
{
    /// <summary>
    /// Инфраструктура (корридоры коммуникаций)
    /// </summary>
    public class Infrastructure
    {
        /// <summary>
        /// Точка размещения ЦПС
        /// </summary>
        public Point Cps { get; private set; }
        /// <summary>
        /// Список каналов
        /// </summary>
        public IEnumerable<IList<Point>> Channels { get; private set; }

        public Infrastructure(Point cps, IEnumerable<IList<Point>> channels)
        {
            Cps = cps;
            Channels = channels;
        }
    }
}
