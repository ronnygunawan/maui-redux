using System;
using System.Diagnostics.CodeAnalysis;

namespace RG.MAUI.Redux {
	public record PreferenceStore<TState, TEvent> : IStore<TState, TEvent>, IDisposable where TState : notnull where TEvent : IEvent {
		public string Key { get; }
		private readonly Reducer<TState, TEvent> _reducer;
		private readonly TState _initialValue;
		private bool _disposedValue;

		private PreferenceSubject<TState>? _preferenceSubject;

		[ExcludeFromCodeCoverage]
		protected virtual PreferenceSubject<TState> PreferenceSubject =>
			_preferenceSubject ??= new PreferenceSubject<TState>(Key, _initialValue);

		public TState State => PreferenceSubject.Value;

		public PreferenceStore(string key, Reducer<TState, TEvent> reducer, TState initialValue) {
			Key = key;
			_reducer = reducer;
			_initialValue = initialValue;
		}

		public IDisposable Subscribe(IObserver<TState> observer) => PreferenceSubject.Subscribe(observer);

		public TEvent Dispatch(TEvent @event) {
			TState newValue = _reducer.Invoke(PreferenceSubject.Value, @event);
			PreferenceSubject.OnNext(newValue);
			return @event;
		}

		[ExcludeFromCodeCoverage(Justification = "_preferenceSubject will always be null when this class is mocked.")]
		protected virtual void Dispose(bool disposing) {
			if (!_disposedValue) {
				if (disposing) {
					// dispose managed state (managed objects)
					_preferenceSubject?.Dispose();
				}

				_disposedValue = true;
			}
		}

		public void Dispose() {
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}

	public record PreferenceStore<TState> : PreferenceStore<TState, IEvent> where TState : notnull {
		public PreferenceStore(string key, Reducer<TState, IEvent> reducer, TState initialValue) : base(key, reducer, initialValue) { }
	}
}
