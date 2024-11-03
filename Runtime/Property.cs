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
                if (Comparer<T>.Default.Compare(value, this.value) == 0)
                    return;
                
                this.value = value;
                OnUpdated?.Invoke();
                OnChanged?.Invoke(value);
            }
        }
        
        private T value;

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
    public class CalculatedProperty<T>: IProperty<T>
    {
        public delegate T ValueFactory();
        
        public event IObservable.OnUpdatedHandler OnUpdated;
        public event IObservable<T>.OnChangedHandler OnChanged;

        public T Value { get; private set; }

        private ValueFactory factory;
        private IObservable[] dependencies;
        
        public CalculatedProperty(ValueFactory factory, params IObservable[] dependencies)
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
        public static IProperty<TResult> Select<TSource, TResult>(this IProperty<TSource> property, Func<TSource, TResult> map) => new CalculatedProperty<TResult>(() => map(property.Value), property);
        public static IProperty<T> Run<T>(this IProperty<T> property, IObservable<T>.OnChangedHandler onChanged)
        {
            property.OnChanged += onChanged;
            return property;
        }
    }
}