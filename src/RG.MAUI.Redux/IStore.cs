using System;

namespace RG.MAUI.Redux {
	public interface IStore<TState, TEvent> : IObservable<TState> where TState : notnull where TEvent : IEvent {
		TState State { get; }
		TEvent Dispatch(TEvent @event);
	}

	public interface IStore<TState> : IStore<TState, IEvent> where TState : notnull { }
}
