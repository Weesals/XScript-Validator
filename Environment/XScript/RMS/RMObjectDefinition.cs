using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTS4.Common;
using RTS4.Environment.Entities;
using RTS4.Environment.Entities.Components;

namespace RTS4.Environment.XScript.RMS {
    public class RMObjectDefinition {

        public string ObjectName;
        public RMClass ParentClass;
        public XReal MinDistance;
        public XReal MaxDistance;

        public List<XEntity> PlacedObjects;

        public RMObjectDefinition(string name) {
            ObjectName = name;
        }


        public XEntity PlaceObjectAt(XVector2 pos) {
            string prefabName = "E" + ObjectName.Replace(" ", "");
            Type prefab = Type.GetType("RTS4.Environment.Entities.Prefabs." + prefabName);
            if (prefab == null) {
                Console.WriteLine("Unable to find prefab \"" + prefabName + "\"");
                return null;
            }
            XEntity entity = prefab.GetConstructor(new Type[] { })
                .Invoke(new object[] { }) as XEntity;
            var transform = entity.GetComponent<CTransform>();
            if (transform != null) transform.Transform = XMatrix.CreateTranslation(new XVector3(pos.X, 0, pos.Y));
            if (PlacedObjects == null) PlacedObjects = new List<XEntity>();
            PlacedObjects.Add(entity);
            return entity;
        }
    }
}
