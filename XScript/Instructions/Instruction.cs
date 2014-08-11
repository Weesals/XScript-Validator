using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XScript.Instructions {
    public abstract class Value {
        public abstract object GetValue();
        public static Value operator + (Value v1, Value v2) {
            if (v1.GetType() != v2.GetType()) { v1 = v1.As<VString>(); v2 = v2.As<VString>(); }
            if (v1 is VNumber) { return new VNumber((v1 as VNumber).Number + (v2 as VNumber).Number); }
            if (v1 is VString) { return new VString((v1 as VString).String + (v2 as VString).String); }
            throw new Exception("Operator not available for " + v1.GetType().Name);
        }
        public static Value operator -(Value v1, Value v2) {
            if (v1 is VNumber) { return new VNumber((v1 as VNumber).Number - (v2 as VNumber).Number); }
            throw new Exception("Operator not available for " + v1.GetType().Name);
        }
        public static Value operator *(Value v1, Value v2) {
            if (v1 is VNumber) { return new VNumber((v1 as VNumber).Number + (v2 as VNumber).Number); }
            throw new Exception("Operator not available for " + v1.GetType().Name);
        }
        public static Value operator /(Value v1, Value v2) {
            if (v1 is VNumber) { return new VNumber((v1 as VNumber).Number + (v2 as VNumber).Number); }
            throw new Exception("Operator not available for " + v1.GetType().Name);
        }
        public static bool operator ==(Value v1, Value v2) { return v1.Equals(v2); }
        public static bool operator !=(Value v1, Value v2) { return !v1.Equals(v2); }
        public override bool Equals(object obj) {
            if (obj is Value) GetValue().Equals((obj as Value).GetValue());
            return base.Equals(obj);
        }

        public Value As<T>() where T : Value {
            if (GetType() == typeof(T)) return this;
            if (typeof(T) == typeof(VString)) {
                return new VString(Convert.ToString(GetValue()));
            } else if (typeof(T) == typeof(VNumber)) {
                return new VNumber(Convert.ToDouble(GetValue()));
            }
            throw new Exception("Conversion not available for " + GetType().Name);
        }
    }
    public class VNumber : Value {
        public double Number;
        public VNumber() { }
        public VNumber(double num) { Number = num; }
        public override object GetValue() { return Number; }
        public override string ToString() { return Number + "d"; }
    }
    public class VString : Value {
        public string String;
        public VString() { }
        public VString(string str) { String = str; }
        public override object GetValue() { return String; }
        public override string ToString() { return "\"" + String + "\""; }
    }
    public class VFunction : Value {
        public String[] Arguments;
        public Instruction Function;
        public VFunction() { }
        public VFunction(String[] arguments, Instruction func) { Arguments = arguments; Function = func; }
        public override object GetValue() { return "Function"; }
        public override string ToString() { return "fn{" + Function.ToString() + "}"; }
    }

    public class Runtime {
        List<Scope> scopes = new List<Scope>();

        public Value GetVariable(string name) {
            for (int s = scopes.Count - 1; s >= 0; --s) {
                if (scopes[s].HasVariable(name)) return scopes[s].GetVariable(name);
            }
            throw new Exception("Variable " + name + " does not exist in this scope!");
        }
        public void SetVariable(string name, Value value) {
            for (int s = scopes.Count - 1; s >= 0; --s) {
                if (scopes[s].HasVariable(name)) { scopes[s].SetVariable(name, value); break; }
            }
        }

        public void AddScope(Scope scope) { scopes.Add(scope); }
        public void RemoveScope(Scope scope) { scopes.Remove(scope); }
    }
    public class Scope : IDisposable {
        Runtime context;
        public Dictionary<string, Value> values = new Dictionary<string, Value>();
        public void DeclareVariable(string name, Value val) { values.Add(name, val); }
        public void SetVariable(string name, Value val) { values[name] = val; }
        public bool HasVariable(string name) { return values.ContainsKey(name); }
        public Value GetVariable(string name) { return values[name]; }

        public Scope(Runtime _context) {
            context = _context;
            context.AddScope(this);
        }

        public void Dispose() {
            context.RemoveScope(this);
        }
    }

    public abstract class Instruction {
        public abstract Value Invoke(Runtime context);

        public static Instruction FromValue(Value value) {
            return new IConstant() { Value = value };
        }
    }

    public class IVDeclare : Instruction {
        public string Name;
        public string Type;
        public override Value Invoke(Runtime context) {
            throw new Exception("Variables cannot be declared at runtime, must be picked up by a scope.");
        }
        public override string ToString() { return Type + " " + Name; }
    }

    public class IScope : Instruction {
        public String[] Values;
        public Instruction[] Instructions;
        Scope scope;
        public void BeginInvoke(Runtime context) {
            scope = new Scope(context);
            for (int v = 0; v < Values.Length; ++v) scope.DeclareVariable(Values[v], null);
        }
        public Value MidInvoke(Runtime context) {
            Value res = null;
            for (int i = 0; i < Instructions.Length; ++i) {
                res = Instructions[i].Invoke(context);
            }
            return res;
        }
        public void EndInvoke(Runtime context) {
            scope.Dispose();
        }
        public override Value Invoke(Runtime context) {
            try {
                BeginInvoke(context);
                return MidInvoke(context);
            } finally {
                EndInvoke(context);
            }
        }
        public override string ToString() {
            return "(" + (Values.Length >= 1 ? Values.Aggregate((v1, v2) => v1 + ", " + v2) : "") + ")" +
                " { " + (Instructions.Length >= 1 ? Instructions.Select(i => i.ToString()).Aggregate((i1, i2) => i1 + ", " + i2) : "") + " }";
        }
    }

    public class IVariable : Instruction {
        public string Name;
        public override Value Invoke(Runtime context) { return context.GetVariable(Name); }
        public override string ToString() { return Name; }
    }
    public class IConstant : Instruction {
        public Value Value;
        public override Value Invoke(Runtime context) { return Value; }
        public override string ToString() { return Value.ToString(); }
    }
    public class IAssignment : Instruction {
        public string Name;
        public Instruction Value;
        public override Value Invoke(Runtime context) {
            var value = Value.Invoke(context);
            context.SetVariable(Name, value);
            return value;
        }
        public override string ToString() { return Name + "=" + Value.ToString(); }
    }

    public class IAddition : Instruction {
        public Instruction[] Values;
        public override Value Invoke(Runtime context) {
            Value val = Values[0].Invoke(context);
            for (int v = 1; v < Values.Length; ++v) val = val + Values[v].Invoke(context);
            return val;
        }
    }
    public class ISubtraction : Instruction {
        public Instruction[] Values;
        public override Value Invoke(Runtime context) {
            Value val = Values[0].Invoke(context);
            for (int v = 1; v < Values.Length; ++v) val = val - Values[v].Invoke(context);
            return val;
        }
    }
    public class IMultiplication : Instruction {
        public Instruction[] Values;
        public override Value Invoke(Runtime context) {
            Value val = Values[0].Invoke(context);
            for (int v = 1; v < Values.Length; ++v) val = val * Values[v].Invoke(context);
            return val;
        }
    }
    public class IDivision : Instruction {
        public Instruction[] Values;
        public override Value Invoke(Runtime context) {
            Value val = Values[0].Invoke(context);
            for (int v = 1; v < Values.Length; ++v) val = val / Values[v].Invoke(context);
            return val;
        }
    }
    public class IEquals : Instruction {
        public Instruction[] Values;
        public override Value Invoke(Runtime context) {
            Value val = Values[0].Invoke(context);
            for (int v = 1; v < Values.Length; ++v) {
                if (val == Values[v].Invoke(context)) continue;
                return new VNumber(0);
            }
            return new VNumber(1);
        }
    }
    public class INotEquals : Instruction {
        public Instruction[] Values;
        public override Value Invoke(Runtime context) {
            Value val = Values[0].Invoke(context);
            for (int v = 1; v < Values.Length; ++v) {
                if (val != Values[v].Invoke(context)) continue;
                return new VNumber(0);
            }
            return new VNumber(1);
        }
    }

    public class IAPI : Instruction {
        public Func<Runtime, Value> OnInvoke;
        public override Value Invoke(Runtime context) {
            return OnInvoke(context);
        }
    }

    public class ICall : Instruction {
        public string FunctionName;
        public Instruction[] Arguments;

        public override Value Invoke(Runtime context) {
            VFunction fnVar = context.GetVariable(FunctionName) as VFunction;
            if (fnVar == null) {
                throw new Exception("Function is null, unable to invoke");
            }
            using (var scope = new Scope(context)) {
                for (int a = 0; a < fnVar.Arguments.Length; ++a) {
                    scope.SetVariable(fnVar.Arguments[a], Arguments[a].Invoke(context));
                }
                return fnVar.Function.Invoke(context);
            }
        }
        public override string ToString() {
            return FunctionName + "(" + Arguments.Select(i => i.ToString()).Aggregate((s1, s2) => s1 + ", " + s2) + ")";
        }
    }
}
