using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using static ScratchesEPS.Ext;

namespace ScratchesEPS {
  class Program {
    static void Main(string[] args) {
      string file;
      using(var s = File.OpenRead("./input.eps"))
      using(var sr = new StreamReader(s)) {
        file = sr.ReadToEnd();
      }

      var tokenizer = new Tokenizer(file.ToCharArray());

      Token t;
      while((t = tokenizer.GetNextToken()).Type != TokenType.EOF) {
        if(t.Type == TokenType.UNKNOWN) {
          Console.Write("\u001b[31m");
          Console.Write(t.ToString());
          Console.Write("\u001b[0m");
        } else {
          Console.Write(t.ToString());
        }
        
        Console.WriteLine();
      }
    }
  }

  
  enum TokenType {
    UNKNOWN,
    PDF_TAG,
    COMMENT,
    LITERAL_INTEGER,
    LITERAL_DECIMAL,
    LITERAL_BOOLEAN,
    LITERAL_STRING,
    LITERAL_STRING_HEX,
    KEYWORD,
    BRACE,
    BRACKETS,
    NAME_DEF,
    EOF
  }

  enum Keyword {
    UNKNOWN,
    BIND,

    LITERAL_TRUE,
    LITERAL_FALSE,
    AND,
    OR,
    BITSHIFT,
    XOR,
    LE,
    GE,
    LT,
    GT,
    NOT,
    EQ,
    NE,

    ABS,
    CVI,
    CVR,
    FLOOR,
    CEILING,
    MOD,
    SIN,
    COS,
    ATAN,
    ADD,
    SUB,
    MUL,
    IDIV,
    DIV,
    SQRT,
    NEG,
    EXP,
    LN,
    LOG,
    ROUND,
    TRUNCATE,

    NEW_PATH,
    SET_LINE_CAP,
    SET_DASH,
    SET_LINE_JOIN,
    SET_LINE_WIDTH,
    SET_MITER_LIMIT,
    MOVE_TO,
    R_MOVE_TO,
    LINE_TO,
    R_LINE_TO,
    CURVE_TO,
    R_CURVE_TO,
    ARC,
    ARCN,
    ARCT,
    ARC_TO,
    CLOSE_PATH,
    FILL,
    STROKE,

    SET_GRAY,
    SET_RGB_COLOR,
    SET_HSB_COLOR,
    SET_CMYK_COLOR,
    SET_COLOR,
    SET_PATTERN,

    CLIP,
    CLIP_PATH,
    CLIP_SAVE,
    CLIP_RESTORE,

            G_STATE,
    CURRENT_G_STATE,
        SET_G_STATE,
            G_SAVE,
            G_RESTORE,



    MATRIX,

    TRANSLATE,
    ROTATE,
    SCALE,
    CONCAT,

    PUSH,
    POP,
    DUP,
    EXCH,
    COPY,
    MARK,
    INDEX,
    ROLL,
    CLEAR,
    COUNT,
    COUNT_TO_MARK,
    CLEAR_TO_MARK,

    IF,
    IFELSE,
    EXEC,
    FOR,
    REPEAT,
    LOOP,
    FORALL,
    EXIT,
    STOP,

    TYPE,
    X_CHECK,
    R_CHECK,
    W_CHECK,
    
    DICT,
    SYSTEM_DICT,
    GLOBAL_DICT,
      USER_DICT,
    BEGIN,
    END,
    DEF,
    STORE,
    LOAD,
    WHERE,
    KNOWN,

    NULL,
  }
  
  struct Tokenizer {
    static CustomStringDict<Keyword> KeywordMap;

    static Tokenizer() {
      KeywordMap = new CustomStringDict<Keyword>(16) {
        { "true" , Keyword.LITERAL_TRUE  },
        { "false", Keyword.LITERAL_FALSE },
      };
      foreach (Keyword keywordConst in Enum.GetValues(typeof(Keyword))) {
        var constStr = Enum.GetName(typeof(Keyword), keywordConst);
        if(!KeywordMap.ContainsValue(keywordConst)) {
          KeywordMap.Add(constStr.Replace("_", string.Empty).ToLower(), keywordConst);
        }
      }
    }


    char[] _data;
    int    _cursor;

    public Tokenizer(char[] data) {
      _data   = data;
      _cursor = 0;
    }

