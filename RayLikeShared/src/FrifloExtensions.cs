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

	static internal LambdaSystem2<T1, T2> New<T1, T2>(ForEachEntity<T1, T2> lambda) where T1 : struct, IComponent where T2 : struct, IComponent {
		return new LambdaSystem2<T1, T2>(lambda);
	}

	internal class LambdaSystem2<T1, T2>(ForEachEntity<T1, T2> lambda) : QuerySystem<T1, T2> where T1 : struct, IComponent where T2 : struct, IComponent {
		protected override void OnUpdate() {
			Query.ForEachEntity(lambda);
		}
	}
}

interface IModule {
	public void Init(EntityStore world);
}

class Singleton {
	internal static Entity Entity;
	internal static Entity Camera;
	internal static Entity Player;
	internal static EntityStore World;

	// Old method breaking wasm. Need to register to AOT maybe?
	// struct SingletonEntity() : IIndexedComponent<int> {
	// 	int Id = 0;
	// 	public int GetIndexedValue() => Id;
	// }

	internal static void Init(EntityStore world) {
		Entity = world.CreateEntity();
		World = world;
	}
}