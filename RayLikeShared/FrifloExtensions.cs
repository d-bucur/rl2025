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
	internal static Vector3 ToVec3(this Position p) => new Vector3(p.x, p.y, p.z);
	internal static Vector3 ToVec3(this Scale3 p) => new Vector3(p.x, p.y, p.z);
}

interface IModule {
	public void Init(EntityStore world);
}

class Singleton {
	internal static Entity Entity;

	struct SingletonEntity() : IIndexedComponent<int> {
		int Id = 0;
		public int GetIndexedValue() => Id;
	}

	internal static void Init(EntityStore world) {
		Entity = world.CreateEntity(new SingletonEntity());
	}

	// these are kind of useless?
	private static void Set<T>(T t) where T : struct, IComponent {
		Entity.AddComponent(t);
	}

	private static ref T Get<T>() where T : struct, IComponent {
		return ref Entity.GetComponent<T>();
	}
}