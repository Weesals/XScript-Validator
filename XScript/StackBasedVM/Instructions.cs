using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using VariableAddress = XScript.StackBasedVM.VirtualMachine.Runtime.VariableAddress;
using System.Diagnostics;
using XScript.Parser2;
using RTS4.Common;

namespace XScript.StackBasedVM {
    public class Instruction {
        public delegate void InvokeDel(VirtualMachine.Runtime r, object data);
        public delegate R BoolOperDel<R, T>(T t1, T t2);

        public string Type;
        public string Name;
        public string[] ReturnValues;
        public string[] ConsumptionValues;

        public InvokeDel Invoke;

        public Instruction(string type, string name, string[] returnValues, string[] consumpValues, InvokeDel invoke) {
            Type = type;
            Name = name;
            ReturnValues = returnValues;
            ConsumptionValues = consumpValues;
            Invoke = invoke;
        }
        public Instruction(string type, string name, string returnValue, string[] consumpValues, InvokeDel invoke) : this(type, name, new [] { returnValue }, consumpValues, invoke) { }

        public static Instruction From<T>(string name, BoolOperDel<T, T> invoke) {
            return From<T, T>(name, invoke);
        }
        public static Instruction From<R, T>(string name, BoolOperDel<R, T> invoke) {
            string retType = ParserUtil.GetCleanTypeName(typeof(R).Name);
            string type = ParserUtil.GetCleanTypeName(typeof(T).Name);
            var instr = new Instruction(type, name, retType, new[] { type, type }, null);
            instr.SetInvoke(invoke);
            return instr;
        }

        public void SetInvoke<R, T>(BoolOperDel<R, T> invoke) {
            Invoke = (r, d) => {
                T v1 = r.SPop<T>(), v2 = r.SPop<T>();
                r.SPush(invoke(v2, v1));
            };
        }

        public override string ToString() { return Type + " " + Name; }
    }

    public class InstructionInstance {
        public Instruction Instruction;
        public object Data;

        public void Invoke(VirtualMachine.Runtime runtime) {
            if (Instruction != null) Instruction.Invoke(runtime, Data);
        }

        public override string ToString() { return Instruction + "(" + Data + ")"; }
    }

    public static class Instructions {

        public static Dictionary<string, Instruction> Set = new Dictionary<string, Instruction>();

        static void AddInstruction(Instruction instruction) {
            Set.Add(instruction.Type + "_" + instruction.Name, instruction);
        }

