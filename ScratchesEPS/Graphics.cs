using System;
using System.Collections.Generic;
using System.Drawing;
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

  enum ColorSpace {
    DEVICE_GRAY,
    DEVICE_RGB,
    DEVICE_CMYK,
  }

  struct GOperation {
    public Keyword Type;
    public float   Arg1;
    public float   Arg2;
    public float   Arg3;
    public float   Arg4;
    public float   Arg5;
    public float   Arg6;
    public int[]   DynData;

    public GOperation(Keyword type,
      float arg1 = 0, float arg2 = 0, float arg3 = 0, float arg4 = 0, float arg5 = 0, float arg6 = 0,
      int[] dynData = null) {
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
  
  struct DashPattern {
    public double[] Pattern;
    public double   Offset;
  }

  struct SubPath {
    public RefStack<GOperation> OPs;
  }

  struct PaintCall {
    public Keyword Type;
    public SubPath[] SubPaths;
  }

  struct Path {
    public RefStack<SubPath>   SubPaths;
    public RefStack<PaintCall> PaintCalls;
    public float CurrentX, CurrentY;

    public PaintCall PCallFromCurrentState(Keyword type) {
      return new PaintCall {
        Type = type,
        SubPaths = this.SubPaths.ToArray(),
      };
    }
  }

  struct GraphicsState {
    public Matrix3x2   CTM;
    public Point       Position;
    //private Path     path;
    //private Path     clippingPath;
    //private Path     clippingPathStack;
    public object[]    ColorSpace;
    public object      Color;
    public object      Font;
    public double      LineWidth;
    public LineCap     LineCap;
    public LineJoin    LineJoin;
    public double      MiterLimit;
    public DashPattern DashPattern;
    public bool        StrokeAdjustment;
    public float       Flatness;
    

    public List<GOperation> Operations;
    public RefStack<Path>   Paths;
  }

  static class Graphics {
    public static RefStack<GraphicsState> StateStack = new RefStack<GraphicsState>(8);

    public static Matrix3x2 DefaultOutputMtx = Matrix3x2.Identity;

    public static string BoundingBox;

    static Graphics() {
      StateStack.Push(new GraphicsState() {
        Operations = new List<GOperation>(),
        Paths      = new RefStack<Path>(16),
        CTM        = Matrix3x2.Identity,  
      });
    }

    public static void SetDash(int offset, int[] pattern) {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.SET_DASH, arg1: offset, dynData: pattern));
      Ext.WriteLineStub();
    }
    public static void SetLineCap(int type) {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.SET_LINE_CAP, arg1: type));
      gState.LineCap = (LineCap)type;
    }
    public static void SetLineJoin(int type) {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.SET_LINE_JOIN, arg1: type));
      gState.LineJoin = (LineJoin)type;
    }
    public static void SetLineWidth(int width) {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.SET_LINE_WIDTH, arg1: width));
      gState.LineWidth = width;
    }
    public static void SetMiterLimit(float limit) {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.SET_MITER_LIMIT, arg1: limit));
      gState.MiterLimit = limit;
    }
    public static void SetFlat(float flatness) {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.SET_FLAT, arg1: flatness));
      gState.Flatness = flatness;
    }

    public static void NewPath() {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.NEW_PATH));

      gState.Paths.Push( new Path {
        SubPaths   = new RefStack<SubPath>(16),
        PaintCalls = new RefStack<PaintCall>(16),
      });

      Ext.WriteLineStub();
    }

    public static void MoveTo(float x, float y) {
      ref var gState = ref StateStack.Peek();
      var v = Vector2.Transform(new Vector2(x, y), gState.CTM);
      var gop = new GOperation(Keyword.MOVE_TO, arg1: v.X, arg2: v.Y);
      gState.Operations.Add(gop);

      ref var path = ref gState.Paths.Peek();
      var subPaths = path.SubPaths;
      void MakeNewPath() {
        var ops = new RefStack<GOperation>(16);
        ops.Push(in gop);
        subPaths.Push(new SubPath { OPs = ops });
      }

      
      if(subPaths.UsedSlots == 0) {
        MakeNewPath();
      } else {
        var subPath = subPaths.Peek();
        if(subPath.OPs.UsedSlots == 0) {
          subPath.OPs.Push(in gop);
        } else if(subPath.OPs.UsedSlots == 1) {
          if(subPath.OPs.Peek().Type == Keyword.MOVE_TO) {
            subPath.OPs.Data[0] = gop;
          } else {
            MakeNewPath();
          }
        } else {
          subPath.OPs.Push(in gop);
        }
      }

      path.CurrentX = gop.Arg1;
      path.CurrentY = gop.Arg2;

      Ext.WriteLineStub();
    }
    public static void LineTo(float x, float y) {
      ref var gState = ref StateStack.Peek();
      var v = Vector2.Transform(new Vector2(x, y), gState.CTM);
      var gop = new GOperation(Keyword.LINE_TO, arg1: v.X, arg2: v.Y);
      gState.Operations.Add(gop);

      gState.Paths.Peek().SubPaths.Peek().OPs.Push(in gop);

      Ext.WriteLineStub();
    }
    public static void CurveTo(float x1, float y1, float x2, float y2, float x3, float y3) {
      ref var gState = ref StateStack.Peek();
      var v1 = Vector2.Transform(new Vector2(x1, y1), gState.CTM);
      var v2 = Vector2.Transform(new Vector2(x2, y2), gState.CTM);
      var v3 = Vector2.Transform(new Vector2(x3, y3), gState.CTM);
      var gop = new GOperation(Keyword.CURVE_TO, v1.X, v1.Y, v2.X, v2.Y, v3.X, v3.Y);
      gState.Operations.Add(gop);

      ref var path = ref gState.Paths.Peek();
      path.SubPaths.Peek().OPs.Push(gop);

      path.CurrentX = gop.Arg1;
      path.CurrentY = gop.Arg2;

      Ext.WriteLineStub();
    }

    public static void ClosePath() {
      ref var gState = ref StateStack.Peek();
      var gop = new GOperation(Keyword.CLOSE_PATH);
      gState.Operations.Add(gop);
      
      var subPath = gState.Paths.Peek().SubPaths.Peek();
      if(subPath.OPs.Peek().Type != Keyword.CLOSE_PATH) {
        subPath.OPs.Push(in gop);
      }

      Ext.WriteLineStub();
    }

    public static void Fill() {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.FILL));

      ref var path = ref gState.Paths.Peek();
      ref var subPath = ref path.SubPaths.Peek();
      if(subPath.OPs.Peek().Type != Keyword.CLOSE_PATH) {
        subPath.OPs.Push(new GOperation(Keyword.CLOSE_PATH));
      }

      path.PaintCalls.Push(path.PCallFromCurrentState(Keyword.FILL));
      
      var ops = new RefStack<GOperation>(16);
      ops.Push(new GOperation(Keyword.MOVE_TO, path.CurrentX, path.CurrentY));
      path.SubPaths.Push(new SubPath { OPs = ops });
      
      
      gState.Operations.Add(new GOperation(Keyword.CLOSE_PATH));
      
      Ext.WriteLineStub();
    }
    public static void EOFill() {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.EO_FILL));
      Ext.WriteLineStub();
    }
    public static void Stroke() {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.STROKE));
      Ext.WriteLineStub();
    }

    public static void Clip() {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.CLIP));
      Ext.WriteLineStub();
    }
    public static void EOClip() {
      ref var gState = ref StateStack.Peek();
      gState.Operations.Add(new GOperation(Keyword.EO_CLIP));
      Ext.WriteLineStub();
    }

    public static void GSave() {
      var newState = StateStack.Peek();
      //newState.Operations = new List<GOperation>(newState.Operations);
      newState.Operations.Add(new GOperation(Keyword.G_SAVE));
      StateStack.Push(newState);
    }
    public static void GRestore() {
      StateStack.Pop();
      StateStack.Peek().Operations.Add(new GOperation(Keyword.G_RESTORE));
    }

    public static void SetRGBColor(float r, float g, float b) {
      ref var gState = ref StateStack.Peek();
      gState.ColorSpace = new object[] { ColorSpace.DEVICE_RGB };
      gState.Color      = new [] {r, g, b};
    }

    public static float GetCurrentGray() {
      ref var gState = ref StateStack.Peek();
      switch ((ColorSpace)gState.ColorSpace[0]) {
        case ColorSpace.DEVICE_GRAY: return (float)gState.Color;
        case ColorSpace.DEVICE_RGB:
          var rgb = (float[])gState.Color;
          var gray = (rgb[0] + rgb[1] + rgb[2]) / 3;
          return gray;
        case ColorSpace.DEVICE_CMYK:
          Ext.WriteLineRed("getcurrentgray cmyk stub");
          throw new Exception();
        default: 
          Ext.WriteLineRed("color space not set properly");
          throw new Exception();
      }
    }
    public static void SetGray(float g) {
      ref var gState = ref StateStack.Peek();
      gState.ColorSpace = new object[] { ColorSpace.DEVICE_GRAY };
      gState.Color      = g;
    }

  }
}
