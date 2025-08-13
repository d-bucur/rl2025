using System.Diagnostics;
using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

static class Prefabs {
	internal static Entity SpawnPlayer(Vec2I pos) {
		var startItem = PrefabTransformations.PickupItem(SpawnConfusionScroll(pos));
		SpawnConfusionScroll(pos + (1, 1));

		var entt = Singleton.World.CreateEntity(
			new InputReceiver(),
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new RotationSingle(0f),
			new Scale3(Config.GridSize * 0.8f, Config.GridSize * 0.8f, Config.GridSize * 0.8f),
			new Billboard(), new TextureWithSource(Assets.heroesTexture) {
				TileIdx = new Vec2I(2, 2)
			},
			new ColorComp(),
			Tags.Get<Player, Character, BlocksPathing>()
		);
		// max 10 components per method...
		entt.Add(
			new EntityName { value = "Hero" },
			new Energy() { GainPerTick = 5 },
			new VisionSource() { Range = 6 },
			new Fighter(40, new Dice(3, 2), 6),
			new Pathfinder(Singleton.Entity.GetComponent<Grid>()).Goal(pos),
			new PathMovement(),
			new Team { Value = 1 }
		);
		entt.AddSignalHandler<DeathSignal>(Combat.PlayerDeath);
		entt.AddRelation(new InventoryItem { Item = startItem });
		return entt;
	}

	internal enum EnemyType {
		Skeleton,
		Banshee,
		Ogre,
		Orc,
	};
	internal static Entity SpawnEnemy(Vec2I pos, EnemyType enemyType) {
		Entity entt = PrepEnemyCommon(pos);
		switch (enemyType) {
			case EnemyType.Skeleton:
				entt.Add(
					new EntityName { value = "Skeleton" },
					new TextureWithSource(Assets.monsterTexture) {
						TileIdx = new Vec2I(0, 4)
					},
					new Fighter(6, new Dice(1, 2), 6),
					new Energy() { GainPerTick = 5 }
				);
				break;
			case EnemyType.Banshee:
				entt.Add(
					new EntityName { value = "Banshee" },
					new TextureWithSource(Assets.monsterTexture) {
						TileIdx = new Vec2I(1, 5)
					},
					new Fighter(8, new Dice(0, 1), 9),
					new Energy() { GainPerTick = 6 }
				);
				break;
			case EnemyType.Orc:
				entt.Add(
					new EntityName { value = "Orc" },
					new TextureWithSource(Assets.monsterTexture) {
						TileIdx = new Vec2I(3, 0)
					},
					new Fighter(10, new Dice(2, 2), 7),
					new Energy() { GainPerTick = 4 }
				);
				break;
			case EnemyType.Ogre:
				entt.Add(
					new EntityName { value = "Ogre" },
					new TextureWithSource(Assets.monsterTexture) {
						TileIdx = new Vec2I(0, 1)
					},
					new Fighter(15, new Dice(2, 3), 9),
					new Energy() { GainPerTick = 3 }
				);
				break;
			default:
				Debug.Fail("Unhandled case");
				break;
		}
		return entt;
	}

	static Entity PrepEnemyCommon(Vec2I pos) {
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
		entt.AddSignalHandler<DeathSignal>(Combat.EnemyDeath);
		return entt;
	}

	internal enum ConsumableTypes {
		HealingPotion,
		LightningDamageScroll,
		ConfusionScroll,
	};
	internal static Entity SpawnRandomConsumable(Vec2I pos) =>
		Helpers.GetRandomEnum<ConsumableTypes>() switch {
			ConsumableTypes.HealingPotion => SpawnHealingPotion(pos, 10),
			ConsumableTypes.LightningDamageScroll => SpawnLightningScroll(pos, 10, 6),
			ConsumableTypes.ConfusionScroll => SpawnConfusionScroll(pos),
		};

	static Entity PrepConsumableCommon(Vec2I pos) {
		return Singleton.World.CreateEntity(
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new RotationSingle(0f),
			new Scale3(Config.GridSize * 0.8f, Config.GridSize * 0.8f, Config.GridSize * 0.8f),
			new ColorComp(),
			new Item() { Consumable = default }, // add default item to avoid the terrible buffer API
			Tags.Get<ItemTag>()
		);
	}

	static Entity SpawnLightningScroll(Vec2I pos, int damage, int range) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(0, 21)
			},
			new EntityName($"Lightning scroll"),
			new Item() { Consumable = new LightningDamageConsumable { Damage = damage, MaximumRange = range } }
		);
		return entt;
	}

	static Entity SpawnHealingPotion(Vec2I pos, int health) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(1, 19)
			},
			new EntityName($"Healing potion +{health}HP"),
			new Item() { Consumable = new HealingConsumable { Amount = health } }
		);
		return entt;
	}

	private static Entity SpawnConfusionScroll(Vec2I pos) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(6, 21)
			},
			new EntityName($"Confusion scroll"),
			new Item() { Consumable = new ConfusionConsumable { Turns = 5 } }
		);
		return entt;
	}
}

static class PrefabTransformations {
	internal static Entity PickupItem(Entity entt) {
		entt.Remove<GridPosition, Position, RotationSingle, Scale3>();
		return entt;
	}

	internal static Entity TurnCharacterToCorpse(Entity entt) {
		Vec2I pos = entt.GetComponent<GridPosition>().Value;
		Grid grid = Singleton.Entity.GetComponent<Grid>();
		grid.RemoveCharacter(pos);
		grid.AddOther(entt, pos);

		entt.Remove<EnemyAI, InputReceiver, Energy>(Tags.Get<BlocksPathing, Character>());
		entt.AddTag<Corpse>();
		ref var name = ref entt.GetComponent<EntityName>();
		name.value = $"Remains of {name.value}";

		if (entt.HasComponent<Billboard>()) {
			ref var bill = ref entt.GetComponent<Billboard>();
			bill.Up = new Vector3(0, 0, -1);
		}
		entt.AddComponent(new RotationSingle(Random.Shared.Next(25, 35)));
		return entt;
	}
}