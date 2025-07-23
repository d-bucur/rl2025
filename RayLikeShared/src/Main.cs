using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct Character : ITag; // Entity in tutorial, but name would conflict with ECS
struct Player : ITag;
struct Enemy : ITag;

struct BlocksPathing : ITag; // walkable in tutorial
struct BlocksFOV : ITag; // transparent in tutorial

record struct CameraFollowTarget(Entity target, Vector3 Offset = default, float Speed = 0.03f) : IComponent;

class Main : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Animations.Add(new PrgressTweens());

		// Add camera following player
		Singleton.Camera.AddComponent(
			new CameraFollowTarget(Singleton.Player,
			new Vector3(0f, 15f, 10f)
		));
		UpdatePhases.Animations.Add(LambdaSystems.New((ref CameraFollowTarget follow, ref Camera cam, Entity e) => {
			var targetPos = follow.target.GetComponent<Position>();
			Vector3 endPos = targetPos.value + follow.Offset;
			endPos.Y = follow.Offset.Y;
			cam.Value.Position = Vector3.Lerp(cam.Value.Position, endPos, follow.Speed);
			cam.Value.Target = Vector3.Lerp(cam.Value.Target, targetPos.value, follow.Speed * 2);
		}));
	}
}

internal class PrgressTweens : QuerySystem<Tween> {
	protected override void OnUpdate() {
		Query.ForEachEntity((ref Tween tween, Entity e) => {
			if (tween.target.IsNull) {
				e.DeleteEntity();
				// not sure if this works
				Console.WriteLine("Deleting Tween because target is dead");
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