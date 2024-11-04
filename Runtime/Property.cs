using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Utils.BR
{
    [PublicAPI]
    public interface IProperty<T> : IObservable<T>, IDisposable
    {
        T Value { get; }
    }
    
    [PublicAPI]
    public class SimpleProperty<T>: IProperty<T>
    {
        public event IObservable.OnUpdatedHandler OnUpdated;
        public event IObservable<T>.OnChangedHandler OnChanged;

        public T Value
        {
            get => value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(value, this.value))
                    return;
                
                this.value = value;
                OnUpdated?.Invoke();
                OnChanged?.Invoke(value);
            }
        }
        
        private T value;

        public static implicit operator T(SimpleProperty<T> v) => v.Value;
        public SimpleProperty(T initialValue) => value = initialValue;
        public SimpleProperty() => value = default;
        public void Dispose()
        {
            OnUpdated = null;
            OnChanged = null;
            value = default;
        }
    }
    
    [PublicAPI]
    public class ComputedProperty<T>: IProperty<T>
    {
        public delegate T ValueFactory();
        
        public event IObservable.OnUpdatedHandler OnUpdated;
        public event IObservable<T>.OnChangedHandler OnChanged;

        public T Value { get; private set; }

        private ValueFactory factory;
        private IObservable[] dependencies;

        public static implicit operator T(ComputedProperty<T> v) => v.Value;

        public ComputedProperty(ValueFactory factory, params IObservable[] dependencies)
        {
            this.dependencies = dependencies;
            this.factory = factory;
            Value = factory();
            foreach (var dependency in dependencies)
                dependency.OnUpdated += RecalculateAndCache;
        }
        public void Dispose()
        {
            foreach (var dependency in dependencies)
                dependency.OnUpdated -= RecalculateAndCache;
            
            OnUpdated = null;
            OnChanged = null;
            Value = default;
        }

        private void RecalculateAndCache()
        {
            Value = factory();
            
            OnUpdated?.Invoke();
            OnChanged?.Invoke(Value);
        }
    }

    [PublicAPI]
    public static class PropertyExtensions
    {
        public static IProperty<TResult> Select<TSource, TResult>(this IProperty<TSource> property, Func<TSource, TResult> map) => new ComputedProperty<TResult>(() => map(property.Value), property);
        public static IProperty<T> Subscribe<T>(this IProperty<T> property, IObservable<T>.OnChangedHandler onChanged)
        {
            property.OnChanged += onChanged;
            return property;
        }
    }
}