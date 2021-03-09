using System;
using System.Collections.Generic;
using System.Text;

namespace ScratchesEPS {
  class RefStack<T> {
    public T[] Data;
    public int UsedSlots;

    public RefStack(int initialCapacity) {
      Data      = new T[initialCapacity];
      UsedSlots = 0;
    }

    public void Push(in T value) {
      if(UsedSlots == Data.Length)
        Array.Resize(ref Data, Data.Length * 2);

      Data[UsedSlots++] = value;
    }
    
    public T Pop() {
      return Data[--UsedSlots];
    }

    public ref T Peek() {
      return ref Data[UsedSlots - 1];
    }

    public void Clear() {
      UsedSlots = 0;
    }

    public T[] ToArray() {
      var arr = new T[UsedSlots];
      Array.Copy(Data, 0, arr, 0, UsedSlots);
      return arr;
    }
  }
}

