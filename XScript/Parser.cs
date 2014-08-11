using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using XScript.Instructions;
using System.Diagnostics;

// RMS: http://web.archive.org/web/20050516102619/http://www.geocities.com/hyenastudios/aomrmstutorial.htm
// Programs: http://aom.heavengames.com/cgi-bin/forums/display.cgi?action=ct&f=19,21161,,20
// XScript ref: http://aomcodereference.googlecode.com/svn/trunk/doc/aom/scripting/xs/rm/Area.html


namespace XScript {
    public class Parser {

        public class TokenMode {
            public string Name;
            public string[] Patterns;
            public string Pattern { set { Patterns = new[] { value }; } }
            public TokenMode[] Pathways;

            public TokenMode(string name) { Name = name; }
            public override string ToString() {
                string str = Name + ", ";
                for (int p = 0; p < Patterns.Length; ++p) {
                    if (p > 0) str += ", ";
                    str += Patterns[p];
                }
                return str;
            }
        }
        public struct Token {
            public string String;
            public int PointInString;
            public TokenMode Mode;

            public override string ToString() {
                return Mode.Name + ",  " + String;
            }
        }

        static TokenMode commentToken = new TokenMode("comment") { Pattern = "^((//).*(\n)|(/\\*).*(\\*/))" };
        static TokenMode flowControlToken = new TokenMode("flow") { Pattern = "^(if|do|while|else)" };
        static TokenMode blockStartToken = new TokenMode("{") { Pattern = "^[{]" };
        static TokenMode blockEndToken = new TokenMode("}") { Pattern = "^[}]" };
        static TokenMode parmStartToken = new TokenMode("(") { Pattern = "^[(]" };
        static TokenMode parmEndToken = new TokenMode(")") { Pattern = "^[)]" };
        static TokenMode typeToken = new TokenMode("type") { Pattern = "^(int|float|double|void|class)" };
        static TokenMode nameToken = new TokenMode("name") { Pattern = "^[a-zA-Z_][a-zA-Z0-9_]*" };
        static TokenMode constantToken = new TokenMode("const") { Patterns = new[] { "^[0-9]+(\\.[0-9]*)?", "^[\"].*[\"]", "^['][\\\\]?.[']" } };
        //static TokenMode stringToken = new TokenMode() { Pattern = "^[\"].*[\"]" };
        //static TokenMode characterToken = new TokenMode() { Pattern = "^['][\\\\]?.[']" };
        static TokenMode commaToken = new TokenMode(",") { Pattern = "^[,]" };
        static TokenMode endStatementToken = new TokenMode(";") { Pattern = "^[;]" };
        static TokenMode operatorToken = new TokenMode("oper") { Pattern = "^([<>=][=]?|[*+\\-/^=])" };
        static TokenMode postUnaryToken = new TokenMode("punary") { Pattern = "^[+-]{1,2}" };
        static TokenMode unaryToken = new TokenMode("unary") { Pattern = "^[+-]{1,2}" };
        static TokenMode[] universalPathways = new[] { commentToken };

