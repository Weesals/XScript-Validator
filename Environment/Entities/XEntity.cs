using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Environment.Entities.Components;
using System.Diagnostics;
using RTS4.Environment.Utility;

namespace RTS4.Environment.Entities {
    public class XEntity : IIdentificationNumber {

        Simulation simulation;

        Registry<XComponent> components = new Registry<XComponent>();

        public int Id { get; private set; }
        public bool Loaded { get { return simulation != null; } }
        public Simulation Simulation { get { return simulation; } }

        void IIdentificationNumber.SetId(int id) {
            Debug.Assert(Id <= 0,
                "Object already has an Id");
            Id = id;
        }

        public void AddComponent(XComponent component) {
            components.CreateEntry(component);
            components.ActivateEntry(component);
        }
        public void RemoveComponent(XComponent component) {
            components.DeactivateEntry(component);
        }

        public void Load(Simulation _simulation) {
            simulation = _simulation;
            for (int c = 0; c < components.Count; ++c) components[c].Load(this);
        }
        public void Unload() {
            for (int c = 0; c < components.Count; ++c) components[c].Unload();
            simulation = null;
        }


        public T GetComponent<T>() where T : XComponent {
            for (int c = 0; c < components.Count; ++c) {
                if (components[c] is T) return components[c] as T;
            }
            return null;
        }


        public IEnumerable<T> GetComponents<T>() where T : XComponent {
            for (int c = 0; c < components.Count; ++c) {
                if (components[c] is T) yield return components[c] as T;
            }
        }
    }
}
