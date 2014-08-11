using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.XScript.RMS {
    public class XMath {

        public XReal pi { get { return (XReal)Math.PI; } }

        public XReal abs(XReal v) { return XReal.Abs(v); }
        public XReal sqrt(XReal v) { return XReal.Sqrt(v); }
        public XReal pow(XReal v, XReal p) { return XReal.Pow(v, p); }

        public XReal sin(XReal v) { return XReal.Sin(v); }
        public XReal cos(XReal v) { return XReal.Cos(v); }
        public XReal tan(XReal v) { return XReal.Tan(v); }
        public XReal asin(XReal v) { return XReal.Asin(v); }
        public XReal acos(XReal v) { return XReal.Acos(v); }
        public XReal atan(XReal v) { return XReal.Atan(v); }
        public XReal atan2(XReal y, XReal x) { return XReal.Atan2(y, x); }

    }
}