        static Parser() {
#if NOTHING
            var allPathways = new[] { commentToken, flowControlToken, blockStartToken, blockEndToken, constantToken, nameToken, commaToken, endStatementToken, operatorToken };
            commentToken.Pathways = allPathways;// new[] { numberToken, variableToken, stringToken, blockStartToken };
            blockStartToken.Pathways = allPathways;// new [] { comment, /*functionToken, */numberToken, variableToken, stringToken, blockStartToken };
            blockEndToken.Pathways = allPathways;// new[] { comment, /*functionToken, */numberToken, variableToken, stringToken, operatorToken, blockEndToken, blockStartToken };
            constantToken.Pathways = allPathways;// new[] { comment, /*functionToken, */variableToken, stringToken, operatorToken, blockStartToken, blockEndToken };
            operatorToken.Pathways = allPathways;// new[] { comment, /*functionToken, */numberToken, variableToken, stringToken, blockStartToken, blockEndToken };
            nameToken.Pathways = allPathways;// new[] { comment, /*functionToken, */numberToken, variableToken, stringToken, operatorToken, blockStartToken, blockEndToken };
            //stringToken.Pathways = allPathways;// new[] { comment, /*functionToken, */numberToken, variableToken, stringToken, operatorToken, blockStartToken, blockEndToken };
            //functionToken.Pathways = new [] { comment, blockStartToken, variableToken, numberToken };
            commaToken.Pathways = allPathways;
            endStatementToken.Pathways = allPathways;
            //characterToken.Pathways = allPathways;
            flowControlToken.Pathways = allPathways;
#endif
            var blockStartPathways = new[] { flowControlToken, blockStartToken, blockEndToken, typeToken, nameToken, constantToken, endStatementToken, unaryToken };
            commentToken.Pathways = null;
            flowControlToken.Pathways = new[] { blockStartToken, parmStartToken, typeToken, nameToken };
            blockStartToken.Pathways = blockStartPathways;
            blockEndToken.Pathways = blockStartPathways;
            parmStartToken.Pathways = new[] { unaryToken, typeToken, nameToken, constantToken, parmEndToken };
            parmEndToken.Pathways = new[] { parmStartToken, parmEndToken, operatorToken, commaToken, endStatementToken, blockStartToken, typeToken, nameToken };
            typeToken.Pathways = new[] { nameToken, parmEndToken };
            nameToken.Pathways = new[] { parmStartToken, parmEndToken, postUnaryToken, operatorToken, commaToken, endStatementToken };
            constantToken.Pathways = new[] {             parmEndToken, operatorToken, commaToken, endStatementToken };
            commaToken.Pathways = new[] { unaryToken, nameToken, constantToken };
            endStatementToken.Pathways = new[] { flowControlToken, unaryToken, typeToken, nameToken, blockStartToken, parmStartToken, blockEndToken, parmEndToken, operatorToken };
            operatorToken.Pathways = new[] { constantToken, nameToken, parmStartToken };
            unaryToken.Pathways = new[] { parmStartToken, nameToken, constantToken };
            postUnaryToken.Pathways = new[] {            parmEndToken, operatorToken, commaToken, endStatementToken };

            InitConditions();
        }

        public Token[] Tokenize(string str) {
            bool valid;
            return Tokenize(str, out valid);
        }
        public Token[] Tokenize(string str, out bool isValid) {
            List<Token> tokens = new List<Token>();
            int c = 0;
            TokenMode currentToken = blockStartToken;
            var pathways = currentToken.Pathways;
            while (true) {
                while (c < str.Length && char.IsWhiteSpace(str[c])) ++c;
                if (c >= str.Length) break;
                String rest = str.Substring(c, str.Length - c);
                int origC = c;
                foreach (var pathway in universalPathways.Concat(pathways)) {
                    foreach (var pattern in pathway.Patterns) {
                        var match = Regex.Match(rest, pattern);
                        if (match.Success && match.Value.Length > 0) {
                            tokens.Add(new Token() { PointInString = c, String = match.Value, Mode = pathway });
                            c += match.Value.Length;
                            currentToken = pathway;
                            if (currentToken.Pathways != null) pathways = currentToken.Pathways;
                            //Console.WriteLine(currentToken.Name);
                            goto done;
                        }
                    }
                }
            done:
                if (origC == c) {
                    int end = Math.Min(str.Length, str.IndexOf('\r', c));
                    int cnt = Math.Min(end - c, 40);
                    Console.WriteLine("Unable to tokenize at " + c + ":\r\n" +
                        "  " + str.Substring(c, cnt) + "\r\n" +
                        "from <" + currentToken.Name + "> pattern " + currentToken);
                    break;
                }
            }
            isValid = (c == str.Length);
            return tokens.ToArray();
        }

        private static int GetTokenPrecedence(ref Token token) {
            if (token.Mode == operatorToken) {
                switch (token.String) {
                    case "+": return 10;
                    case "-": return 10;
                    case "*": return 8;
                    case "/": return 8;
                }
            }
            return 0;
        }

