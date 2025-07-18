using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

class LambdaSystems {
	static internal LambdaSystem1<T1> New<T1>(ForEachEntity<T1> lambda) where T1 : struct, IComponent {
		return new LambdaSystem1<T1>(lambda);
	}

	internal class LambdaSystem1<T1>(ForEachEntity<T1> lambda) : QuerySystem<T1> where T1 : struct, IComponent {
		protected override void OnUpdate() {
			Query.ForEachEntity(lambda);
		}
	}
}

internal static class Helpers {
    // Easy way to do this with an implicit cast?
    internal static Vector3 ToVec3(Position p) => new Vector3(p.x, p.y, p.z);
}