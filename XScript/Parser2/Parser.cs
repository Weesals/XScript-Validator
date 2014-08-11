using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using XScript.StackBasedVM;
using RTS4.Common;

namespace XScript.Parser2 {
    public static class Parser {

        public enum ErrorLevelE { Info, Warning, Error };

        // Used to store the type of a parameter on the faux stack
        public struct StackType {
            public string Type;
            public int InstructionId;
            public StackType(int id, string type) { Type = type; InstructionId = id; }

            public override string ToString() { return Type + " " + InstructionId; }
        }

        // Called recursively to generate tokens/instructions from a source code string
        public static IEnumerable<string> Parse(string source, ref int sourceI, GrammarElement element, ParseContext context) {
            IEnumerable<string> res = null;
            if (element is GrammarElementA) {
                var elA = element as GrammarElementA;
                int iter = sourceI;
                using (var level = context.GetCurrentContext()) {
                    for (int e = 0; e < elA.Elements.Length; ++e) {
                        var ok = Parse(source, ref iter, elA.Elements[e], context);
                        if (ok == null) {
                            level.RemoveChanges();
                            return null;
                        }
                        if (res == null) res = ok; else res = res.Concat(ok);
                    }
                }
                sourceI = iter;
            } else if (element is GrammarElementO) {
                var elA = element as GrammarElementO;
                for (int e = 0; e < elA.Elements.Length; ++e) {
                    int iter = sourceI;
                    var ok = Parse(source, ref iter, elA.Elements[e], context);
                    if (ok != null) { sourceI = iter; res = ok; break; }
                }
            } else if (element is GrammarElementT) {
                ParserUtil.IteratePastWhitespaceAndComments(source, ref sourceI);
                var elA = element as GrammarElementT;
                var token = elA.TokenType;
                int match = token.Match(source, sourceI);
                if (match < 0) {
                    context.MarkError(sourceI, token);
                    return null;
                }
                sourceI += match;
                return new string[] { token.Name };
            } else if (element is GrammarElementR) {
                var elA = element as GrammarElementR;
                var rule = elA.Rule;

                ParserUtil.IteratePastWhitespaceAndComments(source, ref sourceI);
                int sourceS = sourceI;
                int iter = sourceI;
                bool isImplicit = rule.Name == "Implicit";
                isImplicit = false;
                ParseContext.Level level = new ParseContext.Level();
                if (!isImplicit) {
                    context.PushRule(rule);
                    level = context.GetCurrentContext();
                }
                for (int r = 1; r <= rule.Cardinality.To; ++r) {
                    if (!isImplicit) level.ClearChildren();
                    var ok = Parse(source, ref iter, rule.Element, context);
                    if (ok != null) {
                        if (!isImplicit) {
                            if (rule.Build != null) {
                                SourceIterator sourceIter = new SourceIterator(source, sourceI, iter);
                                rule.Build(level, sourceIter);
                            } else {
                                level.PassThrough();
                            }
                        }
                        if (r >= rule.Cardinality.From) {
                            if (res == null) res = ok; else res = res.Concat(ok);
                            sourceI = iter;
                        }
                    } else break;
                }
                if (res == null && rule.Cardinality.From == 0) {
                    res = new string[] { };
                }
                if (!isImplicit) {
                    context.PopRule(rule);
                    if (res == null) level.RemoveChanges();
                    level.Dispose();
                }
            }
            return res;
        }