        public struct TokenArray {
            public Token[] Tokens;
            public int Start;
            public int End;
            public TokenArray(Token[] arr) : this(arr, 0, arr.Length) { }
            public TokenArray(Token[] arr, int start, int end) { Tokens = arr; Start = start; End = end; }
        }
        public struct TokenIterator {
            public TokenArray Tokens;
            public int Iterator;
            public int NextIterator {
                get {
                    int iter = Iterator + 1;
                    while (iter < Tokens.Tokens.Length && Tokens.Tokens[iter].Mode == commentToken) ++iter;
                    return iter;
                }
            }
            public TokenIterator(TokenArray arr) : this(arr, arr.Start) { }
            public TokenIterator(TokenArray arr, int iter) { Tokens = arr; Iterator = iter - 1; }
            public Token Current { get { return Tokens.Tokens[Iterator]; } }
            public Token GetNext() { Iterator = NextIterator; return Tokens.Tokens[Iterator]; }
            public bool HasNext { get { return NextIterator < Tokens.End; } }
        }

        public static TokenMode GetBlockEndMode(TokenMode blockStart) {
            if (blockStart == parmStartToken) return parmEndToken;
            if (blockStart == blockStartToken) return blockEndToken;
            return null;
        }

        private static int FindBlockLength(TokenArray tokens) {
            var iterator = new TokenIterator(tokens);
            List<TokenMode> blocks = new List<TokenMode>();
            {
                var token = iterator.GetNext();
                var end = GetBlockEndMode(token.Mode);
                Debug.Assert(end != null,
                    "Block must begin with a block start");
                blocks.Add(end);
            }
            while (iterator.HasNext && blocks.Count > 0) {
                var token = iterator.GetNext();
                if (token.Mode == blocks[blocks.Count - 1]) blocks.RemoveAt(blocks.Count - 1);
                else {
                    var end = GetBlockEndMode(token.Mode);
                    if (end != null) blocks.Add(end);
                }
            }
            return iterator.Iterator - tokens.Start + 1;        // +1 to include the last bracket
        }

        private static List<string> ParseParameters(Token[] tokens, int start, ref int end) {
            for (int t = start; t < end; ++t) {
                var token = tokens[t];
            }
            return null;
        }

        public struct Cardinality {
            public short From, To;
            public Cardinality(int from, int to) { From = (short)from; To = (short)to; }
            public static implicit operator Cardinality(int max) { return new Cardinality() { From = 1, To = (short)max }; }
        }
        public class GrammarElement {
            public static implicit operator GrammarElement(TokenMode mode) { return new GrammarElementT(mode); }
            public static implicit operator GrammarElement(GrammarRule condition) { return new GrammarElementC(condition); }
        }
        public class GrammarElementT : GrammarElement {
            public TokenMode Mode;
            public Func<Token, bool> ValidityCheck;
            public Action<ParseContext, TokenIterator> OnToken;
            public GrammarElementT(TokenMode mode) { Mode = mode; }
            public GrammarElementT(TokenMode mode, Func<Token, bool> validCheck) { Mode = mode; ValidityCheck = validCheck; }
        }
        public class GrammarElementC : GrammarElement {
            public GrammarRule Condition;
            public GrammarElementC(GrammarRule condition) { Condition = condition; }
        }
        public class GrammarElementO : GrammarElement {
            public GrammarRule[] Conditions;
            public GrammarElementO(params GrammarRule[] conditions) { Conditions = conditions; }
        }
        public class GrammarRule {
            public string Name;
            public Cardinality Cardinality;
            public GrammarElement[] Grammar;
            public GrammarRule(string name) { Name = name; Cardinality = 1; }
            public GrammarRule(string name, GrammarElement element) { Name = name; Grammar = new [] { element }; Cardinality = 1; }
            public GrammarRule(string name, params GrammarElement[] elements) { Name = name; Grammar = elements; Cardinality = 1; }

            public static implicit operator GrammarRule(GrammarElementT el) { return new GrammarRule("", el); }
            public static implicit operator GrammarRule(TokenMode mode) { return new GrammarRule("", new GrammarElementT(mode)); }

            public Action<ParseContext.Level, TokenIterator> Build;
        }

