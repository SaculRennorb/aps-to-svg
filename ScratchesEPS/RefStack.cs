using System;
using System.Collections;
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

    public RefStack<T>.Enumerator GetEnumerator() {
      return new Enumerator(this);
    }

    public struct Enumerator {
      private readonly RefStack<T> _source;
      private int                  _currentInd;

      public Enumerator(RefStack<T> source) {
        _source     = source;
        _currentInd = -1;
      }

      public bool MoveNext() {
        return ++_currentInd < _source.UsedSlots;
      }
      public void Reset() {
        _currentInd = 0;
      }

      public ref T Current {
        get {
          return ref _source.Data[_currentInd];
        }
      }
      
      public void Dispose() { }
    }
  }
}

