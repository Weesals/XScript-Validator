using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;

namespace RTS4.Environment.XScript.RMS {
    public class RMPlayer {

        public string Name;
        public XVector2 Position;
        public int Team;

        public RMArea Area;

        private List<RMResource> resources = new List<RMResource>();

        public RMResource GetResource(string name) {
            var resource = resources.FirstOrDefault(r => r.Name == name);
            if (resource == null) {
                resource = new RMResource() { Name = name, Amount = 0 };
                resources.Add(resource);
            }
            return resource;
        }

    }
}
