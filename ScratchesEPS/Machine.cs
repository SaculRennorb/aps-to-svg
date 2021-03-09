using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Security;
using System.Text;
using System.Xml.Serialization;

namespace ScratchesEPS {
  delegate void StackAction(Stack<StackToken> stack);
  
  enum StackTokenType {
    NULL,
    VALUE,
    INTEGER,
    FLOAT,
    STRING,
    MARK,
    IDENTIFIER_LITERAL,
    IDENTIFIER_CALL,
    ACTION,
    PROCEDURE,
    ARRAY,
    MATRIX,
    SAVE,
    FONT,
  }

  struct NamedStackAction {
    public string      Name;
    public StackAction Action;
    public NamedStackAction(string name, StackAction action) {
      Name   = name;
      Action = action;
    }
    
    [DebuggerStepThrough]
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
    
    [DebuggerStepThrough]
    public override string ToString() {
      var s = $"PSDict[{InitialSize}]";
      foreach (var pair in this) {
        s += $"\n  {pair}";
      }
      return s;
    }
  }

  class Procedure : Queue<StackToken> {

    public Procedure(int capacity) 
      : base(capacity) {}
    
    [DebuggerStepThrough]
    public override string ToString() {
      string s = "Proc: {";
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

  // this is only here because of a bu_g in the copmpiler ... i cant use arraysegment<stacktoken> so this is my replacement
  // see https://github.com/dotnet/core/issues/6026
  struct TokenSegment {
    public StackToken[] Array;
    public int          Index;
    public int          _Length;

    public TokenSegment(StackToken[] array) {
      Array  = array;
      Index  = 0;
      _Length = array.Length;
    }
    public TokenSegment(StackToken[] array, int index, int length) {
      Array  = array;
      Index  = index;
      _Length = length;
    }


    public ref StackToken this[int i] => ref Array[Index + i];
    
    public int Length => _Length;
    public TokenSegment Slice(int start) {
      var ind = Index + start;
      return new TokenSegment(Array, ind, _Length - ind);
    }
    public TokenSegment Slice(int start, int length) {
      return new TokenSegment(Array, Index + start, length);
    }

    public void CopyTo(TokenSegment target) {
      System.Array.Copy(Array, Index, target.Array, target.Index, _Length);
    }

    public bool Equals(in TokenSegment other) {
      return Array == other.Array && Index == other.Index && _Length == other._Length;
    }
    
    [DebuggerStepThrough]
    public override string ToString() {
      var s = $"Segment[{_Length}]: [";
      bool first = true;
      for (int i = 0; i < _Length; i++) {
        if(first)
          first = false;
        else
          s += ", ";
        s += Array[Index + i].ToString();
      }
      s += ']';

      return s;
    }
  }

  class CharDict : CustomStringDict<StackToken>, IPSDict {
    public CharDict(int initialSize) 
      : base(initialSize) { }

  }
  class PrivateDict : CustomStringDict<StackToken>, IPSDict {
    public PrivateDict() 
      : base(0) { }

  }

  class Font : CustomStringDict<StackToken>, IPSDict {
    public Font(int initialSize) 
      : base(initialSize) {
      
      Add("FontType"  , new StackToken());
      Add("FontMatrix", new StackToken());
      var encoding = new StackToken[256];
      for (int i = 0; i < encoding.Length; i++) {
        encoding[i].Type  = StackTokenType.INTEGER;
        encoding[i].Value = i;
      }
      Add("Encoding"  , new StackToken(new TokenSegment(encoding)));
      Add("FontBBox"  , new StackToken(new TokenSegment(new [] { new StackToken(0), new StackToken(0), new StackToken(0), new StackToken(0) })));
      Add("PaintType" , new StackToken(0)); //only 0 or 2 allowed
      var charStrings = new CharDict(0);
      Add("CharStrings", new StackToken(charStrings));
      var @private    = new PrivateDict();
      Add("Private"   , new StackToken(@private));
    }
    
    [DebuggerStepThrough]
    public override string ToString() {
      var s = $"Font";
      foreach (var pair in this) {
        s += $"\n  {pair}";
      }
      return s;
    }
  }

  struct StackToken {
    public object           Value;
    public Matrix3x2        Matrix;
    public Procedure        Procedure;
    public bool             ShouldExecute;
    public StackTokenType   Type;
    public IdentifierName   Identifier;
    public NamedStackAction Action;
    public TokenSegment     Array;
    public ArraySegment<char>       String;

    public static StackToken Mark => new StackToken {
      Type = StackTokenType.MARK
    };
    public StackToken(object value) {
      Type = StackTokenType.VALUE;
      Action = default;
      Procedure = null;
      ShouldExecute = default;
      Array = default;
      Identifier = default;
      Matrix = default;
      String = default;
      Value = value;
    }
    public StackToken(int value) {
      Type   = StackTokenType.INTEGER;
      Action = default;
      Procedure  = null;
      ShouldExecute = default;
      Array  = default;
      Identifier = default;
      Matrix = default;
      String = default;
      Value  = value;
    }
    public StackToken(float value) {
      Type   = StackTokenType.FLOAT;
      Action = default;
      Procedure  = null;
      ShouldExecute = default;
      Array  = default;
      Identifier = default;
      Matrix = default;
      String = default;
      Value  = value;
    }
    public StackToken(NamedStackAction action) {
      Type  = StackTokenType.ACTION;
      Value = null;
      Procedure  = null;
      Array  = default;
      Identifier = default;
      Matrix = default;
      String = default;
      Action = action;
      ShouldExecute = true;
    }
    public StackToken(Procedure dc) {
      Type   = StackTokenType.PROCEDURE;
      Value  = null;
      Action = default;
      Array  = default;
      Identifier = default;
      Matrix = default;
      String = default;
      Procedure  = dc;
      ShouldExecute = false;
    }
    public StackToken(IdentifierName identifier, StackTokenType type) {
      Type   = type;
      Value  = null;
      Action = default;
      Procedure  = null;
      Array  = default;
      Matrix = default;
      String = default;
      Identifier = identifier;
      ShouldExecute = type == StackTokenType.IDENTIFIER_CALL;
    }
    public StackToken(in Matrix3x2 mtx) {
      Type   = StackTokenType.MATRIX;
      Value  = null;
      Action = default;
      Procedure  = null;
      ShouldExecute = default;
      Array  = default;
      Identifier = default;
      String = default;
      Matrix = mtx;
    }
    public StackToken(Matrix3x2 mtx) {
      Type   = StackTokenType.MATRIX;
      Value  = null;
      Action = default;
      Procedure  = null;
      ShouldExecute = default;
      Array  = default;
      Identifier = default;
      String = default;
      Matrix = mtx;
    }
    public StackToken(ArraySegment<char> @string) {
      Type   = StackTokenType.STRING;
      Value  = null;
      Action = default;
      Procedure  = null;
      ShouldExecute = default;
      Identifier = default;
      Matrix = default;
      Array  = default;
      String = @string;
    }
    public StackToken(TokenSegment array) {
      Type   = StackTokenType.ARRAY;
      Value  = null;
      Action = default;
      Procedure  = null;
      ShouldExecute = default;
      Identifier = default;
      Matrix = default;
      String = default;
      Array  = array;
    }
    
    [DebuggerStepThrough]
    public override string ToString() {
      switch (Type) {
        case StackTokenType.VALUE          : return $"VALUE: {Value}";
        case StackTokenType.INTEGER        : return $"INTEGER: {Value}";
        case StackTokenType.FLOAT          : return $"FLOAT: {Value}";
        case StackTokenType.ACTION         : return Action.ToString();
        case StackTokenType.IDENTIFIER_LITERAL: return $"Literal: '{Identifier.Name}'";
        case StackTokenType.IDENTIFIER_CALL: return $"Call: '{Identifier.Name}'";
        case StackTokenType.PROCEDURE      : return $"<{(ShouldExecute ? 'x' : 'r')}>{Procedure}";
        case StackTokenType.ARRAY          : return Array.ToString();
        case StackTokenType.STRING         : return $"\"{new string(String)}\"";
        case StackTokenType.FONT           : return $"Font: {Value}";
        default:                             return $"Type: {Type}";
      }
    }
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
      return $"Identifier: '{Name}'";
    }
  }
  
