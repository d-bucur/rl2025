using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct Character : ITag; // Entity in tutorial, but name would conflict with ECS
struct Player : ITag;
struct Enemy : ITag;
struct Corpse : ITag;
struct Projectile : ITag;
struct InputEnabled : ITag;

struct RotationSingle(float Value = 0f) : IComponent {
	public float Value = Value;
}

struct BlocksPathing : ITag; // walkable in tutorial
struct BlocksFOV : ITag; // transparent in tutorial
struct Walkable : ITag;
struct AboveGround : ITag; // a generic ite that exists above ground like a chest, item, door, exit etc. Should find better name
struct Stairs : ITag;

struct CameraFollowTarget() : IComponent {
	public Entity Target;
	public Vector3 Offset;
	public float Angle;
	public float Distance;
	public float Height;
	public float SpeedPos = 0.03f;
	public float SpeedTargetFact = 2f;
}

class Main : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Animations.Add(new PrgressTweens());
		Singleton.Entity.Add(new Settings());
		Prefabs.MakePlayerChoices();
		UpdatePhases.Animations.Add(LambdaSystems.New((ref CameraFollowTarget follow, ref Camera cam, Entity e) => {
			if (follow.Target.IsNull) return;
			var targetPos = follow.Target.GetComponent<Position>();
			Vector3 endPos = targetPos.value + follow.Offset;
			endPos.Y = follow.Offset.Y;
			cam.Value.Position = Vector3.Lerp(cam.Value.Position, endPos, follow.SpeedPos);
			cam.Value.Target = Vector3.Lerp(cam.Value.Target, targetPos.value, follow.SpeedPos * follow.SpeedTargetFact);
		}));
	}
}

file class PrgressTweens : QuerySystem<Tween> {
	protected override void OnUpdate() {
		// hack to allow callbacks to make structural changes. Should be fine as longs as they don't change Tweens
		Query.ThrowOnStructuralChange = false;

		Query.ForEachEntity((ref Tween tween, Entity e) => {
			if (tween.target.IsNull) {
				e.DeleteEntity();
				return;
			}
			tween.Tick(Tick.deltaTime);
			if (tween.IsFinished()) {
				tween.Cleanup();
				e.DeleteEntity();
			}
		});
	}
}