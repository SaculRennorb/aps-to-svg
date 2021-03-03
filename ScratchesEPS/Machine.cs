using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ScratchesEPS {
  delegate void StackAction(Stack<StackToken> stack);
  struct NamedStackAction {
    public string      Name;
    public StackAction Action;
    public NamedStackAction(string name, StackAction action) {
      Name   = name;
      Action = action;
    }

    public override string ToString() {
      return $"NamedAction '{Name}'";
    }
  }
  
  interface IPSDict : IDictionary<string, StackToken> {
    public bool TryGetValue(ReadOnlySpan<char> key, out StackToken value);
  }
  class PSDict : CustomStringDict<StackToken>, IPSDict {
    public readonly int InitialSize;

    public PSDict(int initialSize) 
      : base(initialSize) {
      InitialSize = initialSize;
    }

    public override string ToString() {
      var s = $"PSDict[{InitialSize}]";
      foreach (var pair in this) {
        s += $"\n  {pair}";
      }
      return s;
    }
  }

  class DeferredContext : Queue<StackToken> {
    public DeferredContext(int capacity) 
      : base(capacity) {}

    public override string ToString() {
      string s = "DC: {";
      bool first = true;
      foreach (var token in this) {
        if(first) 
          first = false;
        else
          s += ", ";
        
        s += token.ToString();
      }
      s += '}';
      return s;
    }
  }

  struct StackToken {
    public StackTokenType   Type;
    public object           Value;
    public NamedStackAction Action;
    public DeferredContext  DContext;
    public IdentifierName   IDName;
    public Matrix3x2        Matrix;

    public StackToken(object value) {
      Type = StackTokenType.VALUE;
      Action = default;
      DContext = null;
      IDName = default;
      Matrix = default;
      Value = value;
    }
    public StackToken(NamedStackAction action) {
      Type   = StackTokenType.ACTION;
      Value  = null;
      DContext = null;
      IDName = default;
      Matrix = default;
      Action = action;
    }
    public StackToken(DeferredContext dc) {
      Type   = StackTokenType.DEFERRED_SCOPE;
      Value  = null;
      Action = default;
      IDName = default;
      Matrix = default;
      DContext = dc;
    }
    public StackToken(IdentifierName idName) {
      Type   = StackTokenType.IDENTIFIER_NAME;
      Value  = null;
      Action = default;
      DContext = null;
      Matrix = default;
      IDName = idName;
    }
    public StackToken(in Matrix3x2 mtx) {
      Type   = StackTokenType.IDENTIFIER_NAME;
      Value  = null;
      Action = default;
      DContext = null;
      IDName = default;
      Matrix = mtx;
    }

    public override string ToString() {
      switch (Type) {
        case StackTokenType.VALUE:           return $"VALUE: {Value}";
        case StackTokenType.ACTION:          return Action.ToString();
        case StackTokenType.IDENTIFIER_NAME: return IDName.ToString();
        case StackTokenType.DEFERRED_SCOPE:  return DContext.ToString();
        default: return $"Type: {Type}";
      }
    }
  }

  enum StackTokenType {
    UNKNOWN,
    VALUE,
    IDENTIFIER_NAME,
    ACTION,
    DEFERRED_SCOPE,
    ARRAY,
    MATRIX,
  }

  struct IdentifierName {
    public string Name;

    public IdentifierName(string name) {
      Name = name;
    }
    public IdentifierName(ReadOnlySpan<char> name) {
      Name = name.ToString();
    }

    public override string ToString() {
      return $"IDName: '{Name}'";
    }
  }

  struct Machine {
    static Dictionary<Keyword, StackToken> _systemDict = new Dictionary<Keyword, StackToken>(32);
    static SysPDDict                       _systemDictWrapper = new SysPDDict(_systemDict);
    static PSDict                            _userDict = new PSDict(32);

    static Stack<StackToken> _stack = new Stack<StackToken>(128);
    static Stack<IPSDict> _dictStack = new Stack<IPSDict>(8);

    static int _deferredState = 0;

    public static void ProcessToken(in Token token) {
      if(token.Type == TokenType.COMMENT || token.Type == TokenType.PDF_TAG)
        return;

      StackToken? stackToken;

      switch (token.Type) {
        case TokenType.BRACE when token.Content[0] == '}':
          Ext.WriteYellow($"ending deferred scope as {_stack.Peek()}");
          _deferredState--;
          if(_deferredState != 0) {
            var dcT = _stack.Pop();
            _stack.Peek().DContext.Enqueue(dcT);
          }
          return;
        case TokenType.BRACE when token.Content[0] == '{':
          Ext.WriteYellow("beginning deferred scope");
          _stack.Push(new StackToken(new DeferredContext(8)));
          _deferredState++;
          return;
        default: if(_deferredState > 0) {
          stackToken = GetStackToken(in token);
          stackToken ??= new StackToken(new IdentifierName(token.Content));
          _stack.Peek().DContext.Enqueue(stackToken.Value);
          return;
        } break;
      }
      
      stackToken = GetStackToken(in token);
      stackToken ??= TryFindAction(token.Content, token.Keyword, out _);
      if(!stackToken.HasValue) {
        Ext.WriteRed($"No corresponding token is defined for '{((ReadOnlySpan<char>)token.Content).ToString()}'");
        return;
      }

      ProcessStackToken(stackToken.Value);
    }

    static void ProcessStackToken(in StackToken sToken) {
      switch (sToken.Type) {
        case StackTokenType.ACTION:
          Ext.WriteYellow($"executing: {sToken.Action.Name}");
          sToken.Action.Action.Invoke(_stack);
          break;
        case StackTokenType.DEFERRED_SCOPE:
          Ext.WriteYellow($"processing: {sToken.DContext}");
          foreach (var innerST in sToken.DContext) {
            ProcessStackToken(in innerST);  
          }
          break;
        default:
          Ext.WriteYellow($"pushing: {sToken}");
          _stack.Push(sToken);
          break;
      }
    }

    static StackToken? GetStackToken(in Token token) {
      switch (token.Type) {
        case TokenType.NAME_DEF:        return new StackToken(new IdentifierName(token.Content)); //not quite right, doesnt create special chars
        case TokenType.LITERAL_INTEGER: return new StackToken(         int.Parse(token.Content));
        case TokenType.LITERAL_DECIMAL: return new StackToken(      double.Parse(token.Content));
        case TokenType.LITERAL_BOOLEAN: return new StackToken(        bool.Parse(token.Content));
      }

      return null;
    }

    static StackToken? TryFindAction(ReadOnlySpan<char> identifier, Keyword fallback, out IPSDict foundIn) {
      StackToken stackToken;
      foreach (var dict in _dictStack) {
        if (dict.TryGetValue(identifier, out stackToken)) {
          foundIn = dict;
          return stackToken;
        }
      }
      if (_userDict.TryGetValue(identifier, out stackToken)) {
        foundIn = _userDict;
        return stackToken;
      }
      if (_systemDict.TryGetValue(fallback, out stackToken)) {
        foundIn = _systemDictWrapper;
        return stackToken;
      }

      foundIn = null;
      return null;
    }

    static Keyword FindFallback(ReadOnlySpan<char> segment) {
      Tokenizer.KeywordMap.TryGetValue(segment, out var kw);
      return kw;
    }

    static Machine() {
      AddSysAction(Keyword.USER_DICT, (s) => {
        Ext.WriteGreen("pushing userdict");
        s.Push(new StackToken(_userDict));
      });
      AddSysAction(Keyword.DICT     , (s) => {
        var cap = (int)s.Pop().Value;
        var newDict = new PSDict(cap);
        Ext.WriteGreen($"pushing custom dict[{cap}]");
        s.Push(new StackToken(newDict));
      });
      AddSysAction(Keyword.BEGIN    , (s) => {
        var dict = (IPSDict)s.Pop().Value;
        Ext.WriteGreen("activating dict");
        _dictStack.Push(dict);
      });
      AddSysAction(Keyword.END      , (_) => {
        Ext.WriteGreen("deactivating dict");
        _dictStack.Pop();
      });
      AddSysAction(Keyword.DEF      , (s) => {
        var value = s.Pop();
        var key   = s.Pop().IDName.Name;
        Ext.WriteGreen($"defining {key} as {value}");
        _dictStack.Peek().Add(key, value);
      });

      static void RecurseBind(ref StackToken stackToken) {
        switch (stackToken.Type) {
          case StackTokenType.IDENTIFIER_NAME: 
            var sa = TryFindAction(stackToken.IDName.Name, FindFallback(stackToken.IDName.Name), out _);
            if(!sa.HasValue) {
              Ext.WriteRed($"No corresponding token is defined for '{stackToken.IDName.Name}'");
              return;
            }
            Ext.WriteGreen($"replacing {stackToken.IDName.Name} with {sa.Value.Action}");
            stackToken = sa.Value;
            break;
          case StackTokenType.DEFERRED_SCOPE: 
            var newDC = new DeferredContext(stackToken.DContext.Count);
            Ext.WriteGreen($"inspecting inner: {stackToken.DContext}");

            while (stackToken.DContext.TryDequeue(out var oldST)) {
              RecurseBind(ref oldST);
              newDC.Enqueue(oldST);
            }

            stackToken.DContext = newDC;
            break;
        }
      }
      AddSysAction(Keyword.BIND     , (s) => {
        var stackToken = s.Pop();
        RecurseBind(ref stackToken);
        s.Push(stackToken);
      });
      AddSysAction(Keyword.IF       , (s) => {
        var p1 = s.Pop();
        var b  = (bool)s.Pop().Value;
        if(b) {
          Ext.WriteGreen($"processing p1: {p1}");
          ProcessStackToken(in p1);
        } else {
          Ext.WriteGreen("b was false");
        }
      });
      AddSysAction(Keyword.IFELSE   , (s) => {
        var p2 = s.Pop();
        var p1 = s.Pop();
        var b  = (bool)s.Pop().Value;
        if(b) {
          Ext.WriteGreen($"processing p1: {p1}");
          ProcessStackToken(in p1);
        } else {
          Ext.WriteGreen($"processing p2: {p2}");
          ProcessStackToken(in p1);
        }
      });
      AddSysAction(Keyword.WHERE    , (s) => {
        var key = s.Pop();
        if(TryFindAction(key.IDName.Name, FindFallback(key.IDName.Name), out var dict) != null) {
          s.Push(new StackToken(dict));
          s.Push(new StackToken(true));
        } else {
          s.Push(new StackToken(false));
        }
      });
      AddSysAction(Keyword.INDEX    , (s) => {
        var n = (int)s.Pop().Value;
        Ext.WriteGreen($"pulling {n}th element from the stack");

        var tmp = new StackToken[n];
        for (int i = 0; i < n; i++) {
          tmp[i] = s.Pop();
        }

        var val = s.Peek();
        
        for (int i = n - 1; i >= 0; i--) {
          s.Push(tmp[i]);
        }

        s.Push(val);
      });
      AddSysAction(Keyword.POP      , (s) => {
        s.Pop();
      });
      AddSysAction(Keyword.DUP      , (s) => {
        s.Push(s.Peek());
      });
      AddSysAction(Keyword.EXCH     , (s) => {
        var t1 = s.Pop();
        var t2 = s.Pop();
        Ext.WriteGreen($"exchanging {t1} and {t2}");
        _stack.Push(t1);
        _stack.Push(t2);
      });

      AddSysAction(Keyword.X_CHECK  , (s) => {
        var t1 = s.Pop();
        _stack.Push(new StackToken(t1.Type == StackTokenType.ACTION));
      });

      AddSysAction(Keyword.SET_DASH , (s) => {
        var offs = s.Pop();
        var arr  = s.Pop();
        Ext.WriteGreen($"set dash to {offs} - [{arr}]");
        //Graphics.SetDash((int)offs.Value, arr);
        Ext.WriteRed("stub");
      });
      AddSysAction(Keyword.SET_LINE_CAP, (s) => {
        var type = (int)s.Pop().Value;
        Ext.WriteGreen($"set line cap to {(LineCap)type}");
        Graphics.SetLineCap(type);
      });
      AddSysAction(Keyword.SET_LINE_JOIN, (s) => {
        var type = (int)s.Pop().Value;
        Ext.WriteGreen($"set line join to {(LineJoin)type}");
        Graphics.SetLineJoin(type);
      });
      AddSysAction(Keyword.SET_LINE_WIDTH, (s) => {
        var width = (int)s.Pop().Value;
        Ext.WriteGreen($"set line width to {width}");
        Graphics.SetLineWidth(width);
      });
      AddSysAction(Keyword.SET_MITER_LIMIT, (s) => {
        var limit = (int)s.Pop().Value;
        Ext.WriteGreen($"set miter limit to {limit}");
        Graphics.SetMiterLimit(limit);
      });
      
      AddSysAction(Keyword.NEW_PATH, (s) => {
        Graphics.NewPath();
      });
      AddSysAction(Keyword.CLOSE_PATH, (s) => {
        Graphics.ClosePath();
      });

      AddSysAction(Keyword.G_SAVE, (s) => {
        Graphics.GSave();
      });
      AddSysAction(Keyword.G_RESTORE, (s) => {
        Graphics.GRestore();
      });
      AddSysAction(Keyword.MATRIX, (s) => {
        Ext.WriteGreen("pushing identity matrix to the stack");
        s.Push(new StackToken(Matrix3x2.Identity));
      });
      AddSysAction(Keyword.CONCAT, (s) => {
        var m1 = s.Pop().Matrix;
        Graphics.UserSpace = Matrix3x2.Multiply(m1, Graphics.UserSpace);
        Ext.WriteGreen($"applying {m1} to user space which now is {Graphics.UserSpace}");
      });
      AddSysAction(Keyword.INVERT_MATRIX, (s) => {
        var m1 = s.Pop().Matrix;
        var m2 = s.Pop().Matrix;

        Ext.WriteGreen($"inverting {m1}");

        if(!Matrix3x2.Invert(m1, out m2))
          Ext.WriteRed("invert failed");

        s.Push(new StackToken(m2));
      });
    }
    
    static void AddSysAction(Keyword kw, StackAction action) {
      _systemDict.Add(kw, new StackToken(new NamedStackAction(Enum.GetName(typeof(Keyword), kw).Replace("_", string.Empty).ToLower(), action)));
    }

    class SysPDDict : IPSDict {
      private readonly Dictionary<Keyword, StackToken> _sysDict;

      public SysPDDict(Dictionary<Keyword, StackToken> sysDict) {
        _sysDict = sysDict;
      }
      IEnumerator<KeyValuePair<string, StackToken>> IEnumerable<KeyValuePair<string, StackToken>>.GetEnumerator() {
        throw new NotImplementedException();
      }
      public IEnumerator GetEnumerator() {
        return ((IEnumerable)_sysDict).GetEnumerator();
      }
      public void Add(KeyValuePair<string, StackToken> item) {
        _sysDict.Add(Tokenizer.KeywordMap[item.Key], item.Value);
      }
      public void Clear() {
        _sysDict.Clear();
      }
      public bool Contains(KeyValuePair<string, StackToken> item) {
        throw new NotImplementedException();
      }
      public void CopyTo(KeyValuePair<string, StackToken>[] array, int arrayIndex) {
        throw new NotImplementedException();
      }
      public bool Remove(KeyValuePair<string, StackToken> item) {
        throw new NotImplementedException();
      }
      public int Count => _sysDict.Count;

      public bool IsReadOnly => false;

      public void Add(string key, StackToken value) {
        _sysDict.Add(Tokenizer.KeywordMap[key], value);
      }
      public bool ContainsKey(string key) {
        return _sysDict.ContainsKey(Tokenizer.KeywordMap[key]);
      }
      public bool Remove(string key) {
        return _sysDict.Remove(Tokenizer.KeywordMap[key]);
      }
      public bool TryGetValue(string key, out StackToken value) {
        return _sysDict.TryGetValue(Tokenizer.KeywordMap[key], out value);
      }
      public bool TryGetValue(ReadOnlySpan<char> key, out StackToken value) {
        return _sysDict.TryGetValue(Tokenizer.KeywordMap[key.ToString()], out value);
      }
      public StackToken this[string key] {
        get => _sysDict[Tokenizer.KeywordMap[key]];
        set => _sysDict[Tokenizer.KeywordMap[key]] = value;
      }

      public ICollection<string> Keys => throw new NotImplementedException();

      public ICollection<StackToken> Values => _sysDict.Values;
    }
  }
}
