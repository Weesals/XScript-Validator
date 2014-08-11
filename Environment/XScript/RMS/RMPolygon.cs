using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.XScript.RMS {
    public class RMPolygon {
        public XVector2[] Points;

        public XReal Length {
            get {
                XReal len = 0;
                for (int p = 0; p < Points.Length; ++p) {
                    XVector2 p1 = Points[p], p2 = Points[(p + 1) % Points.Length];
                    len += XVector2.Distance(p1, p2);
                }
                return len;
            }
        }

        // Returns a point along the surface, expected f from 0 to Length
        public XVector2 GetPointAlongSurface(XReal f) {
            for (int p = 0; p < Points.Length; ++p) {
                XVector2 p1 = Points[p], p2 = Points[(p + 1) % Points.Length];
                XReal len = XVector2.Distance(p1, p2);
                if (len > f) {
                    return XVector2.Lerp(p1, p2, f / len);
                }
                f -= len;
            }
            throw new Exception("rmPolygon::GetPointAlongSurface(), f is not in range!");
        }

    }
}
