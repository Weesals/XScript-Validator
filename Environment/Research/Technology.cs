using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using RTS4.Environment.Entities;

namespace RTS4.Environment.Research {

    public enum TechnologyType { Normal, Power };

    public class Technology {

        public string Name { get; private set; }
        public TechnologyType Type { get; private set; }
        public bool AllowMultiExecution = false;

        public List<Effect> Effects = new List<Effect>();

        private int useCount = 0;

        public Technology(string name, TechnologyType type) {
            Name = name;
            Type = type;
        }

        public void AddEffect(Effect effect) {
            Debug.Assert(useCount == 0,
                "Cannot add effects after a tech has been executed!");
            Effects.Add(effect);
        }


        public void Apply(InteractionContext context) {
            foreach (var effect in Effects) effect.Apply(context);
        }
        public void Unapply(InteractionContext context) {
            foreach (var effect in Effects) effect.Unapply(context);
        }

    }
}
