using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.XScript.RMS {
    public class RMArea {

        public class TerrainLayer {
            public string TerrainType;
            public XReal MinDist, MaxDist;
        }

        public string Name;
        public RMArea Parent;
        public bool WarnOnFailure = true;
        public XVector2 Position = new XVector2(XReal.NaN, XReal.NaN);
        public XReal BaseHeight = XReal.NaN;
        public XReal Size { get { return (MinSize + MaxSize) / 2; } set { MinSize = MaxSize = value; } }
        public XReal MinSize, MaxSize;
        public int InfluenceBlurDistance;
        public XReal Coherence;
        public int HeightBlendDistance;
        public bool TerrainLayerVariance = true;
        public string CliffType;
        public List<RMConstraint> Constraints = new List<RMConstraint>();

        public bool Built = false;
        public XRectangle ActiveArea;

        public List<TerrainLayer> TerrainLayers = new List<TerrainLayer>();
    }

}
