using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;
using RTS4.Environment.Entities.Components;

namespace RTS4.Environment {
    public class World {

        private Simulation simulation;
        private Dictionary<Type, Action<Entities.Components.XComponent>> onComponentAddedListeners = new Dictionary<Type, Action<Entities.Components.XComponent>>();
        private Dictionary<Type, Action<Entities.Components.XComponent>> onComponentRemovedListeners = new Dictionary<Type, Action<Entities.Components.XComponent>>();

        public XReal SeaLevel { get { return simulation.SeaLevel; } set { simulation.SeaLevel = value; } }
        public Terrain Terrain { get { return simulation.Terrain; } }

        public World(Simulation _simulation) {
            simulation = _simulation;
            if (simulation == null) simulation = new Simulation();
            simulation.OnNotifyComponentAdded = c => {
                if (onComponentAddedListeners.ContainsKey(c.GetType())) onComponentAddedListeners[c.GetType()](c);
            };
            simulation.OnNotifyComponentRemoved = c => {
                if (onComponentRemovedListeners.ContainsKey(c.GetType())) onComponentRemovedListeners[c.GetType()](c);
            };
        }

        public void AddComponentListener<T>(Action<T> onComponentAdded, Action<T> onComponentRemoved) where T : XComponent {
            onComponentAddedListeners.Add(typeof(T), c => { onComponentAdded(c as T); });
            onComponentRemovedListeners.Add(typeof(T), c => { onComponentRemoved(c as T); });
            simulation.WithAllComponents<T>(onComponentAdded);
        }

    }
}
