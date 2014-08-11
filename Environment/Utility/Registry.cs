using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using RTS4.Environment.Utility;

namespace RTS4.Environment.Utility {
    public class Registry<T> where T : IIdentificationNumber {

        private List<T> entitiesById = new List<T>();
        private List<T> activeEntities = new List<T>();
        private List<T> inactiveEntities = new List<T>();

        public int Count { get { return activeEntities.Count; } }
        public T this[int i] {
            get { return activeEntities[i]; }
        }

        public void CreateEntry(T entity) {
            lock (entitiesById) {
                entity.SetId(entitiesById.Count);
                entitiesById.Add(entity);
            }
        }

        public void ActivateEntry(T entity) {
            Debug.Assert(!activeEntities.Contains(entity),
                "Entity is already active");
            activeEntities.Add(entity);
            inactiveEntities.Remove(entity);
        }

        public void DeactivateEntry(T entity) {
            Debug.Assert(activeEntities.Contains(entity),
                "Entity must be active before it can be deactivated");
            activeEntities.Remove(entity);
            inactiveEntities.Add(entity);
        }

        public void CloneFrom(Registry<T> registry) {
            throw new NotImplementedException();
        }

    }
}
