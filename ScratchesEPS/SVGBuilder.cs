using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScratchesEPS {
  static class SVGBuilder {
    public static void BuildSVG(List<GOperation> operations) {
      Console.WriteLine("+---------------------------------------+\n" +
                        "|             Building SVG              |\n" +
                        "+---------------------------------------+");

      using var s = File.Open("out.svg", FileMode.Create);
      using var writer = new StreamWriter(s);

      string line = string.Empty;
      foreach (var op in operations) {
        switch (op.Type) {
          case Keyword.NEW_PATH  : line = "<path d=\""; break;
          case Keyword.MOVE_TO   : line = $"M {op.Arg1} {op.Arg2}"; break;
          case Keyword.LINE_TO   : line = $"L {op.Arg1} {op.Arg2}"; break;
          case Keyword.CLOSE_PATH: line = "z\" />"; break;
          default: Ext.WriteLineRed($"cant parse GOp {op.Type}"); continue;
        }
        
        Console.WriteLine(line); 
        writer.WriteLine(line);
      }
    }
  }
}
