using System;

namespace ScratchesEPS {
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
    SET_FLAT,
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
    EO_FILL,
    STROKE,

    SET_GRAY,
    SET_RGB_COLOR,
    SET_HSB_COLOR,
    SET_CMYK_COLOR,
    SET_COLOR,
    SET_PATTERN,

    CURRENT_GRAY,

    CLIP,
    EO_CLIP,
    CLIP_PATH,
    CLIP_SAVE,
    CLIP_RESTORE,

    G_STATE,
    CURRENT_G_STATE,
    SET_G_STATE,
    G_SAVE,
    G_RESTORE,

    CURRENT_FLAT,
    CURRENT_TRANSFER,

    STRING,
    ANCHOR_SEARCH,

    ARRAY,
    LENGTH,
    A_STORE,
    A_LOAD,
    GET_INTERVAL,
    PUT_INTERVAL,

    SET_TRANSFER,

            MATRIX,
        SET_MATRIX,
     INVERT_MATRIX,
    CURRENT_MATRIX,
    DEFAULT_MATRIX,

    D_TRANSFORM,
    I_TRANSFORM,
    TRANSFORM,

    TRANSLATE,
    ROTATE,
    SCALE,
    CONCAT,

    PUSH,
    POP,
    PUT,
    GET,
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
    STOPPED,

    TYPE,
    X_CHECK,
    R_CHECK,
    W_CHECK,
    
    DICT,
    SYSTEM_DICT,
    GLOBAL_DICT,
      USER_DICT,
    STATUS_DICT,
    BEGIN,
    END,
    DEF,
    STORE,
    LOAD,
    WHERE,
    KNOWN,

    SAVE,
    RESTORE,

    FIND_FONT,

