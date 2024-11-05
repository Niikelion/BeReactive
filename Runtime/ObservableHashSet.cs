using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;

namespace Utils.BR
{
    [PublicAPI] public class ObservableHashSet<T> : IObservable<ISet<T>>, IReadOnlyCollection<T>, ISet<T>, IDeserializationCallback, ISerializable
    {
        public int Count => values.Count;
        public bool IsReadOnly => false;
        
        public event IObservable.OnUpdatedHandler OnUpdated;
        public event IObservable<ISet<T>>.OnChangedHandler OnChanged;
        
        private readonly HashSet<T> values = new ();
        private readonly Dictionary<T, IObservable> observables = new ();

        public bool Add(T item)
        {
            if (!values.Add(item)) return false;

            RegisterValue(item);
            BroadcastUpdate();
            return true;
        }
        void ICollection<T>.Add(T item) => Add(item!);
        public bool Remove(T item)
        {
            if (!values.Remove(item)) return false;
            
            UnregisterValue(item);
            BroadcastUpdate();
            return true;
        }

        public int RemoveWhere(Predicate<T> match)
        {
            var toRemove = values.Where(v => match(v)).ToArray();

            foreach (var element in toRemove)
            {
                values.Remove(element);
                UnregisterValue(element);
            }
            
            if (toRemove.Length > 0) BroadcastUpdate();
            
            return toRemove.Length;
        }
        public void Clear()
        {
            bool wasNotEmpty = values.Count > 0;
            foreach (var item in observables) UnregisterValue(item.Key);
            values.Clear();
            observables.Clear();
            if (wasNotEmpty) BroadcastUpdate();
        }
        public bool Contains(T item) => values.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => values.CopyTo(array, arrayIndex);
        public void ExceptWith(IEnumerable<T> other)
        {
            var toRemove = other as T[] ?? other.ToArray();
            var otherSet = toRemove.ToHashSet();
            
            otherSet.IntersectWith(this);

            if (otherSet.Count == 0) return;
            values.ExceptWith(toRemove);

            foreach (var item in otherSet) UnregisterValue(item);
            BroadcastUpdate();
        }
        public void IntersectWith(IEnumerable<T> other)
        {
            var toIntersect = other as T[] ?? other.ToArray();
            var otherSet = toIntersect.ToHashSet();
            
            var toRemove = values.ToHashSet();
            toRemove.ExceptWith(otherSet);

            values.IntersectWith(otherSet);
            if (toRemove.Count == 0) return;

            foreach (var item in toRemove) UnregisterValue(item);
            BroadcastUpdate();
        }
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            var otherArr = other as T[] ?? other.ToArray();
            var otherSet = otherArr.ToHashSet();
            
            var intersectionSet = otherSet.ToHashSet();
            intersectionSet.IntersectWith(values);
            otherSet.ExceptWith(intersectionSet);
            
            if (otherSet.Count == 0 && intersectionSet.Count == 0) return;
            
            values.ExceptWith(intersectionSet);
            values.UnionWith(otherSet);
            
            foreach (var item in otherSet) RegisterValue(item);
            foreach (var item in intersectionSet) UnregisterValue(item);
            
            BroadcastUpdate();
        }
        public void UnionWith(IEnumerable<T> other)
        {
            var otherArr = other as T[] ?? other.ToArray();
            var otherSet = otherArr.ToHashSet();
            otherSet.ExceptWith(values);
            
            if (otherSet.Count == 0) return;
            values.UnionWith(otherSet);
            
            foreach (var item in otherSet) RegisterValue(item);
            
            BroadcastUpdate();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) => values.IsProperSubsetOf(other);
        public bool IsProperSupersetOf(IEnumerable<T> other) => values.IsProperSupersetOf(other);
        public bool IsSubsetOf(IEnumerable<T> other) => values.IsSubsetOf(other);
        public bool IsSupersetOf(IEnumerable<T> other) => values.IsSupersetOf(other);
        public bool Overlaps(IEnumerable<T> other) => values.Overlaps(other);
        public bool SetEquals(IEnumerable<T> other) => values.SetEquals(other);
        
        public IEnumerator<T> GetEnumerator() => values.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => values.GetEnumerator();

        public void OnDeserialization(object sender) { }
        public void GetObjectData(SerializationInfo info, StreamingContext context) { }

        private void RegisterValue(T item)
        {
            if (item is not IObservable observable) return;
            
            observables.Add(item, observable);
            observable.OnUpdated += BroadcastUpdate;
        }

        private void UnregisterValue(T item)
        {
            if (!observables.Remove(item, out var observable)) return;

            observable.OnUpdated -= BroadcastUpdate;
        }
        
        private void BroadcastUpdate()
        {
            OnUpdated?.Invoke();
            OnChanged?.Invoke(this);
        }
    }
}