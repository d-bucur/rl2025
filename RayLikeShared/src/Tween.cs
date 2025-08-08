// copied with minor modifications from https://github.com/d-bucur/flecs-survivors/blob/main/src/Tween.cs
using Friflo.Engine.ECS;

interface IPropertyTween {
	void Tick(float delta, Entity ent);
	void Finish(Entity ent);
	public bool IsFinished();
}

delegate void ComponentSetter<C, P>(ref C component, P property);
delegate void EndCallback<C>(ref C comp);

// TODO change API for better composition in helper methods:
// - basic params that user has to set using <P>: From, To, etc. Passed in constructor
// - params that can be added by helper methods that use <C>: Setter, LerpFunc.
// - optional params can be set using struct field syntax. can have an extra end CB without params
// should allow for easier to use call sites, like DOTween
record struct PropertyTween<C, P>(
	ComponentSetter<C, P> Setter,
	P From,
	P To,
	float RunTime,
	Func<float, float> EasingFunc,
	Func<P, P, float, P> LerpFunc,
	EndCallback<C>? OnEnd = null,
	bool AutoReverse = false,
	int Repetitions = 1
) : IPropertyTween where C : struct, IComponent {
	float CurrentTime = 0;
	int CurrentRepetitions = 0;
	readonly int Repetitions = AutoReverse ? Repetitions * 2 : Repetitions;
	bool IsReversed = false;

	public void Tick(float delta, Entity ent) {
		if (IsFinished()) return;
		CurrentTime += delta;
		float x = CurrentTime / RunTime;
		var t = IsReversed ? EasingFunc(1 - x) : EasingFunc(x);
		var val = LerpFunc(From, To, t);
		ref var comp = ref ent.GetComponent<C>();
		Setter(ref comp, val);
		if (CurrentTime > RunTime) {
			CurrentRepetitions++;
			if (ShouldRepeat()) {
				CurrentTime = 0;
				if (AutoReverse) {
					Reverse();
				}
			}
		}
	}

	void Reverse() {
		CurrentTime = 0;
		IsReversed = !IsReversed;
	}

	readonly bool ShouldRepeat() {
		return Repetitions < 0 || CurrentRepetitions < Repetitions;
	}

	public bool IsFinished() {
		return !ShouldRepeat();
	}

	public void Finish(Entity ent) {
		// Make sure final value is set
		Setter(ref ent.GetComponent<C>(), IsReversed ? From : To);
		OnEnd?.Invoke(ref ent.GetComponent<C>());
	}
}

record struct Tween(Entity target) : IComponent {
	// Now this is pretty simple, but this is the place where you could
	// combine different PropertyTweens in sequence, parallel etc.
	List<IPropertyTween> PropertyTweens = new();

	public void Tick(float delta) {
		foreach (var t in PropertyTweens) {
			t.Tick(delta, target);
		}
	}

	public bool IsFinished() {
		foreach (var t in PropertyTweens) {
			if (!t.IsFinished()) return false;
		}
		return true;
	}

	public void Cleanup() {
		foreach (var p in PropertyTweens) {
			p.Finish(target);
		}
	}

	public void RegisterEcs() {
		target.Store.CreateEntity(this);
	}

	#region generic property
	public Tween With<C, P>(
		ComponentSetter<C, P> Setter,
		P From,
		P To,
		float Time,
		Func<float, float> EasingFunc,
		Func<P, P, float, P> LerpFunc,
		EndCallback<C>? OnEnd = null,
		bool AutoReverse = false,
		int Repetitions = 1
	) where C : struct, IComponent {
		PropertyTweens.Add(new PropertyTween<C, P>(
			Setter,
			From,
			To,
			Time,
			EasingFunc,
			LerpFunc,
			OnEnd,
			AutoReverse,
			Repetitions
		));
		return this;
	}
	#endregion

	#region typed properties
	public static float LerpFloat(float v0, float v1, float t) {
		return v0 + t * (v1 - v0);
	}
	public Tween With<C>(
		ComponentSetter<C, float> Setter,
		float From,
		float To,
		float Time,
		Func<float, float> EasingFunc,
		EndCallback<C>? OnEnd = null,
		bool AutoReverse = false,
		int Repetitions = 1
	) where C : struct, IComponent {
		PropertyTweens.Add(new PropertyTween<C, float>(
			Setter,
			From,
			To,
			Time,
			EasingFunc,
			LerpFloat,
			OnEnd,
			AutoReverse,
			Repetitions
		));
		return this;
	}
	#endregion
}

public class Ease {
	#region easings
	// More: https://easings.net/
	public static float Linear(float x) {
		return x;
	}
	public static float QuartOut(float x) {
		return 1 - MathF.Pow(1 - x, 4);
	}
	public static float QuartIn(float x) {
		return x * x * x * x;
	}
	public static float SineOut(float x) {
		return MathF.Sin(x * MathF.PI / 2);
	}
	public static float SineIn(float x) {
		return 1 - MathF.Cos(x * MathF.PI / 2);
	}
	public static float SineInOut(float x) {
		return -(MathF.Cos(MathF.PI * x) - 1) / 2;
	}
	#endregion
}

// Project specific convenience methods
static class TweenExtensions {
	public static Tween TweenPosition(this Entity entt,
		System.Numerics.Vector3 From,
		System.Numerics.Vector3 To,
		float Time,
		Func<float, float> EasingFunc,
		EndCallback<Position>? OnEnd = null,
		bool AutoReverse = false,
		int Repetitions = 1
	) {
		return new Tween(entt).With(
			(ref Position pos, System.Numerics.Vector3 v) => pos.value = v,
			From, To, Time, EasingFunc, System.Numerics.Vector3.Lerp, OnEnd, AutoReverse, Repetitions
		);
	}
}