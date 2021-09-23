using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Text.Json;
using System.Threading;
using Microsoft.Maui.Essentials;
using RG.MAUI.Redux.Internals;

namespace RG.MAUI.Redux {
	public class PreferenceSubject<TState> : SubjectBase<TState> where TState : notnull {
		private readonly object _gate = new();
		private readonly string _key;
		private ImmutableList<IObserver<TState>> _observers;
		private bool _isStopped;
		private TState _value;
		private Exception? _exception;
		private bool _isDisposed;

		public PreferenceSubject(
			string key,
			TState defaultValue
		) {
			_key = key;
			_value = ReadFromPreferences(key, defaultValue);
			_observers = ImmutableList<IObserver<TState>>.Empty;
		}

		public override bool HasObservers => _observers.Count > 0;

		public override bool IsDisposed {
			get {
				lock (_gate) {
					return _isDisposed;
				}
			}
		}

		public TState Value {
			get {
				lock (_gate) {
					CheckDisposed();
					CheckException();
					return _value;
				}
			}
		}

		public bool TryGetValue([NotNullWhen(true)] out TState? value) {
			lock (_gate) {
				if (_isDisposed) {
					value = default!;
					return false;
				}
				CheckException();
				value = _value;
				return true;
			}
		}

		public override void OnCompleted() {
			ImmutableList<IObserver<TState>>? observers = null;
			lock (_gate) {
				CheckDisposed();
				if (!_isStopped) {
					observers = _observers;
					_observers = ImmutableList<IObserver<TState>>.Empty;
					_isStopped = true;
				}
			}
			if (observers != null) {
				foreach (IObserver<TState> observer in observers) {
					observer.OnCompleted();
				}
			}
		}

		public override void OnError(Exception error) {
			if (error == null) {
				throw new ArgumentNullException(nameof(error));
			}
			ImmutableList<IObserver<TState>>? observers = null;
			lock (_gate) {
				CheckDisposed();
				if (!_isStopped) {
					observers = _observers;
					_observers = ImmutableList<IObserver<TState>>.Empty;
					_isStopped = true;
					_exception = error;
				}
			}
			if (observers != null) {
				foreach (IObserver<TState> observer in observers) {
					observer.OnError(error);
				}
			}
		}

		public override void OnNext(TState value) {
			ImmutableList<IObserver<TState>>? observers = null;
			lock (_gate) {
				CheckDisposed();
				if (!_isStopped) {
					WriteToPreferences(_key, value);
					_value = value;
					observers = _observers;
				}
			}
			if (observers != null) {
				foreach (IObserver<TState> observer in observers) {
					observer.OnNext(value);
				}
			}
		}

		public override IDisposable Subscribe(IObserver<TState> observer) {
			if (observer == null) {
				throw new ArgumentNullException(nameof(observer));
			}
			Exception? exc;
			lock (_gate) {
				CheckDisposed();
				if (!_isStopped) {
					_observers = _observers.Add(observer);
					observer.OnNext(_value);
					return new Subscription(this, observer);
				}
				exc = _exception;
			}
			if (exc != null) {
				observer.OnError(exc);
			} else {
				observer.OnCompleted();
			}
			return Disposable.Empty;
		}

		private void Unsubscribe(IObserver<TState> observer) {
			lock (_gate) {
				if (!_isDisposed) {
					_observers = _observers.Remove(observer);
				}
			}
		}

		public override void Dispose() {
			lock (_gate) {
				_isDisposed = true;
				_observers = null!;
				_value = default!;
				_exception = null;
			}
		}

		[ExcludeFromCodeCoverage]
		private void CheckException() => _exception?.Throw();

		private void CheckDisposed() {
			if (_isDisposed) {
				throw new ObjectDisposedException("");
			}
		}

		[ExcludeFromCodeCoverage]
		protected virtual void WriteToPreferences(string key, TState value) {
			switch (value) {
				case null:
					throw new ArgumentNullException(nameof(value));
				case string s:
					Preferences.Set(key, s);
					break;
				case decimal d:
					Preferences.Set(key, d.ToString(CultureInfo.InvariantCulture));
					break;
				case long l:
					Preferences.Set(key, l);
					break;
				case int i:
					Preferences.Set(key, i);
					break;
				case double d:
					Preferences.Set(key, d);
					break;
				case bool b:
					Preferences.Set(key, b);
					break;
				case float f:
					Preferences.Set(key, f);
					break;
				case DateTime d:
					Preferences.Set(key, d.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture));
					break;
				case TimeSpan t:
					Preferences.Set(key, t.ToString("G", CultureInfo.InvariantCulture));
					break;
				case Guid g:
					Preferences.Set(key, g.ToString("N", CultureInfo.InvariantCulture));
					break;
				default:
					Preferences.Set(key, JsonSerializer.Serialize(value));
					break;
			}
		}

		[ExcludeFromCodeCoverage]
		protected virtual TState ReadFromPreferences(string key, TState defaultValue) {
			switch (defaultValue) {
				case null: throw new ArgumentNullException(nameof(defaultValue));
				case string s: return (TState)(object)Preferences.Get(key, s);
				case decimal d:
					return (TState)(object)decimal.Parse(
						s: Preferences.Get(key, Preferences.ContainsKey(key) ? "" : d.ToString(CultureInfo.InvariantCulture)),
						provider: CultureInfo.InvariantCulture);
				case long l: return (TState)(object)Preferences.Get(key, l);
				case int i: return (TState)(object)Preferences.Get(key, i);
				case double d: return (TState)(object)Preferences.Get(key, d);
				case bool b: return (TState)(object)Preferences.Get(key, b);
				case float f: return (TState)(object)Preferences.Get(key, f);
				case DateTime d:
					return (TState)(object)DateTime.ParseExact(
						s: Preferences.Get(key, Preferences.ContainsKey(key) ? "" : d.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)),
						format: "yyyy-MM-dd HH:mm:ss.ffffff",
						provider: CultureInfo.InvariantCulture);
				case TimeSpan t:
					return (TState)(object)TimeSpan.ParseExact(
						input: Preferences.Get(key, Preferences.ContainsKey(key) ? "" : t.ToString("G", CultureInfo.InvariantCulture)),
						format: "G",
						formatProvider: CultureInfo.InvariantCulture);
				case Guid g:
					return (TState)(object)Guid.Parse(
						input: Preferences.Get(key, Preferences.ContainsKey(key) ? "" : g.ToString("N", CultureInfo.InvariantCulture)));
				default:
					return JsonSerializer.Deserialize<TState>(
						json: Preferences.Get(key, Preferences.ContainsKey(key) ? "" : JsonSerializer.Serialize(defaultValue)))!;
			}
		}

		private sealed class Subscription : IDisposable {
			private PreferenceSubject<TState> _subject;
			private IObserver<TState>? _observer;

			public Subscription(PreferenceSubject<TState> subject, IObserver<TState> observer) {
				_subject = subject;
				_observer = observer;
			}

			[ExcludeFromCodeCoverage]
			public void Dispose() {
				IObserver<TState>? observer = Interlocked.Exchange(ref _observer, null);
				if (observer == null) {
					return;
				}

				_subject.Unsubscribe(observer);
				_subject = null!;
			}
		}
	}
}
