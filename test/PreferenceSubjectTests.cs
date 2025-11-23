using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using Shouldly;
using Moq;
using Moq.Protected;
using RG.MAUI.Redux;
using Xunit;

namespace Tests {
	public class PreferenceSubjectTests {
		public record Foo(int X, string Y);

		[Fact]
		public void CanCreatePreferenceSubjectOfReferenceType() {
			Dictionary<string, Foo> preferencesMock = new();

			PreferenceSubject<Foo> MockSubject() {
				Mock<PreferenceSubject<Foo>> subjectMock = new("foo", new Foo(1, "2")) {
					CallBase = true
				};

				subjectMock
					.Protected()
					.Setup(
						"WriteToPreferences",
						ItExpr.IsAny<string>(),
						ItExpr.IsAny<Foo>()
					)
					.Callback<string, Foo>((key, value) => preferencesMock[key] = value);

				subjectMock
					.Protected()
					.Setup<Foo>(
						"ReadFromPreferences",
						ItExpr.IsAny<string>(),
						ItExpr.IsAny<Foo>()
					)
					.Returns<string, Foo>((key, defaultValue) => {
						if (preferencesMock.TryGetValue(key, out Foo? value)) {
							return value;
						} else {
							return defaultValue;
						}
					});

				return subjectMock.Object;
			}

			PreferenceSubject<Foo> subject = MockSubject();

			subject.HasObservers.ShouldBeFalse();
			subject.IsDisposed.ShouldBeFalse();
			subject.Value.ShouldBe(new Foo(1, "2"));
			subject.Value.ShouldBe(new Foo(0, "0") with { X = 1, Y = "2" });
			subject.TryGetValue(out Foo? foo).ShouldBe(true);
			foo!.X.ShouldBe(1);
			foo.Y.ShouldBe("2");

			Should.Throw<ArgumentNullException>(() => subject.Subscribe(null!)).ParamName.ShouldBe("observer");

			List<Foo> emittedValues = new();
			IDisposable subscription = subject.Subscribe(value => emittedValues.Add(value));
			emittedValues.ShouldBe(new[] { new Foo(1, "2") });
			subject.HasObservers.ShouldBeTrue();

			subject.OnNext(new Foo(2, "3"));
			emittedValues.ShouldBe(new[] { new Foo(1, "2"), new Foo(2, "3") });

			List<Foo> emittedValues2 = new();
			bool completed2 = false;
			IDisposable subscription2 = subject.Subscribe(value => emittedValues2.Add(value), () => completed2 = true);
			emittedValues2.ShouldBe(new[] { new Foo(2, "3") });
			completed2.ShouldBeFalse();

			subject.OnNext(new Foo(3, "4"));
			emittedValues.ShouldBe(new[] { new Foo(1, "2"), new Foo(2, "3"), new Foo(3, "4") });
			emittedValues2.ShouldBe(new[] { new Foo(2, "3"), new Foo(3, "4") });

			subscription.Dispose();
			subject.OnNext(new Foo(4, "5"));
			emittedValues.ShouldBe(new[] { new Foo(1, "2"), new Foo(2, "3"), new Foo(3, "4") });
			emittedValues2.ShouldBe(new[] { new Foo(2, "3"), new Foo(3, "4"), new Foo(4, "5") });

			subject.OnCompleted();
			subject.HasObservers.ShouldBeFalse();
			emittedValues.ShouldBe(new[] { new Foo(1, "2"), new Foo(2, "3"), new Foo(3, "4") });
			emittedValues2.ShouldBe(new[] { new Foo(2, "3"), new Foo(3, "4"), new Foo(4, "5") });
			completed2.ShouldBeTrue();

			List<Foo> emittedValues3 = new();
			bool completed3 = false;
			IDisposable subscription3 = subject.Subscribe([ExcludeFromCodeCoverage] (value) => emittedValues3.Add(value), () => completed3 = true);
			emittedValues3.ShouldBeEmpty();
			completed3.ShouldBeTrue();
			subscription3.ShouldBe(Disposable.Empty);
			subscription3.Dispose();

			subject.OnNext(new Foo(5, "6"));
			emittedValues.ShouldBe(new[] { new Foo(1, "2"), new Foo(2, "3"), new Foo(3, "4") });
			emittedValues2.ShouldBe(new[] { new Foo(2, "3"), new Foo(3, "4"), new Foo(4, "5") });

			subject.Dispose();
			subject.IsDisposed.ShouldBeTrue();
			Should.Throw<ObjectDisposedException>(() => _ = subject.Value);
			subject.TryGetValue(out foo).ShouldBeFalse();
			foo.ShouldBeNull();

			Should.Throw<ObjectDisposedException>(() => _ = subject.Subscribe(value => { }));

			subject = MockSubject();

			List<Foo> emittedValues4 = new();
			Exception? exception4 = null;
			IDisposable subscription4 = subject.Subscribe(value => emittedValues4.Add(value), exc => exception4 = exc);
			emittedValues4.ShouldBe(new[] { new Foo(4, "5") });
			exception4.ShouldBeNull();

			Should.Throw<ArgumentNullException>(() => subject.OnError(null!)).ParamName.ShouldBe("error");

			subject.OnError(new InvalidOperationException());
			exception4.ShouldBeOfType<InvalidOperationException>();
			subject.HasObservers.ShouldBeFalse();
			Should.Throw<InvalidOperationException>(() => _ = subject.Value);
			Should.Throw<InvalidOperationException>(() => _ = subject.TryGetValue(out foo));
			subscription4.Dispose();

			List<Foo> emittedValues5 = new();
			Exception? exception5 = null;
			IDisposable subscription5 = subject.Subscribe([ExcludeFromCodeCoverage] (value) => emittedValues5.Add(value), exc => exception5 = exc);
			emittedValues5.ShouldBeEmpty();
			exception5.ShouldBeOfType<InvalidOperationException>();
			subscription5.Dispose();
		}

		[Fact]
		public void CanCreatePreferenceSubjectOfValueType() {
			Dictionary<string, int> preferencesMock = new();

			PreferenceSubject<int> MockSubject() {
				Mock<PreferenceSubject<int>> subjectMock = new("bar", 0) {
					CallBase = true
				};

				subjectMock
					.Protected()
					.Setup(
						"WriteToPreferences",
						ItExpr.IsAny<string>(),
						ItExpr.IsAny<int>()
					)
					.Callback<string, int>((key, value) => preferencesMock[key] = value);

				subjectMock
					.Protected()
					.Setup<int>(
						"ReadFromPreferences",
						ItExpr.IsAny<string>(),
						ItExpr.IsAny<int>()
					)
					.Returns<string, int>((key, defaultValue) => {
						if (preferencesMock.TryGetValue(key, out int value)) {
							return value;
						} else {
							return defaultValue;
						}
					});

				return subjectMock.Object;
			}

			PreferenceSubject<int> subject = MockSubject();

			List<int> emittedValues1 = new();
			using IDisposable subscription1 = subject.Subscribe([ExcludeFromCodeCoverage] (value) => emittedValues1.Add(value));
			emittedValues1.ShouldBe(new[] { 0 });

			subject.OnNext(1);
			subject.OnNext(2);
			emittedValues1.ShouldBe(new[] { 0, 1, 2 });

			subject = MockSubject();
			List<int> emittedValues2 = new();
			using IDisposable subscription2 = subject.Subscribe([ExcludeFromCodeCoverage] (value) => emittedValues2.Add(value));
			emittedValues2.ShouldBe(new[] { 2 });
		}
	}
}
