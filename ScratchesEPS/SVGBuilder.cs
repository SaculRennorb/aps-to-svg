using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ScratchesEPS {
  static class SVGBuilder {
    public static void BuildSVG() {
      Console.WriteLine("+---------------------------------------+\n" +
                        "|             Building SVG              |\n" +
                        "+---------------------------------------+");

      using var s = File.Open("out.svg", FileMode.Create);
      using var writer = new StreamWriter(s);

      void Write(Keyword type, string line) {
        Console.WriteLine($"{type}: {line}"); 
        writer.WriteLine(line);
      }

      writer.WriteLine($"<svg viewbox=\"{Graphics.BoundingBox}\">");
      
      foreach(ref var path in Graphics.StateStack.Peek().Paths) {
        foreach(ref var call in path.PaintCalls) {
          switch (call.Type) {
            case Keyword.FILL   : Write(0, "<path d=\"");                       break;
            case Keyword.EO_FILL: Write(0, "<path fill-rule=\"evenodd\" d=\""); break;
            default: Ext.WriteLineRed($"cant paint call type {call.Type}"); continue;
          }
          
          foreach (var sPath in call.SubPaths) {
            string line;
            foreach (ref var gop in sPath.OPs) {
              switch (gop.Type) {
                case Keyword.MOVE_TO   : line = $"M {gop.Arg1} {gop.Arg2}"; break;
                case Keyword.LINE_TO   : line = $"L {gop.Arg1} {gop.Arg2}"; break;
                case Keyword.CURVE_TO  : line = $"C {gop.Arg1} {gop.Arg2} {gop.Arg3} {gop.Arg4} {gop.Arg5} {gop.Arg6} "; break;
                case Keyword.CLOSE_PATH: line = "z"; break;
                default: Ext.WriteLineRed($"cant translate Gop {gop.Type}"); continue;
              }
         
              Write(gop.Type, line);
            }
          }

          Write(call.Type, "\" />");
        } 
      }

      writer.WriteLine("</svg>");
    }
  }
}
