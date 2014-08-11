using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.Entities.Components {
    // TODO: Redo this, have a class that holds a transform, which
    // meshes are attached to
    public class CSquareFootprint : CFootprint {

        public XMatrix Transform;
        public XVector3 Offset { get; private set; }
        public XVector3 Size { get; private set; }

        public override XVector3 Centre {
            get { return XVector3.Transform(Offset, Transform); }
        }

        public CSquareFootprint(XReal xMin, XReal xMax, XReal zMin, XReal zMax) {
            Size = new XVector3(xMax - xMin, 0, zMax - zMin);
            Offset = new XVector3((xMax + xMin) / 2, 0, (zMax + zMin) / 2);
            Transform = XMatrix.Identity;
        }

    }
}
