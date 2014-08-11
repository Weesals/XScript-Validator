using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.XScript.RMS {
    public class RMConstraint {

        public string Name;

        public RMConstraint(string name) { Name = name; }

        public virtual bool IsPointOk(XReal tAreaX, XReal tAreaY) {
            return false;
        }
    }

    public class RMConstraintBox : RMConstraint {

        public XRectangleF Box;

        public RMConstraintBox(string name) : base(name) { }

        public override bool IsPointOk(XReal tAreaX, XReal tAreaY) {
            return Box.Contains(new XVector2(tAreaX, tAreaY));
        }
    }

    public class RMConstraintAreas : RMConstraint {

        // In meters
        public XReal MinDistance = XReal.MinValue, MaxDistance = XReal.MaxValue;
        public IEnumerable<RMArea> AreaProvider;

        public RMAreaMap AreaMap;

        private XReal metersPerTile;

        public RMConstraintAreas(string name, XReal _metersPerTile) : base(name) { metersPerTile = _metersPerTile; }

        public override bool IsPointOk(XReal tAreaX, XReal tAreaY) {
            foreach (var area in AreaProvider) {
                if (!area.Built) continue;
                int significantDist = 0;
                if (MaxDistance != XReal.MaxValue) significantDist = (int)MaxDistance;
                else if (MinDistance != XReal.MinValue) significantDist = (int)MinDistance;
                else throw new Exception("No min or max specified?");
                XReal dist2 = AreaMap.GetDistanceSquaredToArea(
                    new XPoint2((tAreaX / metersPerTile).RoundedToInt, (tAreaY / metersPerTile).RoundedToInt),
                    area, significantDist) * metersPerTile * metersPerTile;
                /*float dist2 =
                    XVector2.DistanceSquared(
                        new XVector2(tAreaX, tAreaY),
                        area.Position
                    );*/
                if (MaxDistance != XReal.MaxValue && (dist2 == int.MaxValue || dist2 > MaxDistance * MaxDistance))
                    return false;
                if (dist2 < MinDistance * MinDistance)
                    return false;
                /*if (dist2 > (MaxDistance + area.Size) * (MaxDistance + area.Size) ||
                    dist2 < (MinDistance + area.Size) * (MinDistance + area.Size)) {
                    return false;
                }*/
            }
            return true;
        }
    }

    public class RMConstraintEdge : RMConstraint {

        public XReal MinDistance = XReal.MinValue;
        public XReal MaxDistance = XReal.MaxValue;
        public RMArea Area;

        public RMAreaMap AreaMap;

        public RMConstraintEdge(string name) : base(name) { }

        public override bool IsPointOk(XReal tAreaX, XReal tAreaY) {
            var area = Area;
            XReal dist =
                XVector2.Distance(
                    new XVector2(tAreaX, tAreaY),
                    area.Position
                );
            XReal distFromEdge = dist - area.Size;
            if (XReal.Abs(distFromEdge) > MaxDistance ||
                XReal.Abs(distFromEdge) < MinDistance) {
                return false;
            }
            return true;
        }
    }
}