    public Token GetNextToken() {
      var token = new Token();
      
      do {
        if(_cursor == _data.Length) {
          token.Type = TokenType.EOF;
          return token;
        }

        if(char.IsWhiteSpace(_data[_cursor])) {
          _cursor++;
        } else {
          break;
        }
      } while(true);
      

      token.Start = _cursor;
      
      switch (_data[_cursor]) {
        case '%': {
          var line = SegmentUntilEOL(_cursor + 1);
          if(line.Count > 0 && line[0] == '!')
            token.Type  = TokenType.PDF_TAG;
          else
            token.Type  = TokenType.COMMENT;
          token.Content = line;
          token.Length  = line.Count + 1;
        
          _cursor += line.Count + 2;
        } break;
        case '+': case '-': case '.':
        case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9': {
          var number = SegmentUntilDelimiter(_cursor);
          if(FirstIndexOf(number, '.') > -1)
            token.Type  = TokenType.LITERAL_DECIMAL;
          else
            token.Type  = TokenType.LITERAL_INTEGER;
          token.Content = number;
          token.Length  = number.Count;

          _cursor += token.Length;
        } break;
        case '{': case '}': {
          token.Type    = TokenType.BRACE;
          token.Content = new ArraySegment<char>(_data, _cursor, 1);
          token.Length  = 1;

          _cursor++;
        } break;
        case '[': case ']': {
          token.Type    = TokenType.BRACKETS;
          token.Content = new ArraySegment<char>(_data, _cursor, 1);
          token.Length  = 1;

          _cursor++;
        } break;
        case '(': {
          token.Type    = TokenType.LITERAL_STRING;
          token.Content = SegmentUntil(_cursor + 1, ')');
          token.Length  = token.Content.Count + 2;

          _cursor += token.Length;
        } break;
        case '<': {
          token.Type    = TokenType.LITERAL_STRING_HEX;
          token.Content = SegmentUntil(_cursor + 1, '>');
          token.Length  = token.Content.Count + 2;

          _cursor += token.Length;
        } break;
        case '/': {
          token.Type    = TokenType.NAME_DEF;
          token.Content = SegmentUntilDelimiter(_cursor + 1);
          token.Length  = token.Content.Count + 1;

          _cursor += token.Length;
        } break;
        default: {
          var content = SegmentUntilDelimiter(_cursor);
          
          var contentSpan = (ReadOnlySpan<char>)content;
          if(contentSpan == "true") {
            token.Type    = TokenType.LITERAL_BOOLEAN;
            token.Keyword = Keyword.LITERAL_TRUE;
          } else if(contentSpan == "false") {
            token.Type    = TokenType.LITERAL_BOOLEAN;
            token.Keyword = Keyword.LITERAL_TRUE;
          } else if(KeywordMap.TryGetValue(contentSpan, out var keyword)) {
            token.Type    = TokenType.KEYWORD;
            token.Keyword = keyword;
          }

          token.Content = content;
          token.Length  = content.Count;

          _cursor += token.Length;
        } break;
      }

      return token;
    }

    ArraySegment<char> SegmentUntilDelimiter(int startIndex) {
      for (int e = startIndex; e < _data.Length; e++) {
        if(IsDelimiter(_data[e]))
          return new ArraySegment<char>(_data, startIndex, e - startIndex);
      }

      var errSeg = new ArraySegment<char>(_data, startIndex, _data.Length - startIndex);
      throw new Exception($"No whitespace in \"{(errSeg.Count > 64 ? Dump(errSeg[..64])+"..." : Dump(errSeg))}\".");
    }
    ArraySegment<char> SegmentUntilWhitespace(int startIndex) {
      for (int e = startIndex; e < _data.Length; e++) {
        if(char.IsWhiteSpace(_data[e]))
          return new ArraySegment<char>(_data, startIndex, e - startIndex);
      }

      var errSeg = new ArraySegment<char>(_data, startIndex, _data.Length - startIndex);
      throw new Exception($"No whitespace in \"{(errSeg.Count > 64 ? Dump(errSeg[..64])+"..." : Dump(errSeg))}\".");
    }
    ArraySegment<char> SegmentUntilEOL(int startIndex) {
      for (int e = startIndex; e < _data.Length; e++) {
        if(IsLineEnd(_data[e]))
          return new ArraySegment<char>(_data, startIndex, e - startIndex);
      }

      var errSeg = new ArraySegment<char>(_data, startIndex, _data.Length - startIndex);
      throw new Exception($"No whitespace in \"{(errSeg.Count > 64 ? Dump(errSeg[..64])+"..." : Dump(errSeg))}\".");
    }
    ArraySegment<char> SegmentUntil(int startIndex, char end) {
      for (int e = startIndex; e < _data.Length; e++) {
        if(_data[e] == end)
          return new ArraySegment<char>(_data, startIndex, e - startIndex);
      }

      var errSeg = new ArraySegment<char>(_data, startIndex, _data.Length - startIndex);
      throw new Exception($"Could not find end '{EscapeChar(end)}' in \"{(errSeg.Count > 64 ? Dump(errSeg[..64])+"..." : Dump(errSeg))}\".");
    }
  }


  struct Token {
    public TokenType          Type;
    public Keyword            Keyword;
    public int                Start;
    public int                Length;
    public ArraySegment<char> Content;

    public override string ToString() {
      return $"[{Start:D4}..{Length:D3}] Type: {Type}, Keyword: {Keyword}, Content: {Dump(Content)}";
    }
  }
  
  static class Ext {
  static StringBuilder _dumpSb = new StringBuilder(128);
  public static string Dump(ArraySegment<char> data) {
    _dumpSb.Clear();

    for (int i = 0; i < data.Count; i++) {
      char c = data[i];
      switch (c) {
        case '\n': _dumpSb.Append(@"\n"); break;
        case '\r': _dumpSb.Append(@"\r"); break;
        case '\\': _dumpSb.Append(@"\\"); break;
        case '\t': _dumpSb.Append(@"\t"); break;
        default:   _dumpSb.Append(c);     break;
      }
    }
    return _dumpSb.ToString();
  }
  public static string EscapeChar(char c) {
    switch (c) {
      case '\n': return @"\n";
      case '\r': return @"\r";
      case '\t': return @"\t";
      case '\\': return @"\\";
      default:   return c.ToString();
    }
  }

  public static bool IsDelimiter(char c) {
      return char.IsWhiteSpace(c) || c == '{' || c == '}' || c == '[' || c == ']';
  }
  public static bool IsLineEnd(char c) {
    return c == '\n' || c == '\r';
  }

  public static int FirstIndexOf(ArraySegment<char> data, char search) {
    for (int i = 0; i < data.Count; i++) {
      if(data[i] == search)
        return i;
    }

    return -1;
  }

  static int LastIndexOf(ArraySegment<char> data, char search) {
    int i = data.Count - 1;
    for (; i >= 0; i--) {
      if(data[i] == search)
        break;
    }

    return i;
  }
  }
}
