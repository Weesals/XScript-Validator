using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using XScript.StackBasedVM;
using RTS4.Common;

namespace XScript.Parser2 {
    public static class ParserSetup {

        // The block rule is used as the root when parsing
        // a file, it references all other rules internally
        static GrammarRule blockRule;
        public static GrammarRule BlockRule {
            get { return blockRule; }
        }

        static ParserSetup() {
            // Create our language grammar
            TokenType emptyToken = new TokenType("~", (str, b) => 0);
            TokenType ifToken = new TokenType("if", "^(if)");
            TokenType elseToken = new TokenType("else", "^(else)");
            TokenType forToken = new TokenType("for", "^(for)");
            TokenType flowControlToken = new TokenType("break", "^(break|continue|return)");
            TokenType paramStartToken = new TokenType("(", "^[(]");
            TokenType paramEndToken = new TokenType(")", "^[)]");
            TokenType blockStartToken = new TokenType("{", "^[{]");
            TokenType blockEndToken = new TokenType("}", "^[}]");
            TokenType typeToken = new TokenType("type", "^(int|float|double|bool|string|void|class|fn|var)");
            TokenType nameToken = new TokenType("name", "^[a-zA-Z_][a-zA-Z0-9_]*");
            TokenType numberToken = new TokenType("const", "^[0-9]+(\\.[0-9]*)?[f]?");
            TokenType stringToken = new TokenType("const", "^\"(\\.|[^\"])*\"");
            TokenType boolToken = new TokenType("const", "^(true|false)");
            TokenType charToken = new TokenType("const", "^['][\\\\]?.[']");
            TokenType commaToken = new TokenType(",", "^[,]");
            TokenType endStatementToken = new TokenType(";", "^[;]");
            TokenType assignToken = new TokenType("=", "^[=]");
            TokenType andOrToken = new TokenType("oper", "^(&&|\\|\\|)");
            TokenType mulDivToken = new TokenType("MulDiv", "^[*/]");
            TokenType addSubToken = new TokenType("AddSub", "^[+\\-]");
            TokenType operatorToken = new TokenType("oper", "^[*+\\-/^]");
            TokenType comparitorToken = new TokenType("compar", "^[<>=][=]?");
            TokenType unaryToken = new TokenType("unary", "^[+-]");
            TokenType increment = new TokenType("incr", "^(\\+\\+|\\-\\-)");

            GrammarRule emptyRule = new GrammarRule("Empty", emptyToken);
            //GrammarRule semicolonRule = new GrammarRule("Semicolon", endStatementToken);
            GrammarRule variableRule = new GrammarRule("Variable");
            GrammarRule constantRule = new GrammarRule("Constant");
            GrammarRule ifRule = new GrammarRule("If");
            GrammarRule forRule = new GrammarRule("For");
            GrammarRule flowControlRule = new GrammarRule("Flow", flowControlToken);
            GrammarRule blockOrStatementRule = new GrammarRule("BOS");
            GrammarRule statementRule = new GrammarRule("Statement");
            GrammarRule assignmentRule = new GrammarRule("Assignment");
            GrammarRule vAssignmentRule = new GrammarRule("VAssignment");
            GrammarRule callRule = new GrammarRule("Call");
            GrammarRule termRule = new GrammarRule("Term");
            GrammarRule equationRule = new GrammarRule("Equation");
            GrammarRule andOrRule = new GrammarRule("AndOr");
            GrammarRule mulDivRule = new GrammarRule("MulDiv");
            GrammarRule addSubRule = new GrammarRule("AddSub");
            GrammarRule compareRule = new GrammarRule("Compare");
            GrammarRule paramRule = new GrammarRule("Parameter");
            GrammarRule parmsRule = new GrammarRule("Parameters");
            GrammarRule vDeclrRule = new GrammarRule("VDeclr");
            GrammarRule fDeclrRule = new GrammarRule("FDeclr");
            blockRule = new GrammarRule("Block");
            GrammarRule rValueRule = new GrammarRule("RValue", equationRule);
            GrammarRule conditionRule = new GrammarRule("Condition", equationRule);

            constantRule.SetElements(numberToken | stringToken | charToken | boolToken);
            variableRule.SetElements(nameToken);        // TODO: ++ operator should be here

            blockOrStatementRule.SetElements((blockStartToken & blockRule & blockEndToken) | (statementRule));
            GrammarRule elseRule = null;
            ifRule.SetElements(ifToken & paramStartToken & conditionRule & paramEndToken &
                blockOrStatementRule &
                ((elseRule = (
                    elseToken & (
                        blockOrStatementRule |
                        ifRule
                    )
                )) | emptyRule)
            );
            GrammarRule forConditionRule = null;
            forRule.SetElements(forToken & paramStartToken &
                (statementRule | emptyRule) &
                (forConditionRule = ((comparitorToken & equationRule) | emptyRule)) &
                paramEndToken &
                blockOrStatementRule
            );
            assignmentRule.SetElements(assignToken & rValueRule);
            vAssignmentRule.SetElements(nameToken & assignmentRule);
            callRule.SetElements(nameToken & paramStartToken & parmsRule & paramEndToken);
            GrammarRule unaryRule = null;
            termRule.SetElements(
                unaryRule = ((unaryToken | emptyRule) & (
                    (increment & variableRule) | (variableRule & increment) |
                    callRule | constantRule | variableRule | (paramStartToken & equationRule & paramEndToken)
                ))
            );
            GrammarRule andOrOperRule = null;
            GrammarRule mulDivOperRule = null;
            GrammarRule addSubOperRule = null;
            GrammarRule compOperRule = null;
            andOrRule.SetElements(compareRule & ((andOrOperRule = andOrToken & andOrRule) | emptyRule));
            mulDivRule.SetElements(termRule & ((mulDivOperRule = mulDivToken & mulDivRule) | emptyRule));
            addSubRule.SetElements(mulDivRule & ((addSubOperRule = addSubToken & addSubRule) | emptyRule));
            compareRule.SetElements(addSubRule & ((compOperRule = comparitorToken & compareRule) | emptyRule));
            equationRule.SetElements(andOrRule);
            paramRule.SetElements(rValueRule);
            parmsRule.SetElements(
                (paramRule & (commaToken & paramRule)[0, 1000]) |
                emptyRule
            );
            vDeclrRule.SetElements(typeToken & nameToken & (assignmentRule | emptyToken));
            GrammarRule pDeclrRule = new GrammarRule("PDeclr");
            GrammarRule pArrRule = new GrammarRule("PArr");
            pDeclrRule.SetElements(typeToken & nameToken & (assignmentRule | emptyToken));
            pArrRule.SetElements(pDeclrRule & ((commaToken & pArrRule) | emptyToken));
            fDeclrRule.SetElements(typeToken & nameToken & paramStartToken &
                (pArrRule | typeToken | emptyRule) &
                paramEndToken & blockStartToken & blockRule & blockEndToken
            );
            GrammarRule nerfReturnRules = (
                (fDeclrRule) |
                (vDeclrRule & endStatementToken) |
                (vAssignmentRule & endStatementToken) |
                (callRule & endStatementToken) |
                (termRule & endStatementToken)
            );
            statementRule.SetElements(
                ifRule |
                forRule |
                (flowControlRule & endStatementToken) |
                nerfReturnRules
            );
            blockRule.SetElements(new GrammarRule(statementRule)[0, 1000]);

            int uid = 0;

            // Setup how these grammars are built into instructions
            unaryRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string unary = source.TryMatch(unaryToken);
                context.PassThrough();
                if (unary == "-") {
                    context.Store.Add(new InstructionItem("Constant", "int", -1, source.From));
                    context.Store.Add(new InstructionItem("Mul", "auto", "unary", source.From));
                }
            };
            constantRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string data = source.SourceCode;
                if (numberToken.Match(data, 0) >= 0) {
                    if (data.Contains('.'))
                        context.Store.Add(new InstructionItem("Constant", "real", XReal.Parse(data), source.From));
                    else
                        context.Store.Add(new InstructionItem("Constant", "int", int.Parse(data), source.From));
                } else if (stringToken.Match(data, 0) >= 0) {
                    context.Store.Add(new InstructionItem("Constant", "string", ParserUtil.UnescapeString(data), source.From));
                } else if (boolToken.Match(data, 0) >= 0) {
                    context.Store.Add(new InstructionItem("Constant", "bool", data == "true", source.From));
                } else if (charToken.Match(data, 0) >= 0) {
                    context.Store.Add(new InstructionItem("Constant", "char", data[1], source.From));
                }
            };
            variableRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                context.Store.Add(new InstructionItem("Variable", "auto", source.Match(nameToken), source.From));
            };
            vDeclrRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string type = ParserUtil.GetCleanTypeName(source.Match(typeToken));
                string name = source.Match(nameToken);
                context.Store.Add(new InstructionItem("+Allocate", type, name, source.From));
                context.Store.Add(new InstructionItem("Reference", type + "*", name, source.From));     // Needs to be here because it gets popped if not used
                if (context.Children.Count > 0) {
                    context.PassThrough();
                    //context.Store.Add(new InstructionItem("Conform", type, type));
                    context.Store.Add(new InstructionItem("Assign", type, "~" + name, source.From));
                }
            };
            nerfReturnRules.Build += delegate(ParseContext.Level context, SourceIterator source) {
                context.PassThrough();
                context.Store.Add(new InstructionItem("Pop", "void", null, source.From));
            };
            vAssignmentRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string name = source.Match(nameToken);
                context.Store.Add(new InstructionItem("Reference", "auto*", name, source.From));
                context.PassThrough();
                //context.Store.Add(new InstructionItem("Conform", "auto", "auto", source.From));
                context.Store.Add(new InstructionItem("Assign", "auto", "~" + name, source.From));
            };
            Func<string, int, InstructionItem> GetInstructionFromString = (oper, loc) => {
                switch (oper) {
                    case "&&": return (new InstructionItem("And", "bool", oper, loc));
                    case "||": return (new InstructionItem("Or", "bool", oper, loc));
                    case "<=": return (new InstructionItem("LEqual", "bool", oper, loc));
                    case ">=": return (new InstructionItem("GEqual", "bool", oper, loc));
                    case "<": return (new InstructionItem("Less", "bool", oper, loc));
                    case ">": return (new InstructionItem("Greater", "bool", oper, loc));
                    case "==": return (new InstructionItem("IsEqual", "bool", oper, loc));
                    case "!=": return (new InstructionItem("IsNEqual", "bool", oper, loc));
                    case "+": return (new InstructionItem("Add", "auto", oper, loc));
                    case "-": return (new InstructionItem("Sub", "auto", oper, loc));
                    case "*": return (new InstructionItem("Mul", "auto", oper, loc));
                    case "/": return (new InstructionItem("Div", "auto", oper, loc));
                }
                return new InstructionItem("+Error", "auto", "Unable to find operator for '" + oper + "'", loc);
            };
            andOrOperRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string oper = source.TryMatch(andOrToken);
                context.PassThrough();
                context.Store.Add(GetInstructionFromString(oper, source.From));
            };
            mulDivOperRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string oper = source.TryMatch(operatorToken);
                context.PassThrough();
                context.Store.Add(GetInstructionFromString(oper, source.From));
            };
            addSubOperRule.Build += mulDivOperRule.Build;
            compOperRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string oper = source.TryMatch(comparitorToken);
                context.PassThrough();
                context.Store.Add(GetInstructionFromString(oper, source.From));
            };

            paramRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                context.Store.Add(new InstructionItem("+Param", null, null, source.From));
                context.PassThrough();
            };
            callRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string name = source.Match(nameToken);
                int paramC = 0;
                for (int c = 0; c < context.Children.Count; ++c) {
                    var child = context.Children[c];
                    if (child.Command == "+Param") paramC++;
                    else context.Store.Add(child);
                }
                //context.PassThrough();
                context.Store.Add(new InstructionItem("Call" + paramC, "auto", name, source.From));
            };
            statementRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                /*for (int c = 0; c < context.Children.Count; ++c) {
                    Debug.Assert(context.Children[c].Value != null,
                        "All statements should be converted to instructions!");
                }*/
                context.PassThrough();
                //context.Store.Add(new InstructionItem("RemoveItemIfNotVoid", "void", null, source.From));
            };
            conditionRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                context.PassThrough();
                context.Store.Add(new InstructionItem("+CondEnd", source.From));
            };
            pDeclrRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string type = ParserUtil.GetCleanTypeName(source.Match(typeToken));
                string name = source.Match(nameToken);
                FInternal.Parameter param = new FInternal.Parameter();
                if (context.Children.Count > 0) {
                    Debug.Assert(context.Children.Count == 1 && context.Children[0].Command == "Constant",
                        "Parameter types must be constant");
                    param.Default = context.Children[0].Value;
                }
                param.Name = name;
                context.Store.Add(new InstructionItem("+Parameter", type, param, source.From));
                // Do nothing
            };
            fDeclrRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                string type = ParserUtil.GetCleanTypeName(source.Match(typeToken));
                string name = source.Match(nameToken);
                int id = (++uid);
                context.Store.Add(new InstructionItem("+Allocate", "fn", name, source.From));
                context.Store.Add(new InstructionItem("Reference", "fn*", name, source.From));

                var fn = new FInternal() { Name = name + id };
                List<FInternal.Parameter> parms = new List<FInternal.Parameter>();
                context.Store.Add(new InstructionItem("Function", "fn", fn, source.From));
                context.Store.Add(new InstructionItem("Marker", "void", name + id + "_start", source.From));
                for (int c = 0; c < context.Children.Count; ++c) {
                    var child = context.Children[c];
                    if (child.Command == "+Parameter") {
                        parms.Add((FInternal.Parameter)child.Value);
                    } else if (child.Command == "+Return") {
                        context.Store.Add(new InstructionItem("Constant", "int", 0, source.From));
                        context.Store.Add(new InstructionItem("Return", "fn", name + id, source.From));
                    } else context.Store.Add(child);
                }
                fn.Parameters = parms.ToArray();
                //context.PassThrough();
                context.Store.Add(new InstructionItem("Constant", "int", 0, source.From));
                context.Store.Add(new InstructionItem("Return", "fn", name + id, source.From));
                context.Store.Add(new InstructionItem("Marker", "void", name + id + "_end", source.From));

                context.Store.Add(new InstructionItem("Assign", "fn", "~" + name, source.From));
            };
            forConditionRule.Build += (level, source) => {
                level.Store.Add(new InstructionItem("+ContBeg", source.From));
                string comparison = source.TryMatch(comparitorToken);
                level.PassThrough();
                if (comparison != null) {
                    level.Store.Add(GetInstructionFromString(comparison, source.From));
                }
                level.Store.Add(new InstructionItem("+ContEnd", source.From));
            };
            forRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                int id = (++uid);
                int contBeg = context.FindIndexInChildrenOnce("+ContBeg");
                int contEnd = context.FindIndexInChildrenOnce("+ContEnd");
                int varRef = context.FindIndexInChildrenOnce("Reference", contBeg);
                Debug.Assert(varRef >= 0,
                    "A single assignment should exist before the condition");
                var varRefI = context.Children[varRef];
                string varName = (string)varRefI.Value;
                string varType = ParserUtil.StripStars(varRefI.Type);
                int arAloc = context.FindIndexInChildrenOnce("Allocate", varRef);
                if (arAloc >= 0) {
                    Debug.Assert(context.Children[arAloc].Value.Equals(varRefI.Value),
                        "Assumption that the allocation and reference will always be the same value");
                } else {
                    if (varType == "auto") varType = "int";
                    context.Store.Add(new InstructionItem("+AllocateIfRequired", varType, varName, source.From));
                }
                context.PassRange(0, contBeg);
                context.Store.Add(new InstructionItem("Marker", "void", "for" + id + "_beg", source.From));
                context.Store.Add(new InstructionItem("Variable", varType, varName, source.From));
                context.PassRange(contBeg + 1, contEnd);
                context.Store.Add(new InstructionItem("JumpIfFalse", "void", "for" + id + "_end", source.From));
                for (int c = contEnd + 1; c < context.Children.Count; ++c) {
                    var child = context.Children[c];
                    if (child.Command == "+Break") child = new InstructionItem("JumpTo", "void", "for" + id + "_end", child.Location);
                    else if (child.Command == "+Continue") child = new InstructionItem("JumpTo", "void", "for" + id + "_inc", child.Location);
                    context.Store.Add(child);
                }
                //context.PassRange(contEnd + 1, context.Children.Count);
                context.Store.Add(new InstructionItem("Marker", "void", "for" + id + "_inc", source.From));
                context.Store.Add(new InstructionItem("Reference", "int*", varRefI.Value, source.From));
                context.Store.Add(new InstructionItem("Variable", "int", varRefI.Value, source.From));
                context.Store.Add(new InstructionItem("Constant", "int", 1, source.From));
                context.Store.Add(new InstructionItem("Add", "int", "", source.From));
                context.Store.Add(new InstructionItem("Assign", "int", "~" + (string)varRefI.Value, source.From));
                context.Store.Add(new InstructionItem("Pop", "void", "", source.From));
                context.Store.Add(new InstructionItem("JumpTo", "void", "for" + id + "_beg", source.From));
                context.Store.Add(new InstructionItem("Marker", "void", "for" + id + "_end", source.From));
            };
            flowControlRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                Debug.Assert(context.Children.Count == 0,
                    "For control statements dont support child elements, yet.");
                context.PassThrough();
                string flow = source.Match(flowControlToken);
                switch (flow) {
                    case "break": context.Store.Add(new InstructionItem("+Break", source.From)); break;
                    case "continue": context.Store.Add(new InstructionItem("+Continue", source.From)); break;
                    case "return": context.Store.Add(new InstructionItem("+Return", source.From)); break;
                }
            };
            ifRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                int id = (++uid);
                int condEnd = context.FindIndexInChildrenOnce("+CondEnd");
                int elseStart = context.FindIndexInChildrenOnce("+Else");
                int elseEnd = context.FindIndexInChildrenOnce("+EndElse");
                context.PassRange(0, condEnd);
                context.Store.Add(new InstructionItem("JumpIfFalse", "void", "if" + id, source.From));
                if (elseStart >= 0) {
                    Debug.Assert(elseEnd >= 0,
                        "Else needs to have a start and end!");
                    context.PassRange(condEnd + 1, elseStart);
                    context.Store.Add(new InstructionItem("JumpTo", "void", "if_else" + id, source.From));
                    context.Store.Add(new InstructionItem("Marker", "void", "if" + id, source.From));
                    context.PassRange(elseStart + 1, elseEnd);
                    context.Store.Add(new InstructionItem("Marker", "void", "if_else" + id, source.From));
                } else {
                    context.PassRange(condEnd + 1, context.Children.Count);
                    context.Store.Add(new InstructionItem("Marker", "void", "if" + id, source.From));
                }
                Debug.Assert(condEnd >= 0,
                    "Unable to determine where condition ends!");
            };

            elseRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                int id = (++uid);
                context.Store.Add(new InstructionItem("+Else", "void", id, source.From));
                context.PassThrough();
                context.Store.Add(new InstructionItem("+EndElse", "void", id, source.From));
            };

            blockRule.Build += delegate(ParseContext.Level context, SourceIterator source) {
                var instructions = context.Children;
                var destination = context.Store;
                Dictionary<string, string> name2Type = new Dictionary<string, string>();

                destination.Add(new InstructionItem("BlockStart", "void", null, source.From));

                int destStart = destination.Count;

                for (int i = 0; i < instructions.Count; ++i) {
                    var instr = instructions[i];
                    if(instr.Command == "+AllocateIfRequired") {
                        Debug.Assert(instr.Type != "auto",
                            "Cant allocate a variable of unknown type!");
                        Debug.Assert(instr.Value is String,
                            "Variable names should be strings!");
                        string name = (String)instr.Value;
                        if (!name2Type.ContainsKey(name)) name2Type.Add(name, instr.Type);
                    } else if (instr.Command == "+Allocate") {
                        Debug.Assert(instr.Type != "auto",
                            "Cant allocate a variable of unknown type!");
                        Debug.Assert(instr.Value is String,
                            "Variable names should be strings!");
                        string name = (String)instr.Value;
                        if (!name2Type.ContainsKey(name)) name2Type.Add(name, instr.Type);
                    } else if (instr.Command == "Assign") {
                        string name = ((String)instr.Value).Substring(1);
                        if (name2Type.ContainsKey(name)) instr.Type = name2Type[name];
                        //else instr.Type = "var";
                        destination.Add(instr);
                    } else if (instr.Command == "Reference" || instr.Command == "Variable") {
                        Debug.Assert(instr.Value is String,
                            "Variable names should be strings!");
                        string name = (String)instr.Value;
                        string stars = (instr.Command == "Variable" ? "" : "*");
                        if (instr.Type == "auto" + stars && name2Type.ContainsKey(name))
                            instr.Type = name2Type[(String)instr.Value] + stars;
                        destination.Add(instr);
                    } else if (instr.Command.StartsWith("Call") && instr.Type == "auto") {
                        instr.Type = "var";
                        destination.Add(instr);
                    } else {
                        destination.Add(instr);
                    }
                }
                foreach (var n2t in name2Type) {
                    destination.Insert(destStart, new InstructionItem("Allocate", n2t.Value, n2t.Key, source.From));
                }

                destination.Add(new InstructionItem("BlockEnd", "void", null, source.From));

            };
        }

    }
}