        static GrammarRule emptyRule = new GrammarRule("Empty", new GrammarElement[] { });
        static GrammarRule semicolonRule = new GrammarRule("Semicolon", endStatementToken);
        static GrammarRule variableRule = new GrammarRule("Variable", nameToken);
        static GrammarRule constantRule = new GrammarRule("Constant", constantToken);
        static GrammarRule ifRule = new GrammarRule("If");
        static GrammarRule rValueRule = new GrammarRule("RValue");
        static GrammarRule statementRule = new GrammarRule("Statement");
        static GrammarRule assignmentRule = new GrammarRule("Assignment");
        static GrammarRule vAssignmentRule = new GrammarRule("VAssignment");
        static GrammarRule callRule = new GrammarRule("Call");
        static GrammarRule termRule = new GrammarRule("Term");
        static GrammarRule equationRule = new GrammarRule("Equation");
        static GrammarRule parmsRule = new GrammarRule("Parameters");
        static GrammarRule vDeclrRule = new GrammarRule("VDeclr") { Cardinality = 1000 };
        static GrammarRule vDeclrArrRule = new GrammarRule("VDeclrArr");
        static GrammarRule fDeclrRule = new GrammarRule("FDeclr");
        static GrammarRule declrRule = new GrammarRule("Declr");
        static GrammarRule blockRule = new GrammarRule("Block") { Cardinality = 1000 };

