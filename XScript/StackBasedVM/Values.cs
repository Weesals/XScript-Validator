using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;

namespace XScript.StackBasedVM {
    public abstract class FunctionValue {
        public abstract void Invoke(VirtualMachine.Runtime runtime, object[] parms);
    }

    public class FDelegate : FunctionValue {
        public Func<VirtualMachine.Runtime, object[], object> OnInvoke;

        public FDelegate(Func<VirtualMachine.Runtime, object[], object> onInvoke) {
            OnInvoke = onInvoke;
        }

        public override void Invoke(VirtualMachine.Runtime runtime, object[] parms) {
            Debug.Assert(OnInvoke != null,
                "Cant invoke nothing!");
            var res = OnInvoke(runtime, parms);
            runtime.Stack.Push(res);
        }
    }

    public class FInternal : FunctionValue {
        public struct Parameter {
            public string Name;
            public object Default;
        }
        public string Name;
        public Parameter[] Parameters;

        public override void Invoke(VirtualMachine.Runtime runtime, object[] parms) {
            runtime.PushBlock();
            int parmC = parms.Length;
            // TODO: set these as variables in the block!
            for (int p = 0; p < Parameters.Length; ++p) {
                if (p < parmC) {
                    runtime.SetValue(Parameters[p].Name, parms[p]);
                } else {
                    runtime.SetValue(Parameters[p].Name, Parameters[p].Default);
                }
            }
            runtime.Stack.Push(runtime.ProgramCounter);
            runtime.Jump(Name + "_start");
        }
    }

    public class ValueBinding {
        public object Object;
        public PropertyInfo Property;

        public ValueBinding(PropertyInfo property, object obj) {
            Property = property;
            Object = obj;
        }

        public object GetValue() {
            return Property.GetValue(Object, null);
        }
        public void SetValue(object obj) {
            Property.SetValue(Object, obj, null);
        }
    }
}
