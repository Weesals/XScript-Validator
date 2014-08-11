using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XScript.Parser2 {
    public struct SourceLocation {
        public int CharacterId;
        public SourceLocation(int loc) {
            CharacterId = loc;
        }
        public int GetLineNumber(string source) {
            return source.Substring(0, CharacterId).Count(c => c == '\n') + 1;
        }

        public static implicit operator SourceLocation(int loc) {
            return new SourceLocation(loc);
        }
    }
    public class InstructionItem {
        public string Command;
        public string Type;
        public object Value;
        public SourceLocation Location;
        public InstructionItem(string command, SourceLocation loc) : this(command, null, null, loc) { }
        public InstructionItem(string command, string type, object value, SourceLocation loc) {
            Command = command; Type = type; Value = value;
            Location = loc;
        }

        public override string ToString() { return Type + " " + Command + " " + Value; }
    }

    public class ParseContext {
        public struct Level : IDisposable {
            public ParseContext Context;
            public int Index;
            private int oldStoreStart, oldChildStart;
            public Level(ParseContext context, int level) {
                Context = context;
                Index = level;
                oldStoreStart = Context.GetStoreAt(Index).Count;
                oldChildStart = Context.GetStoreAt(Index + 1).Count;
            }
            public void Clear() {
                Store.Clear();
            }
            public void ClearChildren() {
                Children.Clear();
            }
            public List<InstructionItem> Store { get { return Context.GetStoreAt(Index); } }
            public List<InstructionItem> Children { get { return Context.GetStoreAt(Index + 1); } }

            public int FindIndexInChildrenOnce(string command) {
                return FindIndexInChildrenOnce(command, Children.Count);
            }
            public int FindIndexInChildrenOnce(string command, int limit) {
                var children = Children;
                int ind = -1;
                for (int c = 0; c < limit; ++c) {
                    if (children[c].Command == command) {
                        Debug.Assert(ind < 0, "Item exists twice!");
                        ind = c;
                    }
                }
                return ind;
            }

            public void RemoveChanges() {
                if (oldStoreStart < Store.Count)
                    Store.RemoveRange(oldStoreStart, Store.Count - oldStoreStart);
                if (oldChildStart < Children.Count)
                    Children.RemoveRange(oldChildStart, Children.Count - oldChildStart);
            }
            public void PassThrough() {
                Store.AddRange(Children);
            }

            public void Dispose() { }

            public void PassRange(int from, int to) {
                Debug.Assert(from >= 0 && to >= 0,
                    "From and To need to be valid indices");
                for (int c = from; c < to; ++c) {
                    Store.Add(Children[c]);
                }
            }
        }

        public int MaxParseChar = 0;
        public Dictionary<GrammarRule, TokenType[]> ExpectedTokens = new Dictionary<GrammarRule, TokenType[]>();
        public bool BreakOnError = false;
        public GrammarRule ParsingAs { get { return rules.LastOrDefault(r => r.Name != "Implicit"); } }

        List<GrammarRule> rules = new List<GrammarRule>();

        public List<List<InstructionItem>> Objects = new List<List<InstructionItem>>();

        public List<InstructionItem> GetStoreAt(int level) {
            while (Objects.Count <= level) Objects.Add(new List<InstructionItem>());
            return Objects[level];
        }

        public Level GetCurrentContext() {
            return new Level(this, rules.Count);
        }

        public void MarkError(int charI, TokenType token) {
            if (charI < MaxParseChar) return;
            if (charI > MaxParseChar) {
                ExpectedTokens.Clear();
                MaxParseChar = charI;
            }
            TokenType[] expectedTokens = null;
            if (ExpectedTokens.ContainsKey(ParsingAs)) expectedTokens = ExpectedTokens[ParsingAs];
            if (expectedTokens != null) expectedTokens = expectedTokens.Concat(new[] { token }).ToArray();
            else expectedTokens = new[] { token };
            if (!ExpectedTokens.ContainsKey(ParsingAs)) ExpectedTokens.Add(ParsingAs, expectedTokens);
            else ExpectedTokens[ParsingAs] = expectedTokens;

            if (BreakOnError) {
                Debug.Assert(false,
                    "Script compile error!");
            }
        }

        public void PushRule(GrammarRule rule) {
            rules.Add(rule);
        }
        public void PopRule(GrammarRule rule) {
            Debug.Assert(rules.Last() == rule,
                "Rule is not at the end!");
            rules.RemoveAt(rules.Count - 1);
        }
        public override string ToString() {
            return rules.Select(r => r.Name).Aggregate((s1, s2) => s1 + ", " + s2);
        }
    }

    public class TokenType {
        public string Name;
        public Func<string, int, int> Match;
        public TokenType(string name) { Name = name; Match = null; }
        public TokenType(string name, Func<string, int, int> match) { Name = name; Match = match; }
        public TokenType(string name, string pattern) {
            Name = name;
            var regex = new Regex(pattern);
            Match = (str, b) => MatchOrDefault(regex, str, b);
        }

        public static int MatchOrDefault(Regex regex, string str, int b) {
            var match = regex.Match(str, b, str.Length - b);
            if (match.Success) return match.Length;
            return -1;
        }

        public static GrammarElement operator &(TokenType t1, GrammarElement el) {
            return ((GrammarElement)t1) & el;
        }
        public static GrammarElement operator |(TokenType t1, GrammarElement el) {
            return ((GrammarElement)t1) | el;
        }
        public override string ToString() { return Name; }
    }

    public struct Cardinality {
        public short From, To;
        public Cardinality(int from, int to) { From = (short)from; To = (short)to; }
        public static implicit operator Cardinality(int max) { return new Cardinality() { From = 1, To = (short)max }; }
    }
    public class GrammarElement {
        public static implicit operator GrammarElement(TokenType mode) { return new GrammarElementT(mode); }
        public static implicit operator GrammarElement(GrammarRule condition) { return new GrammarElementR(condition); }

        public static GrammarElement operator &(GrammarElement el, GrammarElement el2) {
            GrammarElementA res = el as GrammarElementA;
            if (res is GrammarElementA) {
                res.Elements = res.Elements.Concat(new[] { el2 }).ToArray();
                return res;
            } else {
                return new GrammarElementA(el, el2);
            }
        }
        public static GrammarElement operator |(GrammarElement el, GrammarElement el2) {
            GrammarElementO res = el as GrammarElementO;
            if (res is GrammarElementO) {
                res.Elements = res.Elements.Concat(new[] { el2 }).ToArray();
                return res;
            } else {
                return new GrammarElementO(el, el2);
            }
        }
        public GrammarRule this[int from, int to] {
            get { return new GrammarRule(this)[from, to]; }
        }
        public static implicit operator GrammarRule(GrammarElement el) {
            return new GrammarRule(el);
        }
    }
    // Token
    public class GrammarElementT : GrammarElement {
        public TokenType TokenType;
        public GrammarElementT(TokenType token) { TokenType = token; }
        public override string ToString() { return TokenType.ToString(); }
    }
    // Nested rule
    public class GrammarElementR : GrammarElement {
        public GrammarRule Rule;
        public GrammarElementR(GrammarRule rule) { Rule = rule; }
        public override string ToString() { return Rule.ToString(); }
    }
    // Always (and)
    public class GrammarElementA : GrammarElement {
        public GrammarElement[] Elements;
        public GrammarElementA(params GrammarElement[] elements) { Elements = elements; }
        public override string ToString() { return Elements.Select(e => e.ToString()).Aggregate((s1, s2) => "(" + s1 + "&" + s2 + ")"); }
    }
    // Optional (or)
    public class GrammarElementO : GrammarElement {
        public GrammarElement[] Elements;
        public GrammarElementO(params GrammarElement[] elements) { Elements = elements; }
        public override string ToString() { return Elements.Select(e => e.ToString()).Aggregate((s1, s2) => "(" + s1 + "|" + s2 + ")"); }
    }

    public class GrammarRule {
        public string Name;
        public Cardinality Cardinality;
        public GrammarElement Element;
        public GrammarRule(string name) { Name = name; Cardinality = 1; }
        public GrammarRule(GrammarElement element) { Name = "Implicit"; Element = element; Cardinality = 1; }
        public GrammarRule(string name, GrammarElement element) { Name = name; Element = element; Cardinality = 1; }

        public void SetElements(GrammarElement element) {
            Element = element;
        }
        public GrammarRule this[int from] {
            get { Cardinality = from; return this; }
        }
        public GrammarRule this[int from, int to] {
            get { Cardinality = new Cardinality(from, to); return this; }
        }

        public static GrammarElement operator &(GrammarRule r1, GrammarElement el) {
            return ((GrammarElement)r1) & el;
        }
        public static GrammarElement operator |(GrammarRule r1, GrammarElement el) {
            return ((GrammarElement)r1) | el;
        }
        public override string ToString() { return Name; }

        public Action<ParseContext.Level /*context*/, SourceIterator /*source*/> Build;
    }

    public class SourceIterator {
        private string source;
        public int From;
        public int Iterator;
        public int To;
        public string SourceCode { get { return source.Substring(From, To - From); } }
        public SourceIterator(string src, int from, int to) { source = src; Iterator = From = from; To = to; }
        public string TryMatch(TokenType token) {
            ParserUtil.IteratePastWhitespaceAndComments(source, ref Iterator);
            int len = token.Match(source, Iterator);
            if (len < 0) return null;
            Iterator += len;
            return source.Substring(Iterator - len, len);
        }
        public string Match(TokenType token) {
            var res = TryMatch(token);
            Debug.Assert(res != null,
                "Token didnt match!");
            return res;
        }
    }


    public static class ParserUtil {
        public static void IteratePastWhitespaceAndComments(string source, ref int iter) {
            while (true) {
                int start = iter;
                // Skip whitespace
                while (iter < source.Length && char.IsWhiteSpace(source[iter])) iter++;
                // Skip comments
                if (iter + 2 <= source.Length && source[iter] == '/') {
                    if (source[iter + 1] == '/') {
                        while (iter < source.Length && source[iter] != '\n') iter++;
                    } else if (source[iter + 1] == '*') {
                        while (iter < source.Length) {
                            if (iter + 2 > source.Length ||
                                (source[iter] == '*' && source[iter + 1] == '/')) { iter += 2; break; }
                            iter++;
                        }
                    }
                }
                if (start == iter) break;
            }
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

        public static int CountStars(string type) {
            int stars = 0;
            while (stars < type.Length && type[type.Length - stars - 1] == '*') ++stars;
            return stars;
        }
        public static string StripStars(string type) {
            int stars = CountStars(type);
            if (stars > 0) return type.Substring(0, type.Length - stars);
            return type;
        }
        public static string AddStars(string type, int stars) {
            return type + new String(Enumerable.Repeat('*', stars).ToArray());
        }

        // Converting from t1 to t2
        public static int CompareConversion(string t1, string t2) {
            if (t1 == t2) return 100;
            int t1Stars = CountStars(t1);
            int t2Stars = CountStars(t2);
            if (t1Stars != t2Stars) return -1;
            if (t1Stars > 0) t1 = t1.Substring(0, t1.Length - t1Stars);
            if (t2Stars > 0) t2 = t2.Substring(0, t2.Length - t2Stars);
            switch (t1) {
                case "bool": {
                    switch (t2) {
                        case "auto":
                        case "var": return 80;
                        case "int": return 70;
                        case "double": return 65;
                        case "string": return 60;
                        case "real": return 50;
                    }
                } break;
                case "int": {
                    switch (t2) {
                        case "double": return 85;
                        case "auto":
                        case "var": return 80;
                        case "string": return 60;
                        case "real": return 50;
                        case "bool": return 10;
                    }
                } break;
                case "real": {
                    switch (t2) {
                        case "double": return 85;
                        case "auto":
                        case "var": return 70;
                        case "string": return 60;
                        case "int": return 40;
                        case "bool": return 10;
                    }
                } break;
                case "double": {
                    switch (t2) {
                        case "auto":
                        case "var": return 70;
                        case "string": return 60;
                        case "real": return 45;
                        case "int": return 40;
                        case "bool": return 10;
                    }
                } break;
                case "string": {
                    switch (t2) {
                        case "auto":
                        case "var": return 70;
                        case "real": return 30;
                        case "int": return 30;
                        case "double": return 30;
                    }
                } break;
                case "fn": {
                    switch (t2) {
                        case "auto":
                        case "var": return 70;
                    }
                } break;
                case "auto": {
                    switch(t2) {
                        case "auto":
                        case "var": return 90;
                    }
                } break;
                case "var": {
                    switch (t2) {
                        case "auto": return 50;
                        case "string": return 40;
                        case "real": return 40;
                        case "int": return 40;
                        case "bool": return 40;
                        case "double": return 40;
                    }
                } break;
            }
            return -1;
        }

        public static string GetCleanTypeName(string type) {
            if (type.StartsWith("System.")) type = type.Substring("System.".Length);
            switch (type) {
                case "bool":
                case "Boolean": return "bool";
                case "short":
                case "Int16": return "short";
                case "int":
                case "Int32": return "int";
                case "float":
                case "Single":
                case "XReal": return "real";
                case "string":
                case "String": return "string";
                case "var":
                case "Object": return "var";
            }
            return type;
        }
    }

}
