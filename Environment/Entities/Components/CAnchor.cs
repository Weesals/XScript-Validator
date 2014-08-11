using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.Entities.Components {
    public class CAnchor : CTransform {

        public override XMatrix Transform {
            get {
                var transform = base.Transform;
                var pos = transform.Translation;
                var y = Owner.Simulation.Terrain.GetHeightAt(new XVector2(pos.X, pos.Z));
                transform.M42 = y;
                return transform;
            }
            set {
                base.Transform = value;
            }
        }
        
    }
}
