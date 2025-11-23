using System;
using System.Collections.Generic;
using Shouldly;
using RG.MAUI.Redux;
using System.Reactive.Linq;
using Xunit;

namespace Tests {
	public class StoreTests {
		private static readonly object OBJECT_A = new();
		private static readonly object OBJECT_B = new();
		private static readonly object OBJECT_C = new();

		private record SetToObjectA() : IEvent;
		private record SetToObjectB() : IEvent;
		private record SetToObject(object Obj) : IEvent;

		private static readonly Reducer<object, IEvent> OBJECT_REDUCER = (state, action) =>
			action switch {
				SetToObjectA => OBJECT_A,
				SetToObjectB => OBJECT_B,
				SetToObject { Obj: var obj } => obj,
				_ => state,
			};

		private record SetToZero() : IEvent;
		private record Negate() : IEvent;
		private record SetTo(int Value) : IEvent;

		private static readonly Reducer<int, IEvent> INT_REDUCER = (state, action) =>
			action switch {
				SetToZero => 0,
				Negate => -state,
				SetTo { Value: var value } => value,
				_ => state
			};

		[Fact]
		public void CanCreateStoreOfReferenceType() {
			using Store<object> store = new(OBJECT_REDUCER, OBJECT_A);
			store.State.ShouldBe(OBJECT_A);
			store.Dispatch(new SetToObjectA());
			store.State.ShouldBe(OBJECT_A);
			store.Dispatch(new SetToObjectB());
			store.State.ShouldBe(OBJECT_B);
			store.Dispatch(new SetToObject(OBJECT_C));
			store.State.ShouldBe(OBJECT_C);
			store.Dispatch(new SetToObject(OBJECT_C) with { Obj = OBJECT_A });
			store.State.ShouldBe(OBJECT_A);
			store.Dispatch(new SetToZero());
			store.State.ShouldBe(OBJECT_A);
		}

		[Fact]
		public void CanCreateStoreOfValueType() {
			using Store<int> store = new(INT_REDUCER, 0);
			store.State.ShouldBe(0);
			store.Dispatch(new SetTo(10));
			store.State.ShouldBe(10);
			store.Dispatch(new SetTo(10) with { Value = 20 });
			store.State.ShouldBe(20);
			store.Dispatch(new Negate());
			store.State.ShouldBe(-20);
			store.Dispatch(new SetToZero());
			store.State.ShouldBe(0);
			store.Dispatch(new SetToObjectA());
			store.State.ShouldBe(0);
		}

		[Fact]
		public void CanSubscribeToStore() {
			using Store<int> store = new(INT_REDUCER, 0);
			List<int> emittedValues = new();
			IDisposable subscription = store.Subscribe(value => emittedValues.Add(value));
			emittedValues.ShouldBe(new[] { 0 });
			store.Dispatch(new SetTo(20));
			emittedValues.ShouldBe(new[] { 0, 20 });
			List<int> emittedValues2 = new();
			IDisposable subscription2 = store.Subscribe(value => emittedValues2.Add(value));
			emittedValues2.ShouldBe(new[] { 20 });
			store.Dispatch(new Negate());
			emittedValues.ShouldBe(new[] { 0, 20, -20 });
			emittedValues2.ShouldBe(new[] { 20, -20 });
			subscription.Dispose();
			store.Dispatch(new SetToZero());
			emittedValues.ShouldBe(new[] { 0, 20, -20 });
			emittedValues2.ShouldBe(new[] { 20, -20, 0 });
			subscription2.Dispose();
		}

		[Fact]
		public void CanUseReactiveLinq() {
			using Store<int> store = new(INT_REDUCER, 0);
			List<int> emittedValues = new();
			using IDisposable subscription = (from value in store
											  where value % 2 == 0
											  select value / 2).Subscribe(value => emittedValues.Add(value));
			emittedValues.ShouldBe(new[] { 0 });
			store.Dispatch(new SetTo(3));
			emittedValues.ShouldBe(new[] { 0 });
			store.Dispatch(new Negate());
			emittedValues.ShouldBe(new[] { 0 });
			store.Dispatch(new SetToZero());
			emittedValues.ShouldBe(new[] { 0, 0 });
			store.Dispatch(new SetTo(20));
			emittedValues.ShouldBe(new[] { 0, 0, 10 });
		}
	}
}