        static void InitConditions() {
            ifRule.Grammar = new GrammarElement[] {
                new GrammarElementT(flowControlToken, t => t.String == "if"),
                parmStartToken, equationRule, parmEndToken,
                blockStartToken, blockRule, blockEndToken
            };
            rValueRule.Grammar = new GrammarElement[] { new GrammarElementO( callRule, equationRule ) };
            assignmentRule.Grammar = new GrammarElement[] { operatorToken, rValueRule };
            vAssignmentRule.Grammar = new GrammarElement[] { variableRule, assignmentRule};
            callRule.Grammar = new GrammarElement[] { nameToken, parmStartToken, parmsRule, parmEndToken };
            termRule.Grammar = new GrammarElement[] { new GrammarElementO(
                variableRule,
                constantRule,
                new GrammarRule("inner", new GrammarElement[] { parmStartToken, equationRule, parmEndToken })
            )};
            equationRule.Grammar = new GrammarElement[] {
                new GrammarElementO(
                    new GrammarRule[] {
                        new GrammarRule("add", termRule, new GrammarElementT(operatorToken, t => t.String == "+"), termRule) {
                            Build = (c, t) => {
                                c.Store.Add(new IAddition() { Values = c.ChildrenAsInstructions });
                            }
                        },
                        new GrammarRule("sub", termRule, new GrammarElementT(operatorToken, t => t.String == "-"), termRule) {
                            Build = (c, t) => {
                                c.Store.Add(new ISubtraction() { Values = c.ChildrenAsInstructions });
                            }
                        },
                        new GrammarRule("mul", termRule, new GrammarElementT(operatorToken, t => t.String == "*"), termRule) {
                            Build = (c, t) => {
                                c.Store.Add(new IMultiplication() { Values = c.ChildrenAsInstructions });
                            }
                        },
                        new GrammarRule("div", termRule, new GrammarElementT(operatorToken, t => t.String == "/"), termRule) {
                            Build = (c, t) => {
                                c.Store.Add(new IDivision() { Values = c.ChildrenAsInstructions });
                            }
                        },
                        new GrammarRule("==", termRule, new GrammarElementT(operatorToken, t => t.String == "=="), termRule) {
                            Build = (c, t) => {
                                c.Store.Add(new IEquals() { Values = c.ChildrenAsInstructions });
                            }
                        },
                        new GrammarRule("!=", termRule, new GrammarElementT(operatorToken, t => t.String == "!="), termRule) {
                            Build = (c, t) => {
                                c.Store.Add(new INotEquals() { Values = c.ChildrenAsInstructions });
                            }
                        },
                        termRule
                    }
                )
            };
            parmsRule.Grammar = new GrammarElement[] {
                new GrammarElementO( rValueRule, emptyRule ),
                new GrammarElementO(
                    new GrammarRule(",Parameters", new GrammarElement[] {
                        commaToken,
                        rValueRule
                    }) { Cardinality = 1000 },
                    emptyRule
                ),
            };
            vDeclrRule.Grammar = new GrammarElement[] { typeToken, nameToken, assignmentRule };
            vDeclrArrRule.Grammar = new GrammarElement[] { vDeclrRule };
            var fnParamDeclr = new GrammarRule("FN Params", new GrammarElement[] {
                new GrammarElementO(
                    new GrammarElementT(typeToken, t => t.String == "void"),  // void
                    vDeclrArrRule
                )
            });
            fDeclrRule.Grammar = new GrammarElement[] {
                typeToken, nameToken, parmStartToken, fnParamDeclr, parmEndToken, blockStartToken, blockRule, blockEndToken
            };
            declrRule.Grammar = new GrammarElement[] { new GrammarElementO( vDeclrRule, fDeclrRule ) };
            statementRule.Grammar = new GrammarElement[] {
                new GrammarElementO(
                    new GrammarRule("Fn;", new GrammarElement[] { fDeclrRule }),
                    new GrammarRule("Var;", new GrammarElement[] { vDeclrArrRule, semicolonRule }),
                    new GrammarRule("Assignment;", new GrammarElement[] { vAssignmentRule, semicolonRule }),
                    new GrammarRule("Call;", new GrammarElement[] { callRule, semicolonRule }),
                    new GrammarRule("If;", new GrammarElement[] { ifRule })
                )
            };
            blockRule.Grammar = new GrammarElement[] { statementRule };

            // Setup AST building
            parmsRule.Build = delegate(ParseContext.Level context, TokenIterator iter) {
                context.PassThrough();
            };
            blockRule.Build = delegate(ParseContext.Level context, TokenIterator iter) {
                List<string> variables = new List<string>();
                List<Instruction> instructions = new List<Instruction>();
                for (int c = 0; c < context.Children.Count; ++c) {
                    var child = context.Children[c];
                    if (child is IVDeclare) {
                        variables.Add((child as IVDeclare).Name);
                    } else if (child is Instruction) {
                        instructions.Add(child as Instruction);
                    } else {
                        throw new Exception("Unknown element");
                    }
                }
                context.Store.Add(new IScope() { Values = variables.ToArray(), Instructions = instructions.ToArray() });
                //context.PassThrough();
            };
            callRule.Build = delegate(ParseContext.Level context, TokenIterator iter) {
                context.Store.Add(new ICall() {
                    Arguments = context.ChildrenAsInstructions,
                    FunctionName = iter.GetNext().String,
                });
            };
            variableRule.Build = delegate(ParseContext.Level context, TokenIterator iter) {
                context.Store.Add(new IVariable() { Name = iter.GetNext().String });
            };
            constantRule.Build = delegate(ParseContext.Level context, TokenIterator iter) {
                string value = iter.GetNext().String;
                if (value[0] == '"') context.Store.Add(new VString(UnescapeString(value)));
                else {
                    double res;
                    if (double.TryParse(value, out res)) context.Store.Add(new VNumber(res));
                }
            };
            vAssignmentRule.Build = delegate(ParseContext.Level context, TokenIterator iter) {
                Debug.Assert(context.Children.Count == 2 && context.Children[0] is IVariable,
                    "Assignment operator must have 2 children, and the lvalue must be a variable");
                var value = context.Children[1];
                context.Store.Add(new IAssignment() {
                    Name = (context.Children[0] as IVariable).Name,
                    Value = (value is Instruction ? value as Instruction : Instruction.FromValue(value as Value))
                });
            };
            vDeclrRule.Build = delegate(ParseContext.Level context, TokenIterator iter) {
                string type = iter.GetNext().String;
                string name = iter.GetNext().String;
                context.Store.Add(new IVDeclare() { Type = type, Name = name });
                Debug.Assert(context.Children.Count == 1,
                    "Var declr can only have 1 assignment");
                var val = context.Children[0];
                context.Store.Add(new IAssignment() {
                    Name = name,
                    Value = (val is Instruction ?
                        val as Instruction :
                        new IConstant() { Value = val as Value }
                    )
                });
            };
            fDeclrRule.Build = delegate(ParseContext.Level context, TokenIterator iter) {
                string type = iter.GetNext().String;
                string name = iter.GetNext().String;
                context.Store.Add(new IVDeclare() { Type = type, Name = name });
                Debug.Assert(context.Children.Count == 1 && context.Children[0] is IScope,
                    "A function must contain a scope");
                context.Store.Add(new IAssignment() {
                    Name = name,
                    Value = new IConstant() {
                        Value = new VFunction(new String[] {}, context.Children[0] as IScope)
                    }
                });
                //context.Store.Add(new VFunction(
            };
        }

