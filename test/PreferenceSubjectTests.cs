using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using FluentAssertions;
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

			subject.HasObservers.Should().BeFalse();
			subject.IsDisposed.Should().BeFalse();
			subject.Value.Should().Be(new Foo(1, "2"));
			subject.Value.Should().Be(new Foo(0, "0") with { X = 1, Y = "2" });
			subject.TryGetValue(out Foo? foo).Should().Be(true);
			foo!.X.Should().Be(1);
			foo.Y.Should().Be("2");

			new Action([ExcludeFromCodeCoverage] () => subject.Subscribe(null!)).Should().Throw<ArgumentNullException>().WithParameterName("observer");

			List<Foo> emittedValues = new();
			IDisposable subscription = subject.Subscribe(value => emittedValues.Add(value));
			emittedValues.Should().ContainInOrder(new Foo(1, "2"));
			subject.HasObservers.Should().BeTrue();

			subject.OnNext(new Foo(2, "3"));
			emittedValues.Should().ContainInOrder(new Foo(1, "2"), new Foo(2, "3"));

			List<Foo> emittedValues2 = new();
			bool completed2 = false;
			IDisposable subscription2 = subject.Subscribe(value => emittedValues2.Add(value), () => completed2 = true);
			emittedValues2.Should().ContainInOrder(new Foo(2, "3"));
			completed2.Should().BeFalse();

			subject.OnNext(new Foo(3, "4"));
			emittedValues.Should().ContainInOrder(new Foo(1, "2"), new Foo(2, "3"), new Foo(3, "4"));
			emittedValues2.Should().ContainInOrder(new Foo(2, "3"), new Foo(3, "4"));

			subscription.Dispose();
			subject.OnNext(new Foo(4, "5"));
			emittedValues.Should().ContainInOrder(new Foo(1, "2"), new Foo(2, "3"), new Foo(3, "4"));
			emittedValues2.Should().ContainInOrder(new Foo(2, "3"), new Foo(3, "4"), new Foo(4, "5"));

			subject.OnCompleted();
			subject.HasObservers.Should().BeFalse();
			emittedValues.Should().ContainInOrder(new Foo(1, "2"), new Foo(2, "3"), new Foo(3, "4"));
			emittedValues2.Should().ContainInOrder(new Foo(2, "3"), new Foo(3, "4"), new Foo(4, "5"));
			completed2.Should().BeTrue();

			List<Foo> emittedValues3 = new();
			bool completed3 = false;
			IDisposable subscription3 = subject.Subscribe([ExcludeFromCodeCoverage] (value) => emittedValues3.Add(value), () => completed3 = true);
			emittedValues3.Should().BeEmpty();
			completed3.Should().BeTrue();
			subscription3.Should().Be(Disposable.Empty);
			subscription3.Dispose();

			subject.OnNext(new Foo(5, "6"));
			emittedValues.Should().ContainInOrder(new Foo(1, "2"), new Foo(2, "3"), new Foo(3, "4"));
			emittedValues2.Should().ContainInOrder(new Foo(2, "3"), new Foo(3, "4"), new Foo(4, "5"));

			subject.Dispose();
			subject.IsDisposed.Should().BeTrue();
			new Action([ExcludeFromCodeCoverage] () => _ = subject.Value).Should().Throw<ObjectDisposedException>();
			subject.TryGetValue(out foo).Should().BeFalse();
			foo.Should().BeNull();

			new Action([ExcludeFromCodeCoverage] () => _ = subject.Subscribe(value => { })).Should().Throw<ObjectDisposedException>();

			subject = MockSubject();

			List<Foo> emittedValues4 = new();
			Exception? exception4 = null;
			IDisposable subscription4 = subject.Subscribe(value => emittedValues4.Add(value), exc => exception4 = exc);
			emittedValues4.Should().ContainInOrder(new Foo(4, "5"));
			exception4.Should().BeNull();

			new Action([ExcludeFromCodeCoverage] () => subject.OnError(null!)).Should().Throw<ArgumentNullException>().WithParameterName("error");

			subject.OnError(new InvalidOperationException());
			exception4.Should().BeOfType<InvalidOperationException>();
			subject.HasObservers.Should().BeFalse();
			new Action([ExcludeFromCodeCoverage] () => _ = subject.Value).Should().Throw<InvalidOperationException>();
			new Action([ExcludeFromCodeCoverage] () => _ = subject.TryGetValue(out foo)).Should().Throw<InvalidOperationException>();
			subscription4.Dispose();

			List<Foo> emittedValues5 = new();
			Exception? exception5 = null;
			IDisposable subscription5 = subject.Subscribe([ExcludeFromCodeCoverage] (value) => emittedValues5.Add(value), exc => exception5 = exc);
			emittedValues5.Should().BeEmpty();
			exception5.Should().BeOfType<InvalidOperationException>();
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
			emittedValues1.Should().ContainInOrder(0);

			subject.OnNext(1);
			subject.OnNext(2);
			emittedValues1.Should().ContainInOrder(0, 1, 2);

			subject = MockSubject();
			List<int> emittedValues2 = new();
			using IDisposable subscription2 = subject.Subscribe([ExcludeFromCodeCoverage] (value) => emittedValues2.Add(value));
			emittedValues2.Should().ContainInOrder(2);
		}
	}
}
