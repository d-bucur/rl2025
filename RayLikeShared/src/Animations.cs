using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

static class Animations {
	internal static void Bump(Entity Target, Vector3 startPos, Vector3 endPos, EndCallback<Position>? onEnd = null) {
		new Tween(Target).With(
			(ref Position p, Vector3 v) => { p.x = v.X; p.z = v.Z; },
			startPos,
			endPos,
			0.2f, Ease.SineOut, Vector3.Lerp
		).With(
			(ref Position p, Vector3 v) => { p.x = v.X; p.z = v.Z; },
			endPos,
			startPos,
			0.3f, Ease.SineIn, Vector3.Lerp,
			OnEnd: onEnd
		).RegisterEcs();
	}

	internal static void Move(Entity Target, Entity actionEntt, Vec2I startPos, Vector3 endPos, Action? onEnd = null) {
		// xz anim
		new Tween(Target).With(
			(ref Position p, Vector3 v) => { p.x = v.X; p.z = v.Z; },
			startPos.ToWorldPos(),
			endPos,
			0.2f, Ease.SineInOut, Vector3.Lerp,
			OnEnd: (ref Position p) => {
				actionEntt.AddTag<IsActionFinished>();
				onEnd?.Invoke();
			}
		).RegisterEcs();

		// y anim
		const float jumpHeight = 0.2f;
		new Tween(Target).With(
			(ref Position p, float v) => { p.y = v; },
			0, jumpHeight,
			0.1f, Ease.QuartOut,
			AutoReverse: true
		).RegisterEcs();

		// scale
		// var scale = Entity.GetComponent<Scale3>();
		// var startScale = scale.x;
		// new Tween(Entity).With(
		//     (ref Scale3 s, float v) => { s.x = v; },
		//     startScale, 0.5f,
		//     0.2f, Ease.Linear
		// ).With(
		//     (ref Scale3 s, float v) => { s.x = v; },
		//     0.5f, startScale,
		//     0.2f, Ease.Linear
		// ).RegisterEcs();
	}

	internal static void DamageFx(Entity fxEntt, Position startPos, Scale3 startScale, Vector3 dir, Action? onEnd = null) {
		dir *= 0.75f;
		const float halfTime = 0.25f;
		// TODO should do parallel tween to handle all these
		new Tween(fxEntt).With(
			(ref Scale3 pos, Vector3 v) => pos.value = v,
			startScale.value, new Vector3(0.5f),
			halfTime, Ease.QuartOut, Vector3.Lerp,
			AutoReverse: true
		).RegisterEcs();

		new Tween(fxEntt).With(
			(ref Position pos, float v) => pos.x = v,
			startPos.value.X, startPos.value.X + dir.X,
			halfTime * 2, Ease.SineIn, Tween.LerpFloat
		).RegisterEcs();
		new Tween(fxEntt).With(
			(ref Position pos, float v) => pos.z = v,
			startPos.value.Z, startPos.value.Z + dir.Z,
			halfTime * 2, Ease.SineIn, Tween.LerpFloat
		).RegisterEcs();

		new Tween(fxEntt).With(
			(ref Position pos, float v) => pos.y = v,
			startPos.value.Y, startPos.value.Y + 0.5f,
			halfTime, Ease.QuartOut, Tween.LerpFloat,
			(ref Position p) => {
				onEnd?.Invoke();
				fxEntt.DeleteEntity();
			},
			AutoReverse: true
		).RegisterEcs();
	}

	internal static void ProjectileFollowFX(Entity projectile, Vector3 startPos, Entity target, Action? onEnd = null) {
		var endCb = ProjectileFXCommon(projectile, onEnd);
		float duration = 0.1f * (target.GetComponent<Position>().value - startPos).Length();
		TweenProjectile(projectile, startPos, target, duration, endCb);
	}

	internal static void ProjectilePosFX(Entity projectile, Vector3 startPos, Vector3 target, Action? onEnd = null) {
		var endCb = ProjectileFXCommon(projectile, onEnd);
		float duration = 0.1f * (target - startPos).Length();
		TweenProjectile(projectile, startPos, target, duration, endCb);
	}

	static EndCallback<Position> ProjectileFXCommon(Entity projectile, Action? onEnd = null) {
		// Animate camera
		ref var follow = ref Singleton.Camera.GetComponent<CameraFollowTarget>();
		follow.Target = projectile;
		var prevSpeed = follow.SpeedTargetFact;
		// follow.SpeedTargetFact = 0.3f;
		EndCallback<Position> endCb = (ref Position p) => {
			projectile.DeleteEntity();
			onEnd?.Invoke();
			// restore camera
			ref var follow = ref Singleton.Camera.GetComponent<CameraFollowTarget>();
			follow.Target = Singleton.Player;
			follow.SpeedTargetFact = prevSpeed;
		};
		return endCb;
	}

	static void TweenProjectile(Entity projectile, Vector3 startPos, Vector3 target, float duration, EndCallback<Position> endCb) {
		new Tween(projectile).With(
			(ref Position pos, Vector3 v) => pos.value = v,
			startPos,
			target,
			duration, Ease.Linear,
			Vector3.Lerp,
			OnEnd: endCb
		).RegisterEcs();

		TweenRotation(projectile, duration);
	}

	static void TweenProjectile(Entity projectile, Vector3 startPos, Entity target, float duration, EndCallback<Position> endCb) {
		new Tween(projectile).With(
			(ref Position pos, Vector3 v) => {
				pos.value = target.GetComponent<Position>().value - v;
				pos.value.Y = 0;
			},
			target.GetComponent<Position>().value - startPos,
			Vector3.Zero,
			duration, Ease.Linear,
			Vector3.Lerp,
			OnEnd: endCb
		).RegisterEcs();

		TweenRotation(projectile, duration);
	}

	private static void TweenRotation(Entity projectile, float duration) {
		new Tween(projectile).With(
			(ref RotationSingle rot, float f) => rot.Value = f,
			0, 360,
			duration,
			Ease.Linear, Tween.LerpFloat
		).RegisterEcs();
	}

	internal static void ExplosionFX(Entity explosion) {
		const float halfTime = 0.5f;
		explosion.AddComponent(new Scale3(0, 0, 0));
		new Tween(explosion).With(
			(ref Scale3 scale, Vector3 v) => scale.value = v,
			Vector3.Zero, Vector3.One,
			halfTime, Ease.QuartOut, Vector3.Lerp,
			AutoReverse: true,
			OnEnd: (ref Scale3 _) => explosion.DeleteEntity()
		).RegisterEcs();

		TweenRotation(explosion, halfTime * 2);
	}
}