    NULL,
    VERSION,
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
    LITERAL_NAME,
    KEYWORD,
    BRACE,
    BRACKET,
    EOF
  }

  struct Token {
    public TokenType Type;
    public Keyword Keyword;
    public int Start, Length;
    public int Line , Column;
    public ArraySegment<char> Content;

    public override string ToString() {
      return $"[{Line:D4}:{Column:D3}] Type: {Type}, Keyword: {Keyword}, Content: {Ext.Dump(Content)}";
    }
  }
  
  struct Tokenizer {
    public static CustomStringDict<Keyword> KeywordMap;

    static Tokenizer() {
      KeywordMap = new CustomStringDict<Keyword>(16) {
        {"true", Keyword.LITERAL_TRUE},
        {"false", Keyword.LITERAL_FALSE},
      };
      foreach (Keyword keywordConst in Enum.GetValues(typeof(Keyword))) {
        var constStr = Enum.GetName(typeof(Keyword), keywordConst);
        if (!KeywordMap.ContainsValue(keywordConst)) {
          KeywordMap.Add(constStr.Replace("_", string.Empty).ToLower(), keywordConst);
        }
      }
    }


    char[] _data;
    int _cursor;
    int _lineCursor, _colCursor;

    public Tokenizer(char[] data) {
      _data = data;
      _cursor = 0;
      _lineCursor = 1;
      _colCursor  = 1;
    }

    public Token GetNextToken() {
      var token = new Token();

      do {
        if(_cursor == _data.Length) {
          token.Type = TokenType.EOF;
          return token;
        }

        if(_data[_cursor] == '\r') {
          _cursor++;
          _lineCursor++;
          _colCursor = 1;
          if(_cursor < _data.Length && _data[_cursor] == '\n') {
            _cursor++;
          }
        } else if (_data[_cursor] == '\n') {
          _cursor++;
          _lineCursor++;
          _colCursor = 1;
        } else if(char.IsWhiteSpace(_data[_cursor])) {
          _cursor++;
          _colCursor++;
        } else {
          break;
        }
      } while (true);


      token.Start = _cursor;
      token.Line   = _lineCursor;
      token.Column =  _colCursor;

      switch(_data[_cursor]) {
        case '%': {
          var line = SegmentUntilEOL(_cursor + 1);
          if(line.Count > 0 && line[0] == '!')
            token.Type = TokenType.PDF_TAG;
          else
            token.Type = TokenType.COMMENT;
          token.Content = line;
          token.Length = line.Count + 1;

          _cursor += line.Count + 1;
        } break;
        case '+':
        case '-':
        case '.':
        case '0':
        case '1':
        case '2':
        case '3':
        case '4':
        case '5':
        case '6':
        case '7':
        case '8':
        case '9': {
          var number = SegmentUntilDelimiter(_cursor);
          if(Ext.FirstIndexOf(number, '.') > -1)
            token.Type = TokenType.LITERAL_DECIMAL;
          else
            token.Type = TokenType.LITERAL_INTEGER;
          token.Content = number;
          token.Length = number.Count;

          _cursor    += token.Length;
          _colCursor += token.Length;
        }
          break;
        case '{':
        case '}': {
          token.Type = TokenType.BRACE;
          token.Content = new ArraySegment<char>(_data, _cursor, 1);
          token.Length = 1;

          _cursor++;
          _colCursor++;
        } break;
        case '[':
        case ']': {
          token.Type = TokenType.BRACKET;
          token.Content = new ArraySegment<char>(_data, _cursor, 1);
          token.Length = 1;

          _cursor++;
          _colCursor++;
        } break;
        case '(': {
          token.Type = TokenType.LITERAL_STRING;
          token.Content = SegmentUntil(_cursor + 1, ')');
          token.Length = token.Content.Count + 2;

          _cursor    += token.Length;
          _colCursor += token.Length;
        } break;
        case '<': {
          token.Type = TokenType.LITERAL_STRING_HEX;
          token.Content = SegmentUntil(_cursor + 1, '>');
          token.Length = token.Content.Count + 2;

          _cursor    += token.Length;
          _colCursor += token.Length;
        } break;
        case '/': {
          token.Type = TokenType.LITERAL_NAME;
          token.Content = SegmentUntilDelimiter(_cursor + 1);
          token.Length = token.Content.Count + 1;

          _cursor    += token.Length;
          _colCursor += token.Length;
        } break;
        default: {
          token.Content = SegmentUntilDelimiter(_cursor);
          
          var contentSpan = (ReadOnlySpan<char>)token.Content;
          if(contentSpan.Equals("true", StringComparison.Ordinal)) {
            token.Type    = TokenType.LITERAL_BOOLEAN;
            token.Keyword = Keyword.LITERAL_TRUE;
          } else if(contentSpan.Equals("false", StringComparison.Ordinal)) {
            token.Type    = TokenType.LITERAL_BOOLEAN;
            token.Keyword = Keyword.LITERAL_TRUE;
          } else if(KeywordMap.TryGetValue(contentSpan, out var keyword)) {
            token.Type    = TokenType.KEYWORD;
            token.Keyword = keyword;
          }

          token.Length = token.Content.Count;

          _cursor    += token.Length;
          _colCursor += token.Length;
        } break;
      }

      return token;
    }

    ArraySegment<char> SegmentUntilDelimiter(int startIndex) {
      for (int e = startIndex; e < _data.Length; e++) {
        if (Ext.IsDelimiter(_data[e]))
          return new ArraySegment<char>(_data, startIndex, e - startIndex);
      }

      var errSeg = new ArraySegment<char>(_data, startIndex, _data.Length - startIndex);
      throw new Exception($"No whitespace in \"{(errSeg.Count > 64 ? Ext.Dump(errSeg[..64]) + "..." : Ext.Dump(errSeg))}\".");
    }
    ArraySegment<char> SegmentUntilWhitespace(int startIndex) {
      for (int e = startIndex; e < _data.Length; e++) {
        if (char.IsWhiteSpace(_data[e]))
          return new ArraySegment<char>(_data, startIndex, e - startIndex);
      }

      var errSeg = new ArraySegment<char>(_data, startIndex, _data.Length - startIndex);
      throw new Exception($"No whitespace in \"{(errSeg.Count > 64 ? Ext.Dump(errSeg[..64]) + "..." : Ext.Dump(errSeg))}\".");
    }
    ArraySegment<char> SegmentUntilEOL(int startIndex) {
      for (int e = startIndex; e < _data.Length; e++) {
        if (Ext.IsLineEnd(_data[e]))
          return new ArraySegment<char>(_data, startIndex, e - startIndex);
      }

      var errSeg = new ArraySegment<char>(_data, startIndex, _data.Length - startIndex);
      throw new Exception($"No whitespace in \"{(errSeg.Count > 64 ? Ext.Dump(errSeg[..64]) + "..." : Ext.Dump(errSeg))}\".");
    }
    ArraySegment<char> SegmentUntil(int startIndex, char end) {
      for (int e = startIndex; e < _data.Length; e++) {
        if (_data[e] == end)
          return new ArraySegment<char>(_data, startIndex, e - startIndex);
      }

      var errSeg = new ArraySegment<char>(_data, startIndex, _data.Length - startIndex);
      throw new Exception($"Could not find end '{Ext.EscapeChar(end)}' in \"{(errSeg.Count > 64 ? Ext.Dump(errSeg[..64]) + "..." : Ext.Dump(errSeg))}\".");
    }
  }
}