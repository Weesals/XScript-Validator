using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTS4.Environment.XScript.RMS {
    public class RMClass {

        public string Name;
        public List<RMArea> Areas = new List<RMArea>();

        public RMClass(string name) { Name = name; }

    }
}