        public static InstructionInstance[] Parse(string source) {
            return Parse(source, delegate(string message, ErrorLevelE level, int line) {
                switch(level) {
                    case ErrorLevelE.Info: Console.WriteLine("Compile Info(" + line + "): " + message); break;
                    case ErrorLevelE.Warning: Console.WriteLine("Compile Warning(" + line + "): " + message); break;
                    case ErrorLevelE.Error: Console.Error.WriteLine("Compile Error(" + line + "): " + message); break;
                }
            });
        }
        public static InstructionInstance[] Parse(string source, Action<string, ErrorLevelE, int> onError) {
            int iter = 0;
            ParseContext context = new ParseContext();
            // Try to parse the code into rough tokens/instructions
            var res = Parse(source, ref iter, ParserSetup.BlockRule, context);
            ParserUtil.IteratePastWhitespaceAndComments(source, ref iter);
            // Was there more code that couldnt be parsed?
            if (res == null || iter < source.Length) {
                int line = source.Substring(0, context.MaxParseChar).Count(c => c == '\n') + 1;
                string lineData = null;
                int at = context.MaxParseChar;
                while (at > 0 && source[at - 1] != '\n') --at;
                int end = source.IndexOf('\n', at);
                while (end > 0 && char.IsWhiteSpace(source[end - 1])) --end;
                while (at < end && char.IsWhiteSpace(source[at])) ++at;
                if (end == -1) end = source.Length;
                lineData = source.Substring(at, end - at);
                onError(
                    "Unable to parse XScript at line " + line + " byte " + context.MaxParseChar + "\r\n" +
                    " " + lineData + "\r\n" +
                    " " + new string(Enumerable.Repeat(' ', context.MaxParseChar - at).ToArray()) + "^\r\n" +
                    "Expected:\r\n" +
                    context.ExpectedTokens.Select(k => "  As <" + k.Key.ToString() + ">:\r\n" +
                        "\t" + k.Value.Select(v => v.ToString()).Aggregate((s1, s2) => s1 + ", " + s2) + "\r\n"
                    ).Aggregate((s1, s2) => s1 + s2),
                    ErrorLevelE.Error,
                    line
                );
            }
            // Ensure no temporary instructions are still present (starting with '+')
            var parsedInstrs = context.GetCurrentContext().Children;
            for (int i = 0; i < parsedInstrs.Count; ++i) {
                var instr = parsedInstrs[i];
                if (instr.Command.StartsWith("+")) {
                    switch (instr.Command) {
                        case "+Error": onError((string)instr.Value, ErrorLevelE.Error, instr.Location.GetLineNumber(source)); break;
                        case "+Info": onError((string)instr.Value, ErrorLevelE.Info, instr.Location.GetLineNumber(source)); break;
                        case "+Warning": onError((string)instr.Value, ErrorLevelE.Warning, instr.Location.GetLineNumber(source)); break;
                        default: {
                            onError("Unexpected token " + instr.Command, ErrorLevelE.Error, instr.Location.GetLineNumber(source));
                        } break;
                    }
                }
            }
            // Begin matching internal instructions to tokens/instructions extracted
            // from the source string
            List<InstructionInstance> instrList = new List<InstructionInstance>();
            {
                var instrSet = StackBasedVM.Instructions.Set;
                List<StackType> stack = new List<StackType>();
                // Helper method to insert instructions into the instrList array
                // without putting them at the end
                Action<int, InstructionInstance> insertInstruction = (at, instr) => {
                    instrList.Insert(at, instr);
                    for (int s = 0; s < stack.Count; ++s) {
                        if (stack[s].InstructionId >= at) {
                            var stackT = stack[s];
                            stackT.InstructionId++;
                            stack[s] = stackT;
                        }
                    }
                };
                // Where to read instructions from
                for (int i = 0; i < parsedInstrs.Count; ++i) {
                    var instr = parsedInstrs[i];
                    // We get the instruction which best matches the parameters
                    // available on the stack, when the instruction is invoked
                    // (for type safety)
                    Instruction bestInstr = null;
                    int bestScore = 0;
                    foreach (var iSet in instrSet) {
                        var instrP = iSet.Value;
                        // Must match names
                        if (instrP.Name != instr.Command) continue;
                        // Calculate a score for how well the parameters match
                        int score = 100;
                        for (int p = 0; p < instrP.ConsumptionValues.Length; ++p) {
                            string paramType = instrP.ConsumptionValues[p];
                            string stackType = stack[stack.Count - 1 - p].Type;
                            score = score * ParserUtil.CompareConversion(stackType, paramType) / 100;
                            if (score <= bestScore) break;
                        }
                        // Is this score better than our previous score?
                        if (score <= bestScore) continue;
                        bestScore = score;
                        bestInstr = instrP;
                    }
                    // No instruction was found for this token/instruction
                    if (bestInstr == null) {
                        onError("Unable to find instruction for " + instr + "\r\n" +
                            (bestInstr != null ?
                                " Best match expected (" + bestInstr.ConsumptionValues.Aggregate((s1, s2) => s1 + ", " + s2) +
                                ") but was given (" + stack.Skip(stack.Count - bestInstr.ConsumptionValues.Length).Select(v => v.Type).Aggregate((s1, s2) => s1 + ", " + s2) + ")"
                            :
                                " Using params (" + stack.Skip(Math.Max(stack.Count - 3, 0)).Select(v => v.Type).Aggregate((s1, s2) => s1 + ", " + s2) + ")"
                            ),
                            ErrorLevelE.Error,
                            instr.Location.GetLineNumber(source)
                        );
                        break;
                    } else {
                        // Are any of the parameters available on the stack incorrect?
                        // Do they need to be cast to anoter type? (ie. float to int)
                        for (int p = 0; p < bestInstr.ConsumptionValues.Length; ++p) {
                            var stackT = stack[stack.Count - 1 - p];
                            var toConvInstr = instrList[stackT.InstructionId];
                            string paramType = bestInstr.ConsumptionValues[p];
                            string stackType = stackT.Type;
                            if (paramType == stackType) continue;
                            if (ParserUtil.StripStars(paramType) == "var") continue;
                            // Constants can be changed at compile-time
                            if (toConvInstr.Instruction.Name == "Constant") {
                                string type = ParserUtil.GetCleanTypeName(toConvInstr.Data.GetType().Name);
                                Debug.Assert(stackT.Type == type,
                                    "Stack type and constant value type must match!");
                                if (paramType == "int") {
                                    toConvInstr.Data = Convert.ToInt32(toConvInstr.Data);
                                    continue;
                                } else if (paramType == "real") {
                                    toConvInstr.Data = (XReal)Convert.ToSingle(toConvInstr.Data);
                                    continue;
                                }
                            }
                            // Otherwise fall back to the more expensive run-time conversion
                            int score = ParserUtil.CompareConversion(stackType, paramType);
                            if (score < 100) {
                                var convInstr = instrSet.FirstOrDefault(i2 => i2.Value.Type == paramType && i2.Value.Name == "Conform").Value;
                                if (convInstr == null) {
                                    onError("Unable to find conversion from " + stackType + " to " + paramType,
                                        ErrorLevelE.Warning,
                                        instr.Location.GetLineNumber(source));
                                    continue;
                                }
                                insertInstruction(stackT.InstructionId + 1, new InstructionInstance() {
                                    Instruction = convInstr,
                                    Data = stackType + "2" + paramType
                                });
                            }
                        }
                        // Simulate executing the instruction to maintain our
                        // faux stack, so next instructions can get the correct
                        // instruction for the expected types
                        Debug.Assert(bestInstr != null,
                            "Unable to find matching instruction!");
                        int toRemove = bestInstr.ConsumptionValues.Length;
                        string retVal = bestInstr.ReturnValues.Length > 0 ? bestInstr.ReturnValues[0] : "void";
                        int retStars = ParserUtil.CountStars(retVal);
                        if (retStars > 0) retVal = retVal.Substring(0, retVal.Length - retStars);
                        if (retVal == "var") {
                            retVal = ParserUtil.StripStars(instr.Type);
                            if (retVal == "auto") retVal = "var";
                        }
                        if (retVal == "var" && toRemove > 0)
                            retVal = ParserUtil.StripStars(stack[stack.Count - 1].Type);
                        else if (retStars > 0) retVal = ParserUtil.AddStars(retVal, retStars);
                        if (toRemove > 0) stack.RemoveRange(stack.Count - toRemove, toRemove);
                        if (retVal != "void") stack.Add(new StackType(instrList.Count, retVal));

                        instrList.Add(new InstructionInstance() {
                            Instruction = bestInstr,
                            Data = instr.Value,
                        });
                    }
                }
                Debug.Assert(stack.Count == 0,
                    "Stack is not balanced!");
            }
            // Our parser wraps the entire file in a block, remove this block
            // so that global variables are not cleaned up after execution
            if(instrList.Count > 0) {
                Debug.Assert(instrList[0].Instruction.Name == "BlockStart",
                    "Script should be contained within a block");
                Debug.Assert(instrList[instrList.Count - 1].Instruction.Name == "BlockEnd",
                    "Script should be contained within a block");
                instrList.RemoveAt(0);
                instrList.RemoveAt(instrList.Count - 1);
            }
            return instrList.ToArray();
            //context.BreakOnError = true;
            //res = Parse(str, ref iter, blockRule, context);
        }

    }
}
