using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct Character : ITag; // Entity in tutorial, but name would conflict with ECS
struct Player : ITag;
struct Enemy : ITag;
struct Corpse : ITag;
struct Name : IComponent {
	public string Value;
}
struct RotationSingle(float Value) : IComponent {
	public float Value = Value;
}

struct BlocksPathing : ITag; // walkable in tutorial
struct BlocksFOV : ITag; // transparent in tutorial

struct CameraFollowTarget() : IComponent {
	public Entity Target;
	public Vector3 Offset = default;
	public float SpeedPos = 0.03f;
	public float SpeedTargetFact = 2f;
}

class Main : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Animations.Add(new PrgressTweens());
		Singleton.Entity.Add(new Settings());

		MessageLog.Print("You descend into the dark dungeon");

		// Add camera following player
		Singleton.Camera.AddComponent(
			new CameraFollowTarget() {
				Target = Singleton.Player,
				Offset = new Vector3(0f, 10f, 10f)
			});
		UpdatePhases.Animations.Add(LambdaSystems.New((ref CameraFollowTarget follow, ref Camera cam, Entity e) => {
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