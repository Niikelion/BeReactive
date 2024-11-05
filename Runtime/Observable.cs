using JetBrains.Annotations;

namespace Utils.BR
{
    [PublicAPI]
    public interface IObservable
    {
        delegate void OnUpdatedHandler();
        public event OnUpdatedHandler OnUpdated;
    }

    [PublicAPI]
    public interface IObservable<T>: IObservable
    {
        delegate void OnChangedHandler(T value);
        public event OnChangedHandler OnChanged;
    }
    
    [PublicAPI]
    public class Observable : IObservable
    {
        public event IObservable.OnUpdatedHandler OnUpdated;
        public void Update() => OnUpdated?.Invoke();
    }

    [PublicAPI]
    public static class ObservableExtensions
    {
        public static T Subscribe<T>(this T property, IObservable.OnUpdatedHandler onUpdated) where T: IObservable
        {
            property.OnUpdated += onUpdated;
            return property;
        }
        public static IObservable<T> Subscribe<T>(this IObservable<T> property, IObservable<T>.OnChangedHandler onChanged)
        {
            property.OnChanged += onChanged;
            return property;
        }
    }
}