using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Utils.BR
{
    [PublicAPI] public class ObservableDictionary<TKey, TValue>: IObservable, IDictionary<TKey, TValue>, IDictionary
    {
        public class DictionaryEnumerator : IDictionaryEnumerator
        {
            private readonly DictionaryEntry[] items;
            private int index = -1;

            public DictionaryEnumerator(ObservableDictionary<TKey, TValue> dictionary)
            {
                items = dictionary.Select(d => new DictionaryEntry(d.Key, d.Value)).ToArray();
            }

            public object Current => Entry;
            public DictionaryEntry Entry => items.Also(ValidateIndex).Let(i => i[index]);
            public object Key => items.Also(ValidateIndex).Let(i => i[index].Key);
            public object Value => items.Also(ValidateIndex).Let(i => i[index].Value);

            public bool MoveNext()
            {
                if (index >= items.Length - 1) return false;
                index++;
                return true;
            }

            public void Reset() => index = -1;
            
            private void ValidateIndex()
            {
                if (index < 0 || index >= items.Length)
                    throw new InvalidOperationException("Enumerator is before or after the collection.");
            }
        }
        
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => values.Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => values.Select(v => v.Value.value).ToArray();
        ICollection IDictionary.Keys => values.Keys;
        ICollection IDictionary.Values => values.Select(v => v.Value.value).ToArray();
        
        public int Count => values.Count;
        public bool IsReadOnly => false;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => values;
        bool IDictionary.IsFixedSize => false;

        public event IObservable.OnUpdatedHandler OnUpdated;

        public ObservableDictionary() { }

        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            foreach (var item in dictionary)
                Add(item.Key, item.Value);
        }

        public TValue this[TKey key]
        {
            get => values[key].value;
            set
            {
                UnregisterValue(values[key].observable);
                values[key] = CreateEntry(value);
                BroadcastUpdate();
            }
        }

        object IDictionary.this[object key]
        {
            get => this[(TKey)key];
            set => this[(TKey)key] = (TValue)value;
        }

        private readonly Dictionary<TKey, (TValue value, IObservable observable)> values = new ();

        public void Dispose()
        {
            Clear();
            BroadcastUpdate();
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (var item in values)
                yield return new(item.Key, item.Value.value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(this);

        public void Add(KeyValuePair<TKey, TValue> item) =>
            values.Add(item.Key, CreateEntry(item.Value));

        public void Clear()
        {
            foreach (var item in values)
                UnregisterValue(item.Value.observable);
            
            values.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (!values.TryGetValue(item.Key, out var pair))
                return false;

            return EqualityComparer<TValue>.Default.Equals(pair.value, item.Value);
        }
        bool IDictionary.Contains(object key) => ContainsKey((TKey) key);

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) =>
            values.Select(p => new KeyValuePair<TKey,TValue>(p.Key, p.Value.value)).ToArray().CopyTo(array, arrayIndex);
        void ICollection.CopyTo(Array array, int index) => CopyTo((KeyValuePair<TKey, TValue>[]) array, index);

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (!Contains(item))
                return false;

            if (!EqualityComparer<TValue>.Default.Equals(item.Value, values[item.Key].value))
                return false;
            
            return Remove(item.Key);
        }

        public void Add(TKey key, TValue value) => values.Add(key, CreateEntry(value));
        void IDictionary.Add(object key, object value) => Add((TKey) key, (TValue) value);

        public bool ContainsKey(TKey key) => values.ContainsKey(key);

        public bool Remove(TKey key)
        {
            UnregisterValue(values.GetValueOrDefault(key, default).observable);
            return values.Remove(key);
        }
        void IDictionary.Remove(object key) => Remove((TKey) key);

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (values.TryGetValue(key, out var entry))
            {
                value = entry.value;
                return true;
            }

            value = default;
            
            return false;
        }

        private (TValue value, IObservable mutable) CreateEntry(TValue item)
        {
            if (item is IObservable mutable)
                mutable.OnUpdated += BroadcastUpdate;
            else
                mutable = null;
            
            return (item, mutable);
        }

        private void UnregisterValue(IObservable mutable)
        {
            if (mutable != null)
                mutable.OnUpdated -= BroadcastUpdate;
        }

        private void BroadcastUpdate() => OnUpdated?.Invoke();
    }
}