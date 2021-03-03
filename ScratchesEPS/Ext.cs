using System;
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

    public static void WriteRed(string msg) {
      Console.Write("\u001b[31m");
      Console.Write(msg);
      Console.WriteLine("\u001b[0m");
    }
    public static void WriteYellow(string msg) {
      Console.Write("\u001b[33m");
      Console.Write(msg);
      Console.WriteLine("\u001b[0m");
    }
    public static void WriteGreen(string msg) {
      Console.Write("\u001b[32m");
      Console.Write(msg);
      Console.WriteLine("\u001b[0m");
    }
  }
}