using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Environment.Resources;

namespace RTS4.Environment.Entities.Components {
    public class CModel : XComponent {

        public XModel Model { get; set; }

        public CTransform Transform {
            get { return Owner.GetComponent<CTransform>(); }
        }

    }
}