  struct SaveState {
    public StackToken[]    Stack;
    public GraphicsState[] Graphics;
  }

  struct ConstructionObj {
    public StackTokenType   Type;
    public List<StackToken> Array;
    public Procedure        Proc;

    public ConstructionObj(List<StackToken> arrayList) {
      Type  = StackTokenType.ARRAY;
      Proc  = default;
      Array = arrayList;
    }
    public ConstructionObj(Procedure proc) {
      Type  = StackTokenType.PROCEDURE;
      Array = default;
      Proc  = proc;
    }

    public StackToken ToToken() {
      if(Type == StackTokenType.ARRAY)
        return new StackToken(new TokenSegment(Array.ToArray()));
      else {
        return new StackToken(Proc);
      }
    }

    public override string ToString() {
      switch (Type) {
        case StackTokenType.ARRAY    : return $"[{string.Join(", ", Array)}]";
        case StackTokenType.PROCEDURE: return $"{{{string.Join(", ", Proc)}}}";
        default:                       return $"Type: {Type}";
      }
    }
  }

  static class Machine {
    static Dictionary<Keyword, StackToken> _systemDict = new Dictionary<Keyword, StackToken>(32);
    static SysPDDict                       _systemDictWrapper = new SysPDDict(_systemDict);
    static PSDict                          _userDict = new PSDict(32);
    static PSDict                        _statusDict = new PSDict(32) {
      { "product", new StackToken("Cryptus EPS to SVG".ToCharArray()) }
    };

    static Stack<StackToken> _stack = new Stack<StackToken>(128);
    static Stack<IPSDict> _dictStack = new Stack<IPSDict>(8);

    static Stack<ConstructionObj> _objConstructionStack = new Stack<ConstructionObj>(8);

    static RefStack<SaveState> _saveStack = new RefStack<SaveState>(8);

    static bool CurrentProcCalledExit = false;
    static bool CurrentProcCalledStop = false;


    public static void ProcessToken(in Token token) {
      if(token.Type == TokenType.COMMENT || token.Type == TokenType.PDF_TAG)
        return;

      StackToken? stackToken;

      switch (token.Type) {
        case TokenType.BRACKET when token.Content[0] == '[':
          _objConstructionStack.Push(new ConstructionObj(new List<StackToken>(8)));
          Ext.WriteLineYellow($"constructing array (depth {_objConstructionStack.Count})");
          return;
        case TokenType.BRACE when token.Content[0] == '{':
          _objConstructionStack.Push(new ConstructionObj(new Procedure(8)));
          Ext.WriteLineYellow($"constructing procedure (depth {_objConstructionStack.Count})");
          return;
        case TokenType.BRACKET when token.Content[0] == ']':
        case TokenType.BRACE   when token.Content[0] == '}':
          var finishedToken = _objConstructionStack.Pop();
          Ext.WriteLineYellow($"ending {finishedToken.Type} as {finishedToken}");

          if(_objConstructionStack.Count > 0) {
            switch (_objConstructionStack.Peek().Type) {
              case StackTokenType.ARRAY    : _objConstructionStack.Peek().Array.Add    (finishedToken.ToToken()); break;
              case StackTokenType.PROCEDURE: _objConstructionStack.Peek().Proc .Enqueue(finishedToken.ToToken()); break;
            }
          } else {
            _stack.Push(finishedToken.ToToken());
          }
          return;
        default:
          if(_objConstructionStack.Count == 0)
            break;

          switch (_objConstructionStack.Peek().Type) {
            case StackTokenType.ARRAY:
              stackToken = TryGetConstStackToken(in token);
              stackToken ??= new StackToken(new IdentifierName(token.Content), StackTokenType.IDENTIFIER_CALL);
              _objConstructionStack.Peek().Array.Add(stackToken.Value);
              return;
            case StackTokenType.PROCEDURE: 
              stackToken = TryGetConstStackToken(in token);
              stackToken ??= new StackToken(new IdentifierName(token.Content), StackTokenType.IDENTIFIER_CALL);
              _objConstructionStack.Peek().Proc.Enqueue(stackToken.Value);
              return;
          }
          break;
      }
      
      stackToken = TryGetConstStackToken(in token);
      stackToken ??= TryFindStackTokenInDicts(token.Content, token.Keyword, out _);
      if(!stackToken.HasValue) {
        Ext.WriteLineRed($"'{((ReadOnlySpan<char>)token.Content).ToString()}' is currently not defined!");
        throw new Exception();
        return;
      }

      ProcessStackToken(stackToken.Value);
    }

    static void ProcessStackToken(in StackToken sToken, int depth = 0) {
      switch (sToken.Type) {
        case StackTokenType.ACTION:
          Ext.WriteLineYellow($"executing: '{sToken.Action.Name}'");
          sToken.Action.Action.Invoke(_stack);
          break;
        case StackTokenType.IDENTIFIER_CALL:
          Ext.WriteLineYellow($"resolving: '{sToken.Identifier.Name}'");
          var action = TryFindStackTokenInDicts(sToken.Identifier.Name, FindFallback(sToken.Identifier.Name), out _);
          if (!action.HasValue) {
            Ext.WriteLineRed($"'{sToken.Identifier.Name}' is currently not defined!");
            throw new Exception();
          } 

          ProcessStackToken(action.Value);
          break;
        case StackTokenType.PROCEDURE when depth == 0 || sToken.ShouldExecute:
          Ext.WriteLineYellow($"processing: {sToken.Procedure}");
          foreach (var innerST in sToken.Procedure) {
            ProcessStackToken(in innerST, depth + 1);  
          }
          break;
        default:
          Ext.WriteLineYellow($"pushing: {sToken}");
          _stack.Push(sToken);
          break;
      }
    }

