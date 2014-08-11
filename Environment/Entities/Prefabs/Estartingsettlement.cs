using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;
using RTS4.Environment.Entities.Components;

namespace RTS4.Environment.Entities.Prefabs {
    public class Estartingsettlement : XEntity {

        public Estartingsettlement() {
            AddComponent(new CModel() { Model = "TownCentre" });
            AddComponent(new CSquareFootprint((XReal)(-2.5f), (XReal)2.5f, (XReal)(-2.5f), (XReal)2.5f) { });
            AddComponent(new CAnchor() { });
            AddComponent(new CSelectable() { Name = "Settlement" });
        }

    }
}
