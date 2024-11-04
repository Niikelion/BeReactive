# BeReactive

Reactive library inspired by System.Reactive that does not require any dependency or additional setup.

# Concepts

## IObservable

`Utils.BR.IObservable` interface provides event `OnUpdated` that is invoked every time `IObservable` is updated.

For convenience `Utils.BR.Observable` implements this interface and provides `void Update()` method to invoke the event.

`Utils.BR.IObservable<T>: IObservable` interface provides event `OnChanged` that is invoked along the `OnUpdated` event, but passes current value as the argument. 

## IProperty

`Utils.BR.IProperty<T>: IObservable<T>` interface provides read only property `T Value`.

`Utils.BR.SimpleProperty<T>: IProperty<T>` is used as simple property/variable, that provides setter for `Value`, which invokes events when the value changes.

`Utils.BR.ComputedProperty<T>: IProperty<T>` is used to represent property computed based on list of observables.

# Extensions

Coming soon.