using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using System.Globalization;
using RTS4.Common;

namespace XScript.StackBasedVM {
    public class VirtualMachine {

        public class Runtime {

            public class VariableAddress {
                public string Name;
                public VariableAddress(string name) { Name = name; }
            }

            public class Block {
                public Dictionary<string, object> values = new Dictionary<string, object>();
            }
            private List<Block> blocks = new List<Block>();
            public Stack<object> Stack = new Stack<object>();

            public object SPop() { return Stack.Pop(); }
            public T SPop<T>() {
                var val = SPop();
                return (T)(val);
            }
            public void SPush(object o) { Stack.Push(o); }

            private InstructionInstance[] program;
            private int programCounter;
            public int ProgramCounter { get { return programCounter; } }

            public Runtime(InstructionInstance[] _program) {
                program = _program;
                programCounter = 0;
                //PushBlock();
            }

            public void Allocate(string p, string name) {
                Debug.Assert(blocks.Count > 0,
                    "Cant add variables without a block!");
                blocks[blocks.Count - 1].values.Add(name, null);
            }
            public VariableAddress GetAddress(string name) {
                return new VariableAddress(name);
            }
            public object GetValue(VariableAddress addr) {
                for (int b = blocks.Count - 1; b >= 0; --b) {
                    if (blocks[b].values.ContainsKey(addr.Name)) {
                        var varObj = blocks[b].values[addr.Name];
                        if (varObj is ValueBinding) return (varObj as ValueBinding).GetValue();
                        else return varObj;
                    }
                }
                Debug.Assert(false,
                    "Unable to find variable " + addr.Name + "!");
                return null;
            }
            public void SetValue(string name, object val) {
                SetValue(GetAddress(name), val);
            }
            public void SetValue(VariableAddress addr, object val) {
                for (int b = blocks.Count - 1; b >= 0; --b) {
                    if (blocks[b].values.ContainsKey(addr.Name)) {
                        var varObj = blocks[b].values[addr.Name];
                        if (varObj is ValueBinding) (varObj as ValueBinding).SetValue(val);
                        else varObj = val;
                        blocks[b].values[addr.Name] = varObj;
                        return;
                    }
                }
                blocks[0].values.Add(addr.Name, val);
            }

            public void SetFunction(string name, MethodInfo method) {
                SetFunction(name, method, null);
            }
            public void SetFunction(string name, MethodInfo method, object owner) {
                SetValue(name, new FDelegate((vm, prms) => {
                    var methodParams = method.GetParameters();
                    if (prms.Length != methodParams.Length) {
                        Debug.Assert(prms.Length < methodParams.Length);
                        var newPrms = new object[methodParams.Length];
                        for (int i = 0; i < prms.Length; ++i) newPrms[i] = prms[i];
                        for (int i = prms.Length; i < newPrms.Length; ++i) {
                            var defVal = methodParams[i].DefaultValue;
                            if (defVal != DBNull.Value) newPrms[i] = defVal;
                        }
                        prms = newPrms;
                    }
                    for (int p = 0; p < prms.Length; ++p) {
                        if (prms[p] != null && prms[p].GetType() != methodParams[p].GetType()) {
                            if (methodParams[p].ParameterType == typeof(XReal)) {
                                if (prms[p] is XReal) continue;
                                else if (prms[p] is int) prms[p] = (XReal)(int)prms[p];
                                else if (prms[p] is float) prms[p] = (XReal)(float)prms[p];
                                else if (prms[p] is string) prms[p] = XReal.Parse((string)prms[p]);
                                else throw new InvalidCastException();
                            } else {
                                prms[p] = Convert.ChangeType(prms[p], methodParams[p].ParameterType);
                            }
                        }
                    }
                    try {
                        return method.Invoke(owner, prms);
                    } catch (Exception e) {
                        string error = "Error invoking method " + method.Name;
                        if (prms.Length > 0)
                            error += " with paramters (" +
                                prms.Select(p => p != null ? p.ToString() : "").Aggregate((s1, s2) => s1 + ", " + s2) + ")";
                        error += ", exception thrown:\r\n" + e;
                        Console.WriteLine(error);
                        return null;
                    }
                }));
            }
            public void AddMethodsFrom(object bag) {
                foreach (var method in bag.GetType().GetMethods()) {
                    if ((method.Attributes & MethodAttributes.SpecialName) == 0 &&
                        method.DeclaringType != typeof(object))
                    {
                        SetFunction(method.Name, method, bag);
                    }
                }
            }
            public void AddPropertiesFrom(object bag) {
                foreach (var property in bag.GetType().GetProperties()) {
                    SetValue(property.Name, new ValueBinding(property, bag));
                }
            }

            public void PushBlock() {
                blocks.Add(new Block());
            }

            public void PopBlock() {
                blocks.RemoveAt(blocks.Count - 1);
            }


            public void Step() {
                if (programCounter < program.Length)
                    program[programCounter++].Invoke(this);
            }
            public bool Complete {
                get { return programCounter >= program.Length; }
            }

            public void Jump(string marker) {
                for (int p = 0; p < program.Length; ++p) {
                    if (program[p].Instruction.Name == "Marker" && program[p].Data.Equals(marker)) {
                        programCounter = p;
                        return;
                    }
                }
                Debug.Assert(false,
                    "Unknown marker!");
            }
            public void Jump(int pc) {
                programCounter = pc;
            }


            public void Invoke(string name, int parmC) {
                var fnObj = GetValue(GetAddress(name));
                if (fnObj is FunctionValue) {
                    object[] parms = new object[parmC];
                    for (int p = parms.Length - 1; p >= 0; --p) {
                        parms[p] = Stack.Pop();
                    }
                    (fnObj as FunctionValue).Invoke(this, parms);
                } else if (fnObj != null) {
                    object[] parms = new object[parmC];
                    for (int p = parms.Length - 1; p >= 0; --p) {
                        parms[p] = Stack.Pop();
                    }
                    Console.WriteLine("Unable to find function " + name);
                    Stack.Push(1);
                }
            }


            public bool FunctionExists(string fnName) {
                var fnObj = GetValue(GetAddress(fnName));
                if (fnObj is FunctionValue) return true;
                if (fnObj is string) return true;
                return false;
            }

            public void ReadConstantsFrom(string fileName) {
                XDocument doc = XDocument.Load(fileName);
                foreach (var val in doc.Element("constants").Elements("value")) {
                    string name = val.Attribute("name").Value;
                    string valS = val.Attribute("value").Value;
                    if (valS.StartsWith("0x")) SetValue(name, int.Parse(valS.Substring(2), NumberStyles.HexNumber));
                    else {
                        int v;
                        if (int.TryParse(valS, out v)) SetValue(name, v);
                        else SetValue(name, v);
                    }
                }
            }
        }

    }
}
