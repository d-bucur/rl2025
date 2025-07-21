using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

struct Character : ITag; // Entity in tutorial, but name would conflict with ECS
struct BlocksPathing : ITag; // walkable in tutorial
struct BlocksFOV : ITag; // transparent in tutorial

class Main : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Animations.Add(new PrgressTweens());
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