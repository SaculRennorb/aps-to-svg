using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ScratchesEPS {
  enum LineCap {
    BUTT   = 0,
    ROUND  = 1,
    SQUARE = 2,
  }
  enum LineJoin {
    MITER  = 0,
    ROUND  = 1,
    BEVEL  = 2,
  }

  struct GOperation {
    public Keyword Type;
    public int     Arg1;
    public int     Arg2;
    public int     Arg3;
    public int     Arg4;
    public int     Arg5;
    public int     Arg6;
    public int[]   DynData;

    public GOperation(Keyword type, int arg1 = 0, int arg2 = 0, int arg3 = 0, int arg4 = 0, int arg5 = 0, int arg6 = 0, int[] dynData = null) {
      Type = type;
      Arg1 = arg1;
      Arg2 = arg2;
      Arg3 = arg3;
      Arg4 = arg4;
      Arg5 = arg5;
      Arg6 = arg6;
      DynData = dynData;
    }
  }

  struct Graphics {
    public static Matrix3x2 UserSpace = Matrix3x2.Identity;

    public static List<GOperation> Operations = new List<GOperation>();

    public static void SetDash(int offset, int[] pattern) {
      Operations.Add(new GOperation(Keyword.SET_DASH, arg1: offset, dynData: pattern));
    }
    public static void SetLineCap(int type) {
      Operations.Add(new GOperation(Keyword.SET_LINE_CAP, arg1: type));
    }
    public static void SetLineJoin(int type) {
      Operations.Add(new GOperation(Keyword.SET_LINE_JOIN, arg1: type));
    }
    public static void SetLineWidth(int width) {
      Operations.Add(new GOperation(Keyword.SET_LINE_WIDTH, arg1: width));
    }
    public static void SetMiterLimit(int limit) {
      Operations.Add(new GOperation(Keyword.SET_MITER_LIMIT, arg1: limit));
    }

    public static void NewPath() {
      Operations.Add(new GOperation(Keyword.NEW_PATH));
    }
    public static void ClosePath() {
      Operations.Add(new GOperation(Keyword.CLOSE_PATH));
    }

    public static void GSave() {
      Operations.Add(new GOperation(Keyword.G_SAVE));
    }
    public static void GRestore() {
      Operations.Add(new GOperation(Keyword.G_RESTORE));
    }
  }
}