    static StackToken? TryGetConstStackToken(in Token token) {
      switch (token.Type) {
        case TokenType.LITERAL_NAME   : return new StackToken(new IdentifierName(token.Content), StackTokenType.IDENTIFIER_LITERAL); //not quite right, doesnt create special chars
        case TokenType.LITERAL_INTEGER: return new StackToken(    Ext.ParseInt  (token.Content));
        case TokenType.LITERAL_DECIMAL: return new StackToken(    Ext.ParseFloat(token.Content));
        case TokenType.LITERAL_BOOLEAN: return new StackToken(        bool.Parse(token.Content));
        case TokenType.LITERAL_STRING : return new StackToken(                   token.Content );
      }

      return null;
    }

    [DebuggerStepThrough]
    static StackToken? TryFindStackTokenInDicts(ReadOnlySpan<char> identifier, Keyword fallback, out IPSDict foundIn) {
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

    [DebuggerStepThrough]
    static Keyword FindFallback(ReadOnlySpan<char> segment) {
      Tokenizer.KeywordMap.TryGetValue(segment, out var kw);
      return kw;
    }

    static Machine() {
      AddSysAction(Keyword.USER_DICT, (s) => {
        Ext.WriteLineGreen("pushing user dict");
        s.Push(new StackToken(_userDict));
      });
      AddSysAction(Keyword.STATUS_DICT, (s) => {
        Ext.WriteLineGreen("pushing status dict");
        s.Push(new StackToken(_statusDict));
      });
      AddSysAction(Keyword.SYSTEM_DICT, (s) => {
        Ext.WriteLineGreen("pushing system dict");
        s.Push(new StackToken(_systemDictWrapper));
      });
      AddSysAction(Keyword.DICT     , (s) => {
        var cap = (int)s.Pop().Value;
        var newDict = new PSDict(cap);
        Ext.WriteLineGreen($"pushing custom dict[{cap}]");
        s.Push(new StackToken(newDict));
      });
      AddSysAction(Keyword.BEGIN    , (s) => {
        var dict = (IPSDict)s.Pop().Value;
        Ext.WriteLineGreen("activating dict");
        _dictStack.Push(dict);
      });
      AddSysAction(Keyword.END      , (_) => {
        Ext.WriteLineGreen("deactivating dict");
        _dictStack.Pop();
      });
      AddSysAction(Keyword.DEF      , (s) => {
        var value  = s.Pop();
        var kToken = s.Pop();
        var key    = kToken.Identifier.Name;
        Ext.WriteLineGreen($"defining '{key}' as {value}");
        _dictStack.Peek().Add(key, value);
      });

      AddSysAction(Keyword.COPY      , (s) => {
        var p1 = s.Pop();
        switch (p1.Type) {
          case StackTokenType.INTEGER: 
            var n = (int)p1.Value;
            Ext.WriteLineGreen($"creating copies of {n} stack elements"); 
            var buffer = new StackToken[n];
            for (int i = 0; i < buffer.Length; i++) {
              buffer[i] = s.Pop();
            }
            for (int i = 0; i < buffer.Length; i++) {
              s.Push(buffer[i]);
            }
            for (int i = 0; i < buffer.Length; i++) {
              s.Push(buffer[i]);
            }
            break;
          case StackTokenType.ARRAY:                                Ext.WriteLineRed($"stub copy {p1.Type}"); break;
          case StackTokenType.VALUE when p1.Value is IPSDict:       Ext.WriteLineRed($"stub copy {p1.Type}"); break;
          case StackTokenType.VALUE when p1.Value is string:        Ext.WriteLineRed($"stub copy {p1.Type}"); break;
          case StackTokenType.VALUE when p1.Value is GraphicsState: Ext.WriteLineRed($"stub copy {p1.Type}"); break;
          default: 
            Ext.WriteLineRed($"copy: first operand type ({p1.Type}) is not in [int, array, dict, string, gState], which is invalid"); 
            throw new Exception();
            break;
        }
      });

      static void RecurseBindProc(ref Procedure proc) {
        var newDC = new Procedure(proc.Count);

        while (proc.TryDequeue(out var oldST)) {
          RecurseBind(ref oldST);
          newDC.Enqueue(oldST);
        }

        proc = newDC;
      }
      static void RecurseBind(ref StackToken stackToken) {
        switch (stackToken.Type) {
          case StackTokenType.IDENTIFIER_CALL: 
            var sa = TryFindStackTokenInDicts(stackToken.Identifier.Name, FindFallback(stackToken.Identifier.Name), out _);
            if(sa.HasValue) {
              var val = sa.Value;
              if(val.Type == StackTokenType.PROCEDURE)
                val.ShouldExecute = true;
              Ext.WriteLineGreen($"replacing {stackToken.Identifier} with {val}");
              stackToken = val;
            } else {
              Ext.WriteLineGreen($"{stackToken.Identifier} is not defined atm");
            }
            break;
          case StackTokenType.PROCEDURE: 
            Ext.WriteLineYellow($"inspecting inner: {stackToken.Procedure}");
            RecurseBindProc(ref stackToken.Procedure);
            break;
        }
      }
      AddSysAction(Keyword.BIND     , (s) => {
        var proc = s.Pop();
        RecurseBindProc(ref proc.Procedure);
        s.Push(proc);
      });
      AddSysAction(Keyword.LOAD     , (s) => {
        var keyToken = s.Pop();
        var key = keyToken.Identifier.Name;
        var value = TryFindStackTokenInDicts(key, FindFallback(key), out _);
        if(value.HasValue) {
          Ext.WriteLineGreen($"loading {keyToken} as {value.Value}");
          s.Push(value.Value);
        } else {
          Ext.WriteLineRed($"loading {keyToken} failed, no representation found!");
          throw new Exception();
        }
      });
      AddSysAction(Keyword.EXEC      , (s) => {
        var proc = s.Pop();
        ProcessStackToken(in proc);
      });
      AddSysAction(Keyword.EXIT, (s) => {
        CurrentProcCalledExit = true;
      });
      AddSysAction(Keyword.LOOP, (s) => {
        var proc = s.Pop();
        Ext.WriteLineGreen($"looping {proc} untill exit is called");
        while(!CurrentProcCalledExit)
          ProcessStackToken(in proc);
        CurrentProcCalledExit = false;
      });
      AddSysAction(Keyword.STOP, (s) => {
        CurrentProcCalledStop = true;
      });AddSysAction(Keyword.STOPPED, (s) => {
        var proc = s.Pop();
        ProcessStackToken(in proc);
        if(CurrentProcCalledStop) {
          Ext.WriteLineGreen($"proc called stop");
          CurrentProcCalledStop = false;
          s.Push(new StackToken(true));
        } else {
          Ext.WriteLineGreen($"proc did not call stop");
          s.Push(new StackToken(false));
        }
      });

      AddSysAction(Keyword.SAVE      , (s) => {
        var stateIndex = _saveStack.UsedSlots;
        var save = new SaveState() {
          Stack    = s.ToArray(),
          Graphics = Graphics.StateStack.ToArray(),
        };
        _saveStack.Push(in save);

        s.Push(new StackToken(stateIndex){ Type = StackTokenType.SAVE });
      });
      AddSysAction(Keyword.RESTORE   , (s) => {
        var saveIndexToken = s.Pop();
        var saveIndex = (int)saveIndexToken.Value;
        var saveState = _saveStack.Data[saveIndex];
        _saveStack.UsedSlots = saveIndex;
        s.Clear();
        foreach (var token in saveState.Stack) {
          s.Push(token);
        }
        Graphics.StateStack.Clear();
        foreach (var state in saveState.Graphics) {
          Graphics.StateStack.Push(in state);
        }
      });

      AddSysAction(Keyword.IF       , (s) => {
        var p1 = s.Pop();
        var cond  = (bool)s.Pop().Value;
        if(cond) {
          Ext.WriteLineGreen("condition was true - processing proc");
          ProcessStackToken(in p1);
        } else {
          Ext.WriteLineGreen("condition was false - doing nothing");
        }
      });
      AddSysAction(Keyword.IFELSE   , (s) => {
        var p2 = s.Pop();
        var p1 = s.Pop();
        var cond  = (bool)s.Pop().Value;
        Ext.WriteLineGreen($"condition was {cond}");
        if(cond) {
          ProcessStackToken(in p1);
        } else {
          ProcessStackToken(in p2);
        }
      });
      AddSysAction(Keyword.KNOWN    , (s) => {
        var  keyToken = s.Pop();
        var dictToken = s.Pop();

        var key = keyToken.Identifier.Name;

        if(((IPSDict)dictToken.Value).TryGetValue(key, out _)) {
          Ext.WriteLineGreen($"found {keyToken} in {dictToken}");
          s.Push(new StackToken(true));
        } else {
          Ext.WriteLineGreen($"did not find {keyToken} in {dictToken}");
          s.Push(new StackToken(false));
        }
      });
      AddSysAction(Keyword.WHERE    , (s) => {
        var key = s.Pop();
        if(TryFindStackTokenInDicts(key.Identifier.Name, FindFallback(key.Identifier.Name), out var dict) != null) {
          Ext.WriteLineGreen($"found {key} in dict {dict}");
          s.Push(new StackToken(dict));
          s.Push(new StackToken(true));
        } else {
          Ext.WriteLineGreen($"did not find {key} in any dict");
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
        Ext.WriteLineGreen($" - token is {val}");
        
        for (int i = n - 1; i >= 0; i--) {
          s.Push(tmp[i]);
        }

        s.Push(val);
      });
      AddSysAction(Keyword.POP      , (s) => {
        var token = s.Pop();
        Ext.WriteLineGreen($"popped {token}");
      });
      AddSysAction(Keyword.PUT      , (s) => {
        var o3 = s.Pop();
        var o2 = s.Pop();
        var o1 = s.Pop();

        switch (o1.Type) {
          case StackTokenType.ARRAY:
            Ext.WriteLineGreen($"replacing index '{o2}' in array '{o1}' width '{o3}'");
            o1.Array[(int)o2.Value] = o3;
            return;
          case StackTokenType.VALUE when o1.Value is IPSDict dict:
            Ext.WriteLineGreen($"replacing key '{o2}' in dict {o1} width value '{o3}'");
            dict[o2.Identifier.Name] = o3;
            return;
          case StackTokenType.STRING:
            Ext.WriteLineGreen($"replacing index '{o2}' in string '{o1}' width '{(char)(int)o3.Value}'");
            o1.String[(int)o2.Value] = (char)(int)o3.Value;
            return;
        }

        Ext.WriteLineRed($"{o1} is neither an array, a dict nor a string, which is not valid.");
      });
      AddSysAction(Keyword.PUT_INTERVAL, (s) => {
        var o3 = s.Pop();
        var o2 = s.Pop();
        var o1 = s.Pop();

        switch (o1.Type) {
          case StackTokenType.ARRAY: {
            Ext.WriteLineGreen($"copying {o3} to '{o1}' at index {o2.Value}");
            o3.Array.CopyTo(o1.Array.Slice((int)o2.Value));
          } return;
          case StackTokenType.STRING: {
            Ext.WriteLineGreen($"copying {o3} to '{o1}' at index {o2.Value}");
            o3.String.CopyTo(o1.String.Slice((int)o2.Value));
          } return;
        }

        Ext.WriteLineRed($"{o1} is neither an array nor a string, which is not valid.");
      });
      AddSysAction(Keyword.GET      , (s) => {
        var o2 = s.Pop();
        var o1 = s.Pop();

        switch (o1.Type) {
          case StackTokenType.ARRAY: {
            var val = o1.Array[(int)o2.Value];
            Ext.WriteLineGreen($"pushing the {o2.Value}th element in '{o1}' ({val}) on the stack");
            s.Push(val);
          } return;
          case StackTokenType.FONT: {
            var font = (Font)o1.Value;
            if(font.TryGetValue(o2.Identifier.Name, out var val)) {
              Ext.WriteLineGreen($"pushing the element with key {o2.Identifier} in {o1} ({val}) on the stack");
              s.Push(val);
            } else {
              Ext.WriteLineRed($"{o1} did not contain an entry for key '{o2.Identifier.Name}'");
              throw new Exception();
            }
          } return;
          case StackTokenType.VALUE when o1.Value is IPSDict dict: {
            if(dict.TryGetValue(o2.Identifier.Name, out var val)) {
              Ext.WriteLineGreen($"pushing the element with key {o2.Identifier} in {o1} ({val}) on the stack");
              s.Push(val);
            } else {
              Ext.WriteLineRed($"key {o2.Identifier} was not found in {o1}!");
              throw new Exception();
            }
          } return;
          case StackTokenType.STRING: {
            var val = o1.String[(int)o2.Value];
            Ext.WriteLineGreen($"pushing the {o2.Value}th char in '{o1}' ({val}) on the stack");
            s.Push(new StackToken((int)val));
          } return;
        }

        Ext.WriteLineRed($"{o1} is neither an array, a dict nor a string, which is not valid.");
      });
      AddSysAction(Keyword.GET_INTERVAL, (s) => {
        var o3 = s.Pop();
        var o2 = s.Pop();
        var o1 = s.Pop();

        switch (o1.Type) {
          case StackTokenType.ARRAY: {
            var val = o1.Array[(int)o2.Value..(int)o3.Value];
            Ext.WriteLineGreen($"pushing slice [{o2.Value}..{o3.Value}] ({val}) on the stack");
            s.Push(new StackToken(val));
          } return;
          case StackTokenType.STRING: {
            var val = o1.String[(int)o2.Value..(int)o3.Value];
            Ext.WriteLineGreen($"pushing slice [{o2.Value}..{o3.Value}] ({val}) on the stack");
            s.Push(new StackToken(val));
          } return;
        }

        Ext.WriteLineRed($"{o1} is neither an array nor a string, which is not valid.");
      });
      AddSysAction(Keyword.DUP      , (s) => {
        s.Push(s.Peek());
      });
      AddSysAction(Keyword.EXCH     , (s) => {
        var t1 = s.Pop();
        var t2 = s.Pop();
        Ext.WriteLineGreen($"exchanging {t1} and {t2}");
        _stack.Push(t1);
        _stack.Push(t2);
      });
      AddSysAction(Keyword.ROLL     , (s) => {
        var j = (int)s.Pop().Value;
        var n = (int)s.Pop().Value;
        Ext.WriteLineGreen($"rolling {n} stack values by {j}");

        var arr = new StackToken[n];
        for (int i = 0; i < n; i++) {
          arr[i] = s.Pop();
        }

        for (int i = 0; i < n; i++) {
          s.Push(arr[Ext.Mod(i + j, n)]);
        }
      });

      AddSysAction(Keyword.AND, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"{t1} and {t2}");
        switch (t1.Type) {
          case StackTokenType.INTEGER:                          s.Push(new StackToken((int)t1.Value & (int)t2.Value)); return;
          case StackTokenType.VALUE when t1.Value is bool bVal: s.Push(new StackToken(bVal && (bool)t2.Value));        return;
        }
        Ext.WriteLineRed($"{t1} is neither a bool nor an int, which is not valid");
      });
      AddSysAction(Keyword.OR, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"{t1} or {t2}");
        switch (t1.Type) {
          case StackTokenType.INTEGER:                          s.Push(new StackToken((int)t1.Value | (int)t2.Value)); return;
          case StackTokenType.VALUE when t1.Value is bool bVal: s.Push(new StackToken(bVal || (bool)t2.Value));        return;
        }
        Ext.WriteLineRed($"{t1} is neither a bool nor an int, which is not valid");
      });
      AddSysAction(Keyword.NOT, (s) => {
        var t1 = s.Pop();
        Ext.WriteLineGreen($"negating {t1}");
        switch (t1.Type) {
          case StackTokenType.INTEGER:                          s.Push(new StackToken(~(int)t1.Value)); return;
          case StackTokenType.VALUE when t1.Value is bool bVal: s.Push(new StackToken(!bVal));          return;
        }
        Ext.WriteLineRed($"{t1} is neither a bool nor an int, which is not valid");
      });
      AddSysAction(Keyword.EQ, (s) => {
        var p2 = s.Pop();
        var p1 = s.Pop();
        Ext.WriteLineGreen($"{p1} == {p2}?");
        bool res = p1.Type == p2.Type;
        if(res) {
          switch (p1.Type) {
            case StackTokenType.FLOAT:
            case StackTokenType.INTEGER: 
              res = p1.Value.Equals(p2.Value);
              break;
            case StackTokenType.ARRAY:
              res = p1.Array.Equals(in p2.Array);
              break;
            default: 
              Ext.WriteLineRed($"ne is not defined for type {p1.Type}");
              break;
          }
        } else {
          if(p1.Type == StackTokenType.INTEGER && p2.Type == StackTokenType.FLOAT 
             || p1.Type == StackTokenType.FLOAT && p2.Type == StackTokenType.INTEGER) {
            res = Ext.ToFloat(p1.Value) == Ext.ToFloat(p2.Value);
          }
        }
        s.Push(new StackToken(res));
      });
      AddSysAction(Keyword.NE, (s) => {
        var p2 = s.Pop();
        var p1 = s.Pop();
        Ext.WriteLineGreen($"{p1} != {p2}?");
        bool res = p1.Type == p2.Type;
        if(res) {
          switch (p1.Type) {
            case StackTokenType.FLOAT:
            case StackTokenType.INTEGER: 
              res = p1.Value.Equals(p2.Value);
              break;
            case StackTokenType.ARRAY:
              res = p1.Array.Equals(in p2.Array);
              break;
            default: 
              Ext.WriteLineRed($"ne is not defined for type {p1.Type}");
              break;
          }
        } else {
          if(p1.Type == StackTokenType.INTEGER && p2.Type == StackTokenType.FLOAT 
             || p1.Type == StackTokenType.FLOAT && p2.Type == StackTokenType.INTEGER) {
            res = Ext.ToFloat(p1.Value) == Ext.ToFloat(p2.Value);
          }
        }
        s.Push(new StackToken(!res));
      });
      AddSysAction(Keyword.LT, (s) => {
        var n2 = s.Pop();
        var n1 = s.Pop();
        Ext.WriteLineGreen($"{n1} < {n2}?");
        if(n1.Type == StackTokenType.FLOAT || n2.Type == StackTokenType.FLOAT) {
          s.Push(new StackToken(Ext.ToFloat(n1.Value) < Ext.ToFloat(n2.Value))); 
        } else if(n1.Type == StackTokenType.INTEGER) {
          s.Push(new StackToken((int)n1.Value < (int)n2.Value));
        } else if(n1.Type == StackTokenType.STRING) {
          Ext.WriteLineRed("lt string stub");
        } else {
          Ext.WriteLineRed($"type of {n1} is not in [int, float, string] which is not valid!");
        }
      });
      AddSysAction(Keyword.GT, (s) => {
        var n2 = s.Pop();
        var n1 = s.Pop();
        Ext.WriteLineGreen($"{n1} > {n2}?");
        if(n1.Type == StackTokenType.FLOAT || n2.Type == StackTokenType.FLOAT) {
          s.Push(new StackToken(Ext.ToFloat(n1.Value) > Ext.ToFloat(n2.Value))); 
        } else if(n1.Type == StackTokenType.INTEGER) {
          s.Push(new StackToken((int)n1.Value > (int)n2.Value));
        } else if(n1.Type == StackTokenType.STRING) {
          Ext.WriteLineRed("gt string stub");
        } else {
          Ext.WriteLineRed($"type of {n1} is not in [int, float, string] which is not valid!");
        }
      });
      AddSysAction(Keyword.LE, (s) => {
        var n2 = s.Pop();
        var n1 = s.Pop();
        Ext.WriteLineGreen($"{n1} <= {n2}?");
        if(n1.Type == StackTokenType.FLOAT || n2.Type == StackTokenType.FLOAT) {
          s.Push(new StackToken(Ext.ToFloat(n1.Value) <= Ext.ToFloat(n2.Value))); 
        } else if(n1.Type == StackTokenType.INTEGER) {
          s.Push(new StackToken((int)n1.Value <= (int)n2.Value));
        } else if(n1.Type == StackTokenType.STRING) {
          Ext.WriteLineRed("le string stub");
        } else {
          Ext.WriteLineRed($"type of {n1} is not in [int, float, string] which is not valid!");
        }
      });
      AddSysAction(Keyword.GE, (s) => {
        var n2 = s.Pop();
        var n1 = s.Pop();
        Ext.WriteLineGreen($"{n1} >= {n2}?");
        if(n1.Type == StackTokenType.FLOAT || n2.Type == StackTokenType.FLOAT) {
          s.Push(new StackToken(Ext.ToFloat(n1.Value) >= Ext.ToFloat(n2.Value))); 
        } else if(n1.Type == StackTokenType.INTEGER) {
          s.Push(new StackToken((int)n1.Value >= (int)n2.Value));
        } else if(n1.Type == StackTokenType.STRING) {
          Ext.WriteLineRed("ge string stub");
        } else {
          Ext.WriteLineRed($"type of {n1} is not in [int, float, string] which is not valid!");
        }
      });

