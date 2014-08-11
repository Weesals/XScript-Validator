using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.Entities.Components {
    public class CTransform : XComponent {

        public virtual XMatrix Transform { get; set; }

        public CTransform() {
            Transform = XMatrix.Identity;
        }

    }
}
