using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace DotsGame
{
    [TestFixture]
    class MapTests
    {
        [Test]
        public void DoTest() {
            Map map = new Map();
            map.AddPoint(2, 1, PointType.FirstPlayer);
            map.AddPoint(3, 2, PointType.FirstPlayer);
            map.AddPoint(2, 3, PointType.FirstPlayer);
            map.AddPoint(1, 2, PointType.FirstPlayer);
            map.AddPoint(2, 2, PointType.SecondPlayer);
            //
            Assert.AreEqual(map.GetPointInfo(2, 2).IsBlocked, true);
        }
    }
}
