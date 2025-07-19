using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

class Main : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Animations.Add(new PrgressTweens());

		Singleton.Entity.AddComponent(new Grid(Config.MAP_SIZE_X, Config.MAP_SIZE_Y));
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