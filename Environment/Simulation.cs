using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;
using RTS4.Environment.Entities;
using RTS4.Environment.Utility;

namespace RTS4.Environment {
    public class Simulation {

        public XReal SeaLevel;
        public Terrain Terrain;
        public Registry<XEntity> objects;

        public Action<Entities.Components.XComponent> OnNotifyComponentAdded;
        public Action<Entities.Components.XComponent> OnNotifyComponentRemoved;

        public Simulation() {
            Terrain = new Terrain();
            objects = new Registry<XEntity>();
        }

        public void AddObject(XEntity entity) {
            objects.CreateEntry(entity);
            objects.ActivateEntry(entity);
            entity.Load(this);
        }
        public void RemoveObject(XEntity entity) {
            entity.Unload();
            objects.DeactivateEntry(entity);
        }


        public void NotifyComponentAdded(Entities.Components.XComponent component) {
            if (OnNotifyComponentAdded != null) OnNotifyComponentAdded(component);
        }

        public void NotifyComponentRemoved(Entities.Components.XComponent component) {
            if (OnNotifyComponentRemoved != null) OnNotifyComponentRemoved(component);
        }

        internal void WithAllComponents<T>(Action<T> with) where T : Entities.Components.XComponent {
            for (int o = 0; o < objects.Count; ++o) {
                foreach (var component in objects[o].GetComponents<T>()) {
                    with(component);
                }
            }
        }
    }
}