        static Instructions() {
            /* Special types:
             * var: match any type
             * auto: match any type, use it like a template (use the same type for ie. return and param)
             */
            // An alias to the AddInstruction method, so lines can be shorter
            Action<Instruction> addInstr = AddInstruction;
            string[] empty = new string[] { };

            addInstr(new Instruction("bool", "Allocate", empty, empty,
                (r, d) => { r.Allocate("bool", (string)d); }));
            addInstr(new Instruction("bool", "Conform", "bool", new[] { "var" }, (r, d) => {
                r.Stack.Push(Convert.ToBoolean(r.Stack.Pop()));
            }));
            addInstr(Instruction.From<bool>("And", (v1, v2) => { return v1 && v2; }));
            addInstr(Instruction.From<bool>("Or", (v1, v2) => { return v1 || v2; }));

            addInstr(new Instruction("int", "Allocate", empty, empty,
                (r, d) => { r.Allocate("int", (string)d); }));
            addInstr(Instruction.From<int>("Add", (v1, v2) => v1 + v2 ));
            addInstr(Instruction.From<int>("Sub", (v1, v2) => v1 - v2 ));
            addInstr(Instruction.From<int>("Mul", (v1, v2) => v1 * v2 ));
            addInstr(Instruction.From<int>("Div", (v1, v2) => v1 / v2 ));
            addInstr(Instruction.From<bool, int>("Less", (v1, v2) => v1 < v2 ));
            addInstr(Instruction.From<bool, int>("Greater", (v1, v2) => v1 > v2 ));
            addInstr(Instruction.From<bool, int>("LEqual", (v1, v2) => v1 <= v2 ));
            addInstr(Instruction.From<bool, int>("GEqual", (v1, v2) => v1 >= v2 ));
            addInstr(new Instruction("int", "Conform", "int", new [] { "var" }, (r, d) => {
                r.Stack.Push(Convert.ToInt32(r.Stack.Pop()));
            }));

            addInstr(new Instruction("real", "Allocate", empty, empty,
                (r, d) => { r.Allocate("real", (string)d); }));
            addInstr(Instruction.From<XReal>("Add", (v1, v2) => v1 + v2 ));
            addInstr(Instruction.From<XReal>("Sub", (v1, v2) => v1 - v2));
            addInstr(Instruction.From<XReal>("Mul", (v1, v2) => v1 * v2));
            addInstr(Instruction.From<XReal>("Div", (v1, v2) => v1 / v2));
            addInstr(Instruction.From<bool, XReal>("Less", (v1, v2) => v1 < v2));
            addInstr(Instruction.From<bool, XReal>("Greater", (v1, v2) => v1 > v2));
            addInstr(Instruction.From<bool, XReal>("LEqual", (v1, v2) => v1 <= v2));
            addInstr(Instruction.From<bool, XReal>("GEqual", (v1, v2) => v1 >= v2));
            addInstr(new Instruction("real", "Conform", "real", new[] { "var" }, (r, d) => {
                object val = r.Stack.Peek();
                if (val is XReal) return;
                else if (val is int) r.Stack.Push((XReal)r.SPop<int>());
                else if (val is float) r.Stack.Push((XReal)r.SPop<float>());
                else if (val is string) r.Stack.Push(XReal.Parse(r.SPop<string>()));
            }));

            addInstr(new Instruction("string", "Allocate", empty, empty,
                (r, d) => { r.Allocate("string", (string)d); }));
            addInstr(Instruction.From<string>("Add", (v1, v2) => v1 + v2 ));
            addInstr(Instruction.From<int, string>("Sub", (v1, v2) => v1.CompareTo(v2) ));
            addInstr(Instruction.From<bool, string>("Less", (v1, v2) => v1.CompareTo(v2) < 0 ));
            addInstr(Instruction.From<bool, string>("Greater", (v1, v2) => v1.CompareTo(v2) > 0 ));
            addInstr(Instruction.From<bool, string>("LEqual", (v1, v2) => v1.CompareTo(v2) <= 0 ));
            addInstr(Instruction.From<bool, string>("GEqual", (v1, v2) => v1.CompareTo(v2) >= 0 ));
            addInstr(new Instruction("string", "Conform", "string", new[] { "var" }, (r, d) => {
                r.Stack.Push(Convert.ToString(r.Stack.Pop()));
            }));
            /*
            addInstr(Instruction.From<object>("Add", (v1, v2) =>
                MiscUtil.Operator.Add(v1, v2) ));
            addInstr(Instruction.From<object>("Sub", (v1, v2) =>
                MiscUtil.Operator.Subtract(v1, v2) ));
            addInstr(Instruction.From<object>("Mul", (v1, v2) =>
                MiscUtil.Operator.Multiply(v1, v2) ));
            addInstr(Instruction.From<object>("Div", (v1, v2) =>
                MiscUtil.Operator.Divide(v1, v2) ));
            addInstr(Instruction.From<bool, object>("Less", (v1, v2) =>
                MiscUtil.Operator.LessThan(v1, v2) ));
            addInstr(Instruction.From<bool, object>("Greater", (v1, v2) =>
                MiscUtil.Operator.GreaterThan(v1, v2) ));
            addInstr(Instruction.From<bool, object>("LEqual", (v1, v2) =>
                MiscUtil.Operator.LessThanOrEqual(v1, v2) ));
            addInstr(Instruction.From<bool, object>("GEqual", (v1, v2) =>
                MiscUtil.Operator.GreaterThanOrEqual(v1, v2) ));
            */
            addInstr(new Instruction("fn", "Allocate", empty, empty,
                (r, d) => { r.Allocate("fn", (string)d); }));

            string var1 = "var";

            /*addInstr(new Instruction("var", "Add", "var", new[] { "var", "var" },
                (r, d) => { r.Stack.Push(MiscUtil.Operator.Add(r.Stack.Pop(), r.Stack.Pop())); }));
            addInstr(new Instruction("var", "Mul", "var", new[] { "var", "var" },
                (r, d) => { r.Stack.Push(MiscUtil.Operator.Multiply(r.Stack.Pop(), r.Stack.Pop())); }));
            addInstr(new Instruction("var", "Sub", "var", new[] { "var", "var" },
                (r, d) => { r.Stack.Push(MiscUtil.Operator.Subtract(r.Stack.Pop(), r.Stack.Pop())); }));
            addInstr(new Instruction("var", "Div", "var", new[] { "var", "var" },
                (r, d) => { r.Stack.Push(MiscUtil.Operator.Divide(r.Stack.Pop(), r.Stack.Pop())); }));
            */
            addInstr(new Instruction("var", "Pop", empty, new[] { "var" },
                (r, d) => { r.Stack.Pop(); }));
            addInstr(new Instruction("var*", "Pop", empty, new[] { "var*" },
                (r, d) => { r.Stack.Pop(); }));
            addInstr(new Instruction("var", "IsEqual", "bool", new[] { var1, var1 },
                (r, d) => { r.Stack.Push(r.Stack.Pop().Equals(r.Stack.Pop())); }));
            addInstr(new Instruction("var", "Reference", "var*", empty,
                (r, d) => { r.Stack.Push(r.GetAddress((string)d)); }));
            addInstr(new Instruction("var", "Variable", "var", empty,
                (r, d) => { r.Stack.Push(r.GetValue(r.GetAddress((string)d))); }));
            addInstr(new Instruction("var", "Constant", "var", empty,
                (r, d) => { r.Stack.Push(d); }));
            addInstr(new Instruction("var", "Deference", var1, new[] { var1 + "*" },
                (r, d) => { r.Stack.Push(r.GetValue((VariableAddress)r.Stack.Pop())); }));
            addInstr(new Instruction("var", "Assign", var1, new[] { var1, "var*" },
                (r, d) => { var val = r.Stack.Pop(); var addr = (VariableAddress)r.Stack.Pop(); r.SetValue(addr, val); r.Stack.Push(val); }));

            addInstr(new Instruction("var", "Call0", "var", empty,
                (r, d) => { r.Invoke((string)d, 0); }));
            addInstr(new Instruction("var", "Call1", "var", new[] { "var" },
                (r, d) => { r.Invoke((string)d, 1); }));
            addInstr(new Instruction("var", "Call2", "var", new[] { "var", "var" },
                (r, d) => { r.Invoke((string)d, 2); }));
            addInstr(new Instruction("var", "Call3", "var", new[] { "var", "var", "var" },
                (r, d) => { r.Invoke((string)d, 3); }));
            addInstr(new Instruction("var", "Call4", "var", new[] { "var", "var", "var", "var" },
                (r, d) => { r.Invoke((string)d, 4); }));
            addInstr(new Instruction("var", "Call5", "var", new[] { "var", "var", "var", "var", "var" },
                (r, d) => { r.Invoke((string)d, 5); }));
            addInstr(new Instruction("var", "Call6", "var", new[] { "var", "var", "var", "var", "var", "var" },
                (r, d) => { r.Invoke((string)d, 6); }));
            addInstr(new Instruction("var", "Call7", "var", new[] { "var", "var", "var", "var", "var", "var", "var" },
                (r, d) => { r.Invoke((string)d, 7); }));
            addInstr(new Instruction("var", "Call8", "var", new[] { "var", "var", "var", "var", "var", "var", "var", "var" },
                (r, d) => { r.Invoke((string)d, 8); }));
            addInstr(new Instruction("var", "Call9", "var", new[] { "var", "var", "var", "var", "var", "var", "var", "var", "var" },
                (r, d) => { r.Invoke((string)d, 9); }));

            addInstr(new Instruction("void", "JumpIfFalse", empty, new[] { "bool" },
                (r, d) => { if ((bool)r.Stack.Pop() == false) r.Jump((string)d); }));
            addInstr(new Instruction("void", "Jump", empty, new[] { "var" },
                (r, d) => { var pos = r.Stack.Pop();
                    if (pos is string) r.Jump((string)pos);
                    else if (pos is int) r.Jump((int)pos);
                    else Debug.Assert(false, "Unknown jump type!");
                }));
            addInstr(new Instruction("void", "JumpTo", empty, empty,
                (r, d) => {
                    if (d is string) r.Jump((string)d);
                    else if (d is int) r.Jump((int)d);
                    else Debug.Assert(false, "Unknown jump type!");
                }));
            addInstr(new Instruction("void", "Marker", empty, empty,
                (r, d) => { }));
            addInstr(new Instruction("void", "BlockStart", empty, empty,
                (r, d) => { r.PushBlock(); }));
            addInstr(new Instruction("void", "BlockEnd", empty, empty,
                (r, d) => { r.PopBlock(); }));

            addInstr(new Instruction("fn", "Return", empty, new[] { "int" },
                (r, d) => {
                    var val = r.Stack.Pop();
                    var loc = r.Stack.Pop();
                    r.Stack.Push(val);
                    if (val is string) r.Jump((string)loc);
                    else if (val is int) r.Jump((int)loc);
                    else Debug.Assert(false, "Unable to return!");
                    r.PopBlock();
                }));
            addInstr(new Instruction("fn", "Function", "fn", empty,
                (r, d) => { r.Stack.Push(d); r.Jump(((FInternal)d).Name + "_end"); }));
        }

    }
}