        public static string UnescapeString(string str) {
            Debug.Assert(str[0] == '"' && str[str.Length - 1] == '"',
                "String must begin and end in quotes");
            string res = "";
            for (int s = 1; s < str.Length - 1; ++s) {
                char c = str[s];
                if (c == '\\') res += str[++s];
                else {
                    Debug.Assert(c != '"', "Strings cannot have quotes without escapes");
                    res += c;
                }
            }
            return res;
        }

        public class ParseContext {
            public struct Level : IDisposable {
                public ParseContext Context;
                public int Index;
                public Level(ParseContext context, int level) {
                    Context = context;
                    Index = level;
                }
                public void Clear() {
                    Store.Clear();
                }
                public void ClearChildren() {
                    Children.Clear();
                }
                public List<Object> Store { get { return Context.GetStoreAt(Index); } }
                public List<Object> Children { get { return Context.GetStoreAt(Index + 1); } }

                public void PassThrough() {
                    Store.AddRange(Children);
                }

                public void Dispose() { }

                public Instruction[] ChildrenAsInstructions {
                    get {
                        return Children.Select(c => {
                            if(c is Value) return Instruction.FromValue(c as Value); else return c as Instruction;
                        }).ToArray();
                    }
                }
            }
            public List<List<Object>> Objects = new List<List<object>>();

            public List<Object> GetStoreAt(int level) {
                while (Objects.Count <= level) Objects.Add(new List<object>());
                return Objects[level];
            }
        }

        static int farthestIterator = 0;
        public static Action<ParseContext> Parse(ref TokenIterator iterator, GrammarRule condition, int level) {
            int c;
            TokenIterator startIter = iterator;
            string tabs = "";
            for (int l = 0; l < level; ++l) tabs += " ";
            List<Action<ParseContext>> children = new List<Action<ParseContext>>();
            for (c = 0; c < condition.Cardinality.To; ++c) {
                TokenIterator titerator = iterator;
                for (int g = 0; g < condition.Grammar.Length; ++g) {
                    var grammar = condition.Grammar[g];
                    if (grammar is GrammarElementT) {
                        var el = (grammar as GrammarElementT);
                        Token token;
                        do {
                            if (!titerator.HasNext) goto fail;
                            token = titerator.GetNext();
                        } while (token.Mode == commentToken);
                        if (token.Mode != el.Mode || (el.ValidityCheck != null && !el.ValidityCheck(token))) goto fail;
                    } else if (grammar is GrammarElementC) {
                        var success = Parse(ref titerator, (grammar as GrammarElementC).Condition, level + 1);
                        if (success == null) goto fail;
                        children.Add(success);
                    } else if (grammar is GrammarElementO) {
                        var options = (grammar as GrammarElementO).Conditions;
                        int goodOpt = -1;
                        for (int o = 0; o < options.Length; ++o) {
                            TokenIterator subIter = titerator;
                            var success = Parse(ref subIter, options[o], level + 1);
                            if (success != null) {
                                titerator = subIter;
                                children.Add(success);
                                goodOpt = o;
                                break;
                            }
                        }
                        if (goodOpt == -1) goto fail;
                    }
                    if (iterator.Iterator > farthestIterator) farthestIterator = iterator.Iterator;
                }
                iterator = titerator;
                Console.WriteLine(tabs + condition.Name);
            }
        fail:
            if (c >= condition.Cardinality.From) return delegate (ParseContext context) {
                using (var contextLevel = new ParseContext.Level(context, level)) {
                    contextLevel.ClearChildren();
                    for (int cn = 0; cn < children.Count; ++cn) children[cn](context);
                    if (condition.Build != null) condition.Build(contextLevel, startIter);
                    else contextLevel.PassThrough();
                }
            };
            return null;
        }

