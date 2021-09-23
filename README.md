# RG.MAUI.Redux

[![NuGet](https://img.shields.io/nuget/v/RG.MAUI.Redux.svg)](https://www.nuget.org/packages/RG.MAUI.Redux/) [![.NET](https://github.com/ronnygunawan/maui-redux/actions/workflows/dotnet.yml/badge.svg)](https://github.com/ronnygunawan/maui-redux/actions/workflows/dotnet.yml)

A minimal Redux implementation for MAUI projects

### Creating Events, Reducer, and Store

```cs
// Events
public interface IFooEvent : IEvent { }
public record FooIncremented() : IFooEvent;
public record FooDecremented() : IFooEvent;
public record FooIncrementedBy(int Value) : IFooEvent;
public record FooReset() : IFooEvent;

// Store and Reducer
public record FooStore() : Store<int, IFooEvent>(
    reducer: (state, action) =>
        action switch {
            FooIncremented => state + 1,
            FooDecremented => state - 1,
            FooIncrementedBy { Value: var val } => state + val,
            FooReset => 0,
            _ => state_
        }.
    initialValue: 0
);

// Creating a Store instance
var fooStore = new FooStore();
```

### Reading State

```cs
var state = fooStore.State;
```

### Dispatching Event

```cs
fooStore.Dispatch(new FooDecremented());
```

### Subscribing to Store updates

```cs
var subscription = fooStore.Subscribe(value => {
    Console.WriteLine(value);
});
```

### Unsubscribing from Store updates

```cs
subscription.Dispose();
```

### Querying Store updates using Reactive.Linq

```cs
var query = from value in fooStore
            where value % 2 == 0
            select value / 2;

var subscription = query.Subscribe(value => {
    Console.WriteLine(value);
});
```

### Persisting value to MAUI Preferences

```cs
public record PersistedFooStore() : PreferenceStore<int, IFooEvent>(
    key: "Foo",
    reducer: (state, action) =>
        action switch {
            FooIncremented => state + 1,
            FooDecremented => state - 1,
            FooIncrementedBy { Value: var val } => state + val,
            FooReset => 0,
            _ => state_
        }.
    initialValue: 0
);
```