      AddSysAction(Keyword.ADD, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"{t1} + {t2}");
        if(t1.Type == StackTokenType.FLOAT || t2.Type == StackTokenType.FLOAT) {
          s.Push(new StackToken(Ext.ToFloat(t1.Value) + Ext.ToFloat(t2.Value))); 
        } else if(t1.Type == StackTokenType.INTEGER) {
          s.Push(new StackToken((int)t1.Value + (int)t2.Value));
        } else {
          Ext.WriteLineRed($"type of {t1} is not in [int, float] which is not valid!");
        }
      });
      AddSysAction(Keyword.SUB, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"{t1} - {t2}");
        if(t1.Type == StackTokenType.FLOAT || t2.Type == StackTokenType.FLOAT) {
          s.Push(new StackToken(Ext.ToFloat(t1.Value) - Ext.ToFloat(t2.Value))); 
        } else if(t1.Type == StackTokenType.INTEGER) {
          s.Push(new StackToken((int)t1.Value - (int)t2.Value));
        } else {
          Ext.WriteLineRed($"type of {t1} is not in [int, float] which is not valid!");
        }
      });
      AddSysAction(Keyword.MUL, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"{t1} * {t2}");
        if(t1.Type == StackTokenType.FLOAT || t2.Type == StackTokenType.FLOAT) {
          s.Push(new StackToken(Ext.ToFloat(t1.Value) * Ext.ToFloat(t2.Value))); 
        } else if(t1.Type == StackTokenType.INTEGER) {
          s.Push(new StackToken((int)t1.Value * (int)t2.Value));
        } else {
          Ext.WriteLineRed($"type of {t1} is not in [int, float] which is not valid!");
        }
      });
      AddSysAction(Keyword.DIV, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"{t1} / {t2}");

        switch (t1.Type) {
          case StackTokenType.INTEGER:
          case StackTokenType.FLOAT:
            s.Push(new StackToken(Ext.ToFloat(t1.Value) / Ext.ToFloat(t2.Value))); 
            return;
          default: 
            Ext.WriteLineRed($"type of {t1} is not in [int, float] which is not valid!");
            throw new Exception();
        }
      });
      AddSysAction(Keyword.IDIV, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"{t1} i/ {t2}");

        if (t1.Type == StackTokenType.INTEGER) {
          s.Push(new StackToken((int)t1.Value / (int)t2.Value));
        } else {
          Ext.WriteLineRed($"type of {t1} is not int which is not valid!");
          throw new Exception();
        }
      });
      AddSysAction(Keyword.ABS, (s) => {
        var t1 = s.Pop();
        Ext.WriteLineGreen($"absolute of {t1.Value}");
        switch (t1.Value) {
          case int   iVal: s.Push(new StackToken(Math.Abs(iVal))); return;
          case float fVal: s.Push(new StackToken(Math.Abs(fVal))); return;
        }
      });
      AddSysAction(Keyword.ROUND, (s) => {
        var t1 = s.Pop();
        Ext.WriteLineGreen($"round {t1}");
        s.Push(new StackToken((float)Math.Round(Ext.ToFloat(t1.Value))));
      });

      AddSysAction(Keyword.X_CHECK  , (s) => {
        var t1 = s.Pop();
        _stack.Push(new StackToken(t1.Type == StackTokenType.ACTION));
      });
      AddSysAction(Keyword.MARK, (s) => {
        s.Push(StackToken.Mark);
      });
      AddSysAction(Keyword.CLEAR_TO_MARK, (s) => {
        Ext.WriteLineGreen("clearing to mark");
        while (s.Pop().Type != StackTokenType.MARK) /**/;
      });

      AddSysAction(Keyword.SET_DASH , (s) => {
        var offs = s.Pop();
        var arr  = s.Pop();
        Ext.WriteLineGreen($"set dash to {offs} - [{arr}]");
        //Graphics.SetDash((int)offs.Value, arr);
        Ext.WriteLineRed("stub");
      });
      AddSysAction(Keyword.SET_LINE_CAP, (s) => {
        var type = (int)s.Pop().Value;
        Ext.WriteLineGreen($"set line cap to {(LineCap)type}");
        Graphics.SetLineCap(type);
      });
      AddSysAction(Keyword.SET_LINE_JOIN, (s) => {
        var type = (int)s.Pop().Value;
        Ext.WriteLineGreen($"set line join to {(LineJoin)type}");
        Graphics.SetLineJoin(type);
      });
      AddSysAction(Keyword.SET_LINE_WIDTH, (s) => {
        var width = (int)s.Pop().Value;
        Ext.WriteLineGreen($"set line width to {width}");
        Graphics.SetLineWidth(width);
      });
      AddSysAction(Keyword.SET_MITER_LIMIT, (s) => {
        var limit = Ext.ToFloat(s.Pop().Value);
        Ext.WriteLineGreen($"set miter limit to {limit}");
        Graphics.SetMiterLimit(limit);
      });
      AddSysAction(Keyword.SET_FLAT, (s) => {
        var t1 = s.Pop();
        var flatness = Ext.ToFloat(t1.Value);
        Ext.WriteLineGreen($"set flat to {flatness}");
        Graphics.SetFlat(flatness);
      });
      
      AddSysAction(Keyword.NEW_PATH, (s) => {
        Graphics.NewPath();
      });
      AddSysAction(Keyword.MOVE_TO, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"move to <{t1.Value}, {t2.Value}>");
        Graphics.MoveTo(Ext.ToFloat(t1.Value), Ext.ToFloat(t2.Value));
      });
      AddSysAction(Keyword.LINE_TO, (s) => {
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"line to <{t1.Value}, {t2.Value}>");
        Graphics.LineTo(Ext.ToFloat(t1.Value), Ext.ToFloat(t2.Value));
      });
      AddSysAction(Keyword.CURVE_TO, (s) => {
        var t6 = s.Pop();
        var t5 = s.Pop();
        var t4 = s.Pop();
        var t3 = s.Pop();
        var t2 = s.Pop();
        var t1 = s.Pop();
        Ext.WriteLineGreen($"curve to <{t1.Value}, {t2.Value}> <{t3.Value}, {t4.Value}> <{t5.Value}, {t6.Value}>");
        Graphics.CurveTo(
          Ext.ToFloat(t1.Value), Ext.ToFloat(t2.Value),
          Ext.ToFloat(t3.Value), Ext.ToFloat(t4.Value),
          Ext.ToFloat(t5.Value), Ext.ToFloat(t5.Value)
        );
      });
      AddSysAction(Keyword.CLOSE_PATH, (s) => {
        Graphics.ClosePath();
      });
      AddSysAction(Keyword.FILL, (s) => {
        Graphics.Fill();
      });
      AddSysAction(Keyword.EO_FILL, (s) => {
        Graphics.EOFill();
      });
      AddSysAction(Keyword.STROKE, (s) => {
        Graphics.Stroke();
      });
      AddSysAction(Keyword.CLIP, (s) => {
        Graphics.Clip();
      });
      AddSysAction(Keyword.EO_CLIP, (s) => {
        Graphics.EOClip();
      });

      AddSysAction(Keyword.G_SAVE, (s) => {
        Graphics.GSave();
      });
      AddSysAction(Keyword.G_RESTORE, (s) => {
        Graphics.GRestore();
      });
      AddSysAction(Keyword.SET_TRANSFER, (s) => {
        var proc = s.Pop();
        Ext.WriteLineGreen($"set color transfer function to {proc}");
        Ext.WriteLineRed("stub");
      });
      AddSysAction(Keyword.SET_RGB_COLOR, (s) => {
        var t3 = s.Pop();
        var t2 = s.Pop();
        var t1 = s.Pop();

        var r = Math.Clamp(Ext.ToFloat(t1.Value), 0, 1);
        var g = Math.Clamp(Ext.ToFloat(t2.Value), 0, 1);
        var b = Math.Clamp(Ext.ToFloat(t3.Value), 0, 1);

        Ext.WriteLineGreen($"setting color to RGB({r}, {g}, {b}) ");
        Graphics.SetRGBColor(r, g, b);
      });
      AddSysAction(Keyword.SET_GRAY, (s) => {
        var t1 = s.Pop();

        var gray = Math.Clamp(Ext.ToFloat(t1.Value), 0, 1);

        Ext.WriteLineGreen($"setting color to RGB({gray}) ");
        Graphics.SetGray(gray);
      });

      AddSysAction(Keyword.MATRIX, (s) => {
        Ext.WriteLineGreen("pushing identity mtx to the stack");
        s.Push(new StackToken(Matrix3x2.Identity));
      });
      AddSysAction(Keyword.DEFAULT_MATRIX, (s) => {
        s.Pop();
        Ext.WriteLineGreen("pushing default output mtx to the stack");
        s.Push(new StackToken(Graphics.DefaultOutputMtx));
      });
      AddSysAction(Keyword.CONCAT, (s) => {
        var m1 = s.Pop().Matrix;
        ref var gState = ref Graphics.StateStack.Peek();
        gState.CTM = Matrix3x2.Multiply(m1, gState.CTM);
        Ext.WriteLineGreen($"applying {m1} to user space which now is {gState.CTM}");
      });
      AddSysAction(Keyword.INVERT_MATRIX, (s) => {
        var m1 = s.Pop().Matrix;
        var m2 = s.Pop().Matrix;

        Ext.WriteLineGreen($"inverting {m1}");

        if(!Matrix3x2.Invert(m1, out m2))
          Ext.WriteLineRed("invert failed");

        s.Push(new StackToken(in m2));
      });
      AddSysAction(Keyword.SET_MATRIX, (s) => {
        var m1 = s.Pop().Matrix;
        Ext.WriteLineGreen($"setting user space to {m1}");
        Graphics.StateStack.Peek().CTM = m1;
      });
      AddSysAction(Keyword.CURRENT_MATRIX, (s) => {
        var m1 = s.Pop().Matrix;
        var mtx = Graphics.StateStack.Peek().CTM;
        Ext.WriteLineGreen($"replacing {m1} with the current user space matrix {mtx} on the stack");
        s.Push(new StackToken(mtx));
      });
      AddSysAction(Keyword.TRANSLATE, (s) => {
        var t3 = s.Pop();
        var t2 = s.Pop();
        if(t3.Type == StackTokenType.MATRIX) {
          var t1 = s.Pop();
          var v = new Vector2(Ext.ToFloat(t1.Value), Ext.ToFloat(t2.Value));
          var m = t3.Matrix;
          Ext.WriteLineGreen($"translating origin of custom mtx {m} by {v}");
          m = Matrix3x2.Multiply(Matrix3x2.CreateTranslation(v), m);
          s.Push(new StackToken(m));
        } else {
          var v = new Vector2(Ext.ToFloat(t2.Value), Ext.ToFloat(t3.Value));
          Ext.WriteLineGreen($"translating origin of CTM by {v}");
          ref var gState = ref Graphics.StateStack.Peek();
          gState.CTM = Matrix3x2.Multiply(Matrix3x2.CreateTranslation(v), gState.CTM);
        }
      });
      AddSysAction(Keyword.TRANSFORM, (s) => {
        var p3 = s.Pop();
        var p2 = Ext.ToFloat(s.Pop().Value);

        Vector2   v;
        Matrix3x2 m;
        if(p3.Type == StackTokenType.MATRIX) {
          var p1 = Ext.ToFloat(s.Pop().Value);
          v = new Vector2(p1, p2);
          Ext.WriteGreen($"transforming {v} by custom {p3.Matrix}");
          m = p3.Matrix;
        } else {
          v = new Vector2(p2, Ext.ToFloat(p3.Value));
          m = Graphics.StateStack.Peek().CTM;
          Ext.WriteGreen($"transforming {v} by CTM {m}");
        }

        var vRes = Vector2.Transform(v, m);
        Ext.WriteLineGreen($" - result is {v}");
        s.Push(new StackToken(vRes.X));
        s.Push(new StackToken(vRes.Y));
      });
      AddSysAction(Keyword.I_TRANSFORM, (s) => {
        var p3 = s.Pop();
        var p2 = Ext.ToFloat(s.Pop().Value);

        Vector2   v;
        Matrix3x2 m;
        if(p3.Type == StackTokenType.MATRIX) {
          var p1 = Ext.ToFloat(s.Pop().Value);
          v = new Vector2(p1, p2);
          Ext.WriteGreen($"transforming {v} by inverse of custom {p3.Matrix}");
          m = p3.Matrix;
        } else {
          v = new Vector2(p2, Ext.ToFloat(p3.Value));
          m = Graphics.StateStack.Peek().CTM;
          Ext.WriteGreen($"transforming {v} by inverse of CTM {m}");
        }

        //inverse transform
        if(!Matrix3x2.Invert(m, out var tmp)) {
          Ext.WriteLineRed(" can't invert this matrix!");
          throw new Exception();
        }
        m = tmp;

        var vRes = Vector2.Transform(v, m);
        Ext.WriteLineGreen($" - result is {v}");
        s.Push(new StackToken(vRes.X));
        s.Push(new StackToken(vRes.Y));
      });

      AddSysAction(Keyword.CURRENT_GRAY, (s) => {
        float gray = Graphics.GetCurrentGray();
        Ext.WriteLineGreen($"pushing current gray value {gray} on the stack");
        s.Push(new StackToken(gray));
      });

      AddSysAction(Keyword.D_TRANSFORM, (s) => {
        var p3 = s.Pop();
        var p2 = Ext.ToFloat(s.Pop().Value);

        Vector2   v;
        Matrix3x2 m;
        if(p3.Type == StackTokenType.MATRIX) {
          var p1 = Ext.ToFloat(s.Pop().Value);
          v = new Vector2(p1, p2);
          Ext.WriteGreen($"d_transforming {v} by custom {p3.Matrix}");
          m = p3.Matrix;
        } else {
          v = new Vector2(p2, Ext.ToFloat(p3.Value));
          m = Graphics.StateStack.Peek().CTM;
          Ext.WriteGreen($"d_transforming {v} by CTM {m}");
        }

        //dtransform ignores the translate
        m = new Matrix3x2(m.M11, m.M12, m.M21, m.M22, 0, 0);

        var vRes = Vector2.Transform(v, m);
        Ext.WriteLineGreen($" - result is {v}");
        s.Push(new StackToken(vRes.X));
        s.Push(new StackToken(vRes.Y));
      });

      AddSysAction(Keyword.CURRENT_FLAT, (s) => {
        ref var gState = ref Graphics.StateStack.Peek();
        var f = gState.Flatness;
        Ext.WriteLineGreen($"pushing current flatness {f} on the stack");
        s.Push(new StackToken(f));
      });
      AddSysAction(Keyword.CURRENT_TRANSFER, (s) => {
        var f = new Procedure(0);
        Ext.WriteLineGreen($"pushing current transfer function {f} on the stack");
        s.Push(new StackToken(f));
        Ext.WriteLineRed("current transfer stub");
      });

      AddSysAction(Keyword.STRING, (s) => {
        var ln = (int)s.Pop().Value;
        Ext.WriteLineGreen($"pushing new string of ln {ln} on the stack");
        s.Push(new StackToken(new char[ln]));
      });
      AddSysAction(Keyword.VERSION, (s) => {
        Ext.WriteLineGreen($"pushing version string on the stack");
        s.Push(new StackToken("0.0".ToCharArray()));
      });
      AddSysAction(Keyword.ANCHOR_SEARCH, (s) => {
        var seekToken = s.Pop();
        var  strToken = s.Pop();

        Ext.WriteLineGreen($"searching for {seekToken} in {strToken}");

        var seek = seekToken.String;
        var str  =  strToken.String;

        if(str.Count >= seek.Count && ((ReadOnlySpan<char>)seek).SequenceEqual(str.Slice(0, seek.Count))) {
          var postToken = new StackToken(str.Slice(seek.Count));
          Ext.WriteLineGreen($"did match, post is '{postToken}'");
          s.Push(postToken);
          s.Push(seekToken);
          s.Push(new StackToken(true));
        } else {
          Ext.WriteLineGreen($"did not match");
          s.Push(strToken);
          s.Push(new StackToken(false));
        }
      });

      AddSysAction(Keyword.ARRAY, (s) => {
        var ln = (int)s.Pop().Value;
        Ext.WriteLineGreen($"pushing new array of ln {ln} on the stack");
        s.Push(new StackToken(new TokenSegment(new StackToken[ln])));
      });
      AddSysAction(Keyword.LENGTH, (s) => {
        var t1 = s.Pop();
        Ext.WriteLineGreen($"pushing length of {t1} on the stack");
        switch (t1.Type) {
          case StackTokenType.ARRAY             : s.Push(new StackToken(t1.Array._Length));            return;
          case StackTokenType.VALUE when t1.Value is IPSDict dict: s.Push(new StackToken(dict.Count)); return;
          case StackTokenType.STRING            : s.Push(new StackToken(t1.String.Count));             return;
          case StackTokenType.IDENTIFIER_LITERAL: s.Push(new StackToken(t1.Identifier.Name.Length));   return;
        }
        
        Ext.WriteLineRed($"type of {t1} is not in [array, dict, string, identifier], this is invalid!");
      });
      AddSysAction(Keyword.A_STORE, (s) => {
        var arrayToken = s.Pop();
        var array = arrayToken.Array;
        Ext.WriteLineGreen($"storing {array._Length} elements in the array {arrayToken}");
        for (int i = array._Length - 1; i >= 0; i--) {
         array[i] = s.Pop(); 
        }
        s.Push(arrayToken);
      });
      AddSysAction(Keyword.A_LOAD, (s) => {
        var arrayToken = s.Pop();
        var array = arrayToken.Array;
        Ext.WriteLineGreen($"loading all elements from {arrayToken}");
        for (int i = 0; i < array._Length; i++) {
          s.Push(array[i]);
        }
        s.Push(arrayToken);
      });
      AddSysAction(Keyword.NULL, (s) => {
        Ext.WriteLineGreen($"pushing null on the stack");
        s.Push(new StackToken(){ Type = StackTokenType.NULL });
      });


      AddSysAction(Keyword.CVR, (s) => {
        var p = s.Pop();
        Ext.WriteLineGreen($"converting {p} to real (float)");

        switch (p.Type) {
          case StackTokenType.INTEGER: s.Push(new StackToken((float)(int)p.Value));      return;
          case StackTokenType.FLOAT  : s.Push(p);                                        return;
          case StackTokenType.STRING : s.Push(new StackToken(Ext.ParseFloat(p.String))); return;
        }

        Ext.WriteLineRed($"type of {p} is not in [int, float, string], this in invalid!");
      });

      AddSysAction(Keyword.FIND_FONT, (s) => {
        var fontName = s.Pop().Identifier;
        Ext.WriteLineGreen($"searching for font '{fontName}'");
        s.Push(new StackToken(new Font(8)){ Type = StackTokenType.FONT });
        Ext.WriteLineRed($"findfont stub");
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
        if(!Tokenizer.KeywordMap.TryGetValue(key, out var keyword)) {
          value = default;
          return false;
        }
        return _sysDict.TryGetValue(keyword, out value);
      }
      public StackToken this[string key] {
        get => _sysDict[Tokenizer.KeywordMap[key]];
        set => _sysDict[Tokenizer.KeywordMap[key]] = value;
      }

      public ICollection<string> Keys => throw new NotImplementedException();

      public ICollection<StackToken> Values => _sysDict.Values;

      public override string ToString() {
        var s = $"SYSdict";
        foreach (var pair in this) {
          s += $"\n  {pair}";
        }
        return s;
      }
    }
  }
}
