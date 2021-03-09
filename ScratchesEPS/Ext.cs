using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace ScratchesEPS {
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
    
    [DebuggerStepThrough]
    public static bool IsDelimiter(char c) {
      return char.IsWhiteSpace(c) || c == '{' || c == '}' || c == '[' || c == ']' || c == '/';
    }
    [DebuggerStepThrough]
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
    
    [DebuggerStepThrough]
    public static int Mod(int a, int b) {
      return ((a % b) + b) % b;
    }
    
    [DebuggerStepThrough]
    public static float ToFloat(object o) {
      if(o is float fVal) return fVal;
      if(o is int   iVal) return iVal;
      
      throw new Exception("not convertible to float");
    }
    
    [DebuggerStepThrough]
    public static int ParseInt(ReadOnlySpan<char> span) {
      var radixIndex = span.IndexOf('#');
      if(radixIndex > -1) {
        var radix = int.Parse(span.Slice(0, radixIndex));
        
        return Convert.ToInt32(new string(span.Slice(radixIndex + 1)), radix);
      } else {
        return int.Parse(span);
      }
    }
    
    [DebuggerStepThrough]
    public static float ParseFloat(ReadOnlySpan<char> span) {
      if(span[0] == '.') {
        Span<char> newBuff = stackalloc char[span.Length + 1];
        newBuff[0] = '0';
        span.CopyTo(newBuff[1..]);
        return float.Parse(newBuff, provider: CultureInfo.InvariantCulture);
      } else {
        return float.Parse(span, provider: CultureInfo.InvariantCulture);
      }
    }

    [DebuggerStepThrough]
    public static void WriteLineStub([CallerMemberName] string name = null) {
      Console.Write("\u001b[31m");
      Console.Write(name);
      Console.Write(" stub");
      Console.WriteLine("\u001b[0m");
    }
    [DebuggerStepThrough]
    public static void WriteLineRed(string msg) {
      Console.Write("\u001b[31m");
      Console.Write(msg);
      Console.WriteLine("\u001b[0m");
    }
    [DebuggerStepThrough]
    public static void WriteLineYellow(string msg) {
      Console.Write("\u001b[33m");
      Console.Write(msg);
      Console.WriteLine("\u001b[0m");
    }
    [DebuggerStepThrough]
    public static void WriteLineGreen(string msg) {
      Console.Write("\u001b[32m");
      Console.Write(msg);
      Console.WriteLine("\u001b[0m");
    }
    [DebuggerStepThrough]
    public static void WriteLinePurple(string msg) {
      Console.Write("\u001b[35m");
      Console.Write(msg);
      Console.WriteLine("\u001b[0m");
    }
    
    [DebuggerStepThrough]
    public static void WritePurple(string msg) {
      Console.Write("\u001b[35m");
      Console.Write(msg);
      Console.Write("\u001b[0m");
    }
    [DebuggerStepThrough]
    public static void WriteGreen(string msg) {
      Console.Write("\u001b[32m");
      Console.Write(msg);
      Console.Write("\u001b[0m");
    }
  }
}