        public static Instruction Parse(Token[] tokens) {
            return Parse(new TokenArray(tokens));
        }
        public static Instruction Parse(TokenArray tokens) {
            var iterator = new TokenIterator(tokens);
            /* VDeclr: type, named, (equals, Statement){0..1}, semicolon
             * Declr: [FDeclr, VDeclr]
             * PDeclr: (VarDeclr, comma){1..n}
             * Params: (Statement, comma){1..n}
             * Block: [Function, Declr, Call]{1..n}
             * Statement: [Call, Equation], semicolon
             * Call: named, paren, Params, paren
             * FDeclr: type, named, paren, PDeclr, paren, brace, Block, brace
             */
            var iter = Parse(ref iterator, blockRule, 0);
            if (iter == null) {
                iterator.Iterator = farthestIterator;
                string str = "";
                for (int t = 0; t < 10 && iterator.HasNext; ++t)
                    str += (t > 0 ? " " : "") + iterator.GetNext().String;
                Debug.WriteLine("Unable to complete parse, error at " + str);
                return null;
            }
            var context = new ParseContext();
            iter(context);
            var store = context.GetStoreAt(0);
            for (int o = 0; o < store.Count; ++o) {
                Console.WriteLine(store[o]);
            }
            return store[0] as Instruction;

            while (iterator.HasNext) {
                var token = iterator.GetNext();
                if (token.Mode == commentToken) {
                    continue;
                } else if (token.Mode == typeToken) {   // Variable / function declaration
                    string type = token.String;
                    token = iterator.GetNext();
                    Debug.Assert(token.Mode == nameToken,
                        "A name must come after the type in a variable declaration, " + type);
                    string name = token.String;
                    Debug.Assert(iterator.HasNext,
                        "Missing semicolon, assignment operator or open parenthesis after type declaration, " + type + " " + name);
                    token = iterator.GetNext();
                    if (token.Mode == endStatementToken) {
                        Console.WriteLine("Variable found, " + type + " " + name);
                    } else if (token.Mode == operatorToken) {
                        Debug.Assert(token.String == "=",
                            "A type declaration can only have an assignment operator, " + type + " " + name);
                        string equalTo = "";
                        token = iterator.GetNext();
                        int equalI = iterator.Iterator;
                        while (token.Mode != endStatementToken) { equalTo += token.String; token = iterator.GetNext(); }
                        Console.WriteLine("Variable found, " + type + " " + name + " = " + equalTo);
                        Parse(new TokenArray(tokens.Tokens, equalI, iterator.Iterator));
                    } else if (token.Mode == parmStartToken) {
                        int len = FindBlockLength(new TokenArray(tokens.Tokens, iterator.Iterator, tokens.End));
                        iterator.Iterator += len - 1;
                        token = iterator.GetNext();
                        if (token.Mode == blockStartToken) {
                            int bodyLen = FindBlockLength(new TokenArray(tokens.Tokens, iterator.Iterator, tokens.End));
                            int bodyStart = iterator.Iterator + 1;
                            iterator.Iterator += bodyLen - 1;
                            Console.WriteLine("Function found, " + type + " " + name + "(" + len + ") " + bodyLen);
                            Parse(new TokenArray(tokens.Tokens, bodyStart, iterator.Iterator)); 
                        } else if(token.Mode == endStatementToken) {
                            Console.WriteLine("Function call, " + type + " " + name + "(" + len + ") ");
                        }
                    } else Debug.Assert(false, "Unknown or incomplete variable declaration, " + type + " " + name);
                } else if(token.Mode == nameToken) {
                } else if (token.Mode == flowControlToken) {
                    switch (token.String) {
                        case "if": {
                            //Console.WriteLine("If");
                        } break;
                    }
                }
            }
            return null;
        }

    }
}
