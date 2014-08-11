using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTS4.Environment.Resources {
    public class XModel {

        public string Name { get; private set; }

        public XModel(string name) { Name = name; }

        public static implicit operator XModel(string str) {
            return new XModel(str);
        }

        public override string ToString() { return Name; }

    }
}
