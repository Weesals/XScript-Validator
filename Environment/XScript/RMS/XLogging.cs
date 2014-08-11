using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.XScript.RMS {
    public class XLogging {
        public void rmEchoError(string echoString, int level) {
            Console.WriteLine("-Error: " + (level != 0 ? level + ": " : "") + echoString);
        }
        public void rmEchoInfo(string echoString, int level) {
            Console.WriteLine("-Info: " + (level != 0 ? level + ": " : "") + echoString);
        }
        public void rmEchoWarning(string echoString, int level) {
            Console.WriteLine("-Warning: " + (level != 0 ? level + ": " : "") + echoString);
        }

        public void rmSetStatusText(string status, XReal progress) {
            Console.WriteLine("-Progress " + progress * 100 + "%: " + status);
        }
    }
}
