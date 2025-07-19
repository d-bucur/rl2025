using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

class Main : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Animations.Add(new PrgressTweens());
	}
}

internal class PrgressTweens : QuerySystem<Tween> {
	protected override void OnUpdate() {
		Query.ForEachEntity((ref Tween tween, Entity e) => {
			// TODO
			// if (!tween.target.IsAlive()) {
			// 	e.Destruct();
			// 	return;
			// }
			tween.Tick(Tick.deltaTime);
			if (tween.IsFinished()) {
				tween.Cleanup();
				// e.DeleteEntity();
			}
		});
	}
}