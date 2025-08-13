using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

static class Prefabs {
	internal static void SpawnPlayer(Vec2I pos) {
		Singleton.Player = Singleton.World.CreateEntity(
			new InputReceiver(),
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new RotationSingle(0f),
			new Scale3(Config.GridSize * 0.8f, Config.GridSize * 0.8f, Config.GridSize * 0.8f),
			new Billboard(), new TextureWithSource(Assets.heroesTexture) {
				TileSize = new Vec2I(32, 32),
				TileIdx = new Vec2I(2, 2)
			},
			new ColorComp(),
			Tags.Get<Player, Character, BlocksPathing>()
		);
		// max 10 components per method...
		Singleton.Player.Add(
			new EntityName { value = "Hero" },
			new Energy() { GainPerTick = 5 },
			new VisionSource() { Range = 6 },
			new Fighter(40, new Dice(3, 2), 6),
			new Pathfinder(Singleton.Entity.GetComponent<Grid>()).Goal(pos),
			new PathMovement(),
			new Team { Value = 1 }
		);
		Singleton.Player.AddSignalHandler<DeathSignal>(Combat.PlayerDeath);
	}

	enum MonsterType {
		Skeleton,
		Banshee,
		Ogre,
		Orc,
	};

	internal static void SpawnRandomEnemy(Vec2I pos) {
		var monsterTypes = Enum.GetValues(typeof(MonsterType));
		var enemyType = (MonsterType)monsterTypes.GetValue(Random.Shared.Next(monsterTypes.Length))!;

		var entt = Singleton.World.CreateEntity(
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new RotationSingle(0f),
			new Scale3(Config.GridSize * 0.8f, Config.GridSize * 0.8f, Config.GridSize * 0.8f),
			new Billboard(),
			new ColorComp(),
			new EnemyAI(),
			new Pathfinder(Singleton.Entity.GetComponent<Grid>()),
			new PathMovement(),
			new Team { Value = 2 },
			Tags.Get<Enemy, Character, BlocksPathing>()
		);
		entt.OnTagsChanged += Movement.OnEnemyVisibilityChange;
		switch (enemyType) {
			case MonsterType.Skeleton:
				entt.Add(
					new EntityName { value = "Skeleton" },
					new TextureWithSource(Assets.monsterTexture) {
						TileSize = new Vec2I(32, 32),
						TileIdx = new Vec2I(0, 4)
					},
					new Fighter(6, new Dice(1, 2), 6),
					new Energy() { GainPerTick = 5 }
				);
				break;
			case MonsterType.Banshee:
				entt.Add(
					new EntityName { value = "Banshee" },
					new TextureWithSource(Assets.monsterTexture) {
						TileSize = new Vec2I(32, 32),
						TileIdx = new Vec2I(1, 5)
					},
					new Fighter(8, new Dice(0, 1), 9),
					new Energy() { GainPerTick = 6 }
				);
				break;
			case MonsterType.Orc:
				entt.Add(
					new EntityName { value = "Orc" },
					new TextureWithSource(Assets.monsterTexture) {
						TileSize = new Vec2I(32, 32),
						TileIdx = new Vec2I(3, 0)
					},
					new Fighter(10, new Dice(2, 2), 7),
					new Energy() { GainPerTick = 4 }
				);
				break;
			case MonsterType.Ogre:
				entt.Add(
					new EntityName { value = "Ogre" },
					new TextureWithSource(Assets.monsterTexture) {
						TileSize = new Vec2I(32, 32),
						TileIdx = new Vec2I(0, 1)
					},
					new Fighter(15, new Dice(2, 3), 9),
					new Energy() { GainPerTick = 3 }
				);
				break;
		}
		entt.AddSignalHandler<DeathSignal>(Combat.EnemyDeath);
	}

	internal static void SpawnHealingPotion(Vec2I pos) {
		var health = 10;
		Singleton.World.CreateEntity(
			new EntityName($"Healing potion +{health}HP"),
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new RotationSingle(0f),
			new Scale3(Config.GridSize * 0.8f, Config.GridSize * 0.8f, Config.GridSize * 0.8f),
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileSize = new Vec2I(32, 32),
				TileIdx = new Vec2I(1, 19)
			},
			new ColorComp(),
			new Item() { Consumable = new HealingConsumable { Amount = health } },
			Tags.Get<ItemTag>()
		);
	}
}

static class PrefabTransformations {
	internal static void PickupItem(Entity item) {
		item.Remove<GridPosition, Position, RotationSingle, Scale3>();
	}

	internal static void TurnCharacterToCorpse(Entity entity) {
		Vec2I pos = entity.GetComponent<GridPosition>().Value;
		Grid grid = Singleton.Entity.GetComponent<Grid>();
		grid.RemoveCharacter(pos);
		grid.AddOther(entity, pos);

		entity.Remove<EnemyAI, InputReceiver, Energy>(Tags.Get<BlocksPathing, Character>());
		entity.AddTag<Corpse>();
		ref var name = ref entity.GetComponent<EntityName>();
		name.value = $"Remains of {name.value}";

		if (entity.HasComponent<Billboard>()) {
			ref var bill = ref entity.GetComponent<Billboard>();
			bill.Up = new Vector3(0, 0, -1);
		}
		entity.AddComponent(new RotationSingle(Random.Shared.Next(25, 35)));
	}
}