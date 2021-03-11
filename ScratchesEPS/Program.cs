using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ScratchesEPS {
  class Program {
    static void Main(string[] args) {
      Thread.CurrentThread.CurrentCulture = new CultureInfo("en-Us");

      var sw = new Stopwatch();

      sw.Start();
      string file;
      using(var s = File.OpenRead("./input.eps"))
      using(var sr = new StreamReader(s)) {
        file = sr.ReadToEnd();
        sw.Stop();
      }
      Ext.WriteLinePurple($"reading the file took {sw.ElapsedMilliseconds}ms/{sw.ElapsedTicks}t");

      var tokenizer = new Tokenizer(file.ToCharArray());

      var _ = new StackToken();

      sw.Restart();
      Token t;
      while((t = tokenizer.GetNextToken()).Type != TokenType.EOF) {
        Ext.WritePurple($"[{sw.ElapsedMilliseconds:D4}ms] ");

        if(t.Type == TokenType.UNKNOWN) {
          Ext.WriteLineRed(t.ToString());
        } else {
          Console.WriteLine(t.ToString());
        }

        Machine.ProcessToken(in t);
      }

      sw.Restart();
      SVGBuilder.BuildSVG();
      sw.Stop();
      Ext.WriteLinePurple($"building the svg took {sw.ElapsedMilliseconds}ms/{sw.ElapsedTicks}t");
    }
  }
}
