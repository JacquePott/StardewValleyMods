using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MassProduction
{
    [Serializable]
    public class SavedMPMInfo
    {
        public string LocationName;
        public int CoordinateX;
        public int CoordinateY;
        public string UpgradeKey;

        public Vector2 GetCoordinates()
        {
            return new Vector2(CoordinateX, CoordinateY);
        }
    }
}
