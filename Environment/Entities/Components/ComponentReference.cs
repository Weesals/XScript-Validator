using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace RTS4.Environment.Entities.Components {
    public struct ComponentReference<T> where T : XComponent {

        XEntity entity;

        public ComponentReference(XEntity _entity) {
            entity = _entity;
        }
        public ComponentReference(XComponent _other) {
            entity = _other.Owner;
        }

        public static implicit operator T(ComponentReference<T> compRef) {
            Debug.Assert(compRef.entity.GetComponents<T>().Count() <= 1);
            return compRef.entity.GetComponent<T>();
        }
        public static T operator ~(ComponentReference<T> compRef) {
            return (T)compRef;
        }

    }
}
