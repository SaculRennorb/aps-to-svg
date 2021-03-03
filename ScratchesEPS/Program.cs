using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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

        Machine.ProcessToken(in t);
      }
    }
  }
}
