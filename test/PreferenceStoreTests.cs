using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using FluentAssertions;
using Moq;
using Moq.Protected;
using RG.MAUI.Redux;
using Xunit;

namespace Tests {
	public class PreferenceStoreTests {
		private record SetTo(string Value) : IEvent;
		private record Clear() : IEvent;
		private record Unknown() : IEvent;

		private static readonly Reducer<string, IEvent> STRING_REDUCER = (state, action) =>
			action switch {
				SetTo { Value: var value } => value,
				Clear => "",
				_ => state
			};

		[Fact]
		public void CanCreatePreferenceStore() {
			Dictionary<string, string> preferencesMock = new();

			PreferenceSubject<string> MockSubject() {
				Mock<PreferenceSubject<string>> subjectMock = new("foo", "") {
					CallBase = true
				};

				subjectMock
					.Protected()
					.Setup(
						"WriteToPreferences",
						ItExpr.IsAny<string>(),
						ItExpr.IsAny<string>()
					)
					.Callback<string, string>((key, value) => preferencesMock[key] = value);

				subjectMock
					.Protected()
					.Setup<string>(
						"ReadFromPreferences",
						ItExpr.IsAny<string>(),
						ItExpr.IsAny<string>()
					)
					.Returns<string, string>((key, defaultValue) => {
						if (preferencesMock.TryGetValue(key, out string? value)) {
							return value;
						} else {
							return defaultValue;
						}
					});

				return subjectMock.Object;
			}

			using (PreferenceStore<string> store = new MockPreferenceStore<string>("foo", STRING_REDUCER, "", MockSubject())) {
				store.Key.Should().Be("foo");
				store.State.Should().Be("");
				store.Dispatch(new SetTo("asd"));
				store.State.Should().Be("asd");
				store.Dispatch(new SetTo("") with { Value = "fgh" });
				store.State.Should().Be("fgh");
				store.Dispatch(new Clear());
				store.State.Should().Be("");
				store.Dispatch(new SetTo("ijk"));
				store.State.Should().Be("ijk");
				store.Dispatch(new Unknown());
				store.State.Should().Be("ijk");
			}

			using (PreferenceStore<string> store = new MockPreferenceStore<string>("foo", STRING_REDUCER, "", MockSubject())) {
				store.State.Should().Be("ijk");
				List<string> emittedValues = new();
				using IDisposable subscription = (from value in store
												  where value.Length == 3
												  select value).Subscribe(value => emittedValues.Add(value));
				emittedValues.Should().ContainInOrder("ijk");
				store.Dispatch(new SetTo("1234"));
				emittedValues.Should().ContainInOrder("ijk");
			}
		}

		[ExcludeFromCodeCoverage]
		record MockPreferenceStore<TState>(string Key, Reducer<TState, IEvent> Reducer, TState InitialValue, PreferenceSubject<TState> MockPreferenceSubject) : PreferenceStore<TState>(Key, Reducer, InitialValue) where TState : notnull {
			protected override PreferenceSubject<TState> PreferenceSubject => MockPreferenceSubject;
		}
	}
}
