using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ScratchesEPS {
  class CustomStringDict<T> : IDictionary<string, T> {
    private Entry[] _slots;
    private int     _slotsUsed;

    public CustomStringDict(int initialSize) {
      _slots = new Entry[initialSize];
      for (int i = 0; i < _slots.Length; i++) {
        _slots[i] = new Entry(initialSize: 2);
      }
    }

    public IEnumerator<KeyValuePair<string, T>> GetEnumerator() {
      throw new NotImplementedException();
    }
    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }
    public void Add(KeyValuePair<string, T> item) {
      Add(item.Key, item.Value);
    }
    public void Clear() {
      throw new NotImplementedException();
    }
    public bool Contains(KeyValuePair<string, T> item) {
      throw new NotImplementedException();
    }
    public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex) {
      throw new NotImplementedException();
    }
    public bool Remove(KeyValuePair<string, T> item) {
      throw new NotImplementedException();
    }
    public int Count { get; }
    public bool IsReadOnly { get; }
    public void Add(string key, T value) {
      if(_slotsUsed > _slots.Length * 0.75) {
        Resize();
      }

      var index = Mod(key.GetHashCode(), _slots.Length);
      _slots[index].Add(key, value);

      _slotsUsed++;
    }

    int Mod(int a, int b) {
      return ((a % b) + b) % b;
    }

    private void Resize() {
      var oldSlots = _slots;
      _slots = new Entry[_slots.Length * 2];
      for (int i = 0; i < _slots.Length; i++) {
        _slots[i] = new Entry(initialSize: 2);
      }

      for (var i = 0; i < oldSlots.Length; i++) {
        for (int j = 0; j < oldSlots[i].SlotsUsed; j++) {
          Add(oldSlots[i].Slots[j]);
        }
      }
    }

    public bool ContainsKey(string key) {
      throw new NotImplementedException();
    }
    public bool ContainsValue(T value) {
      foreach (var bucket in _slots) {
        for (int i = 0; i < bucket.SlotsUsed; i++) {
          if(bucket.Slots[i].Value.Equals(value))
            return true;
        }
      }

      return false;
    }
    public bool Remove(string key) {
      throw new NotImplementedException();
    }
    public bool TryGetValue(string key, out T value) {
      return TryGetValue((ReadOnlySpan<char>)key, out value);
    }
    public bool TryGetValue(ReadOnlySpan<char> key, out T value) {
      var index = Mod(string.GetHashCode(key), _slots.Length);
      return _slots[index].Find(key, out value);
    }

    public T this[string key] {
      get => throw new NotImplementedException();
      set => throw new NotImplementedException();
    }

    public ICollection<string> Keys { get; }
    public ICollection<T> Values { get; }

    struct Entry {
      public KeyValuePair<string, T>[] Slots;
      public int                       SlotsUsed;

      public Entry(int initialSize = 2) {
        Slots     = new KeyValuePair<string, T>[initialSize];
        SlotsUsed = 0;
      }

      public void Add(string key, T value) {
        if(SlotsUsed == Slots.Length) {
          Resize();
        }

        Slots[SlotsUsed] = new KeyValuePair<string, T>(key, value);
        SlotsUsed++;
      }
      
      public bool Find(ReadOnlySpan<char> key, out T val) {
        for (int i = 0; i < SlotsUsed; i++) {
          ref var e = ref Slots[i];
          if(MemoryExtensions.Equals(e.Key, key, StringComparison.Ordinal)) {
            val = e.Value;
            return true;
          }
        }

        val = default;
        return false;
      }

      private void Resize() {
        Array.Resize(ref Slots, Slots.Length * 2);
      }

      public override string ToString() {
        return $"Bucket [{SlotsUsed:D2} / {Slots.Length:D2}]";
      }
    }
  }
}
