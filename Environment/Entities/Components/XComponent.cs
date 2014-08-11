using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Environment.Utility;
using System.Diagnostics;

namespace RTS4.Environment.Entities.Components {
    public class XComponent : IIdentificationNumber {

        XEntity owner;

        public int Id { get; private set; }
        public bool Loaded { get { return owner != null; } }
        public XEntity Owner { get { return owner; } }
        public Simulation Simulation { get { return owner.Simulation; } }

        public void SetId(int id) {
            Id = id;
        }

        public virtual void Load(XEntity _owner) {
            Debug.Assert(_owner.Simulation != null,
                "Owner is not loaded yet?");
            owner = _owner;
            Simulation.NotifyComponentAdded(this);
        }
        public virtual void Unload() {
            Debug.Assert(owner.Simulation != null,
                "Owner was unloaded too soon! (or twice?)");
            Simulation.NotifyComponentRemoved(this);
            owner = null;
        }

        public T GetComponent<T>() where T : XComponent {
            return Owner != null ? Owner.GetComponent<T>() : null;
        }

    }
}
