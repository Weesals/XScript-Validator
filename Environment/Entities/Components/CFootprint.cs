using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.Entities.Components {
    public abstract class CFootprint : XComponent {

        public abstract XVector3 Centre { get; }

    }
}
