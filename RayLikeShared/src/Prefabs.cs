using System.Diagnostics;
using System.Numerics;
using Friflo.Engine.ECS;

namespace RayLikeShared;

static class Prefabs {
	internal enum EnemyType {
		Skeleton,
		Banshee,
		Ogre,
		Orc,
		Malthael,
		Dragon,
	};

	internal enum ConsumableType {
		HealingPotion,
		LightningDamageScroll,
		ConfusionScroll,
		FireballScroll,
		RagePotion,
		NecromancyScroll,
	};

	internal static Entity SpawnPlayer(Vec2I pos) {
		var player = Singleton.World.CreateEntity(
			new InputReceiver(),
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new RotationSingle(0f),
			new Scale3(1, 1, 1),
			new Billboard(), new TextureWithSource(Assets.heroesTexture) {
				TileIdx = new Vec2I(2, 2)
			},
			new ColorComp(),
			Tags.Get<Player, Character, BlocksPathing>()
		);
		// max 10 components per method...
		player.Add(
			new EntityName { value = "Hero" },
			new Energy() { GainPerTick = 5 },
			new VisionSource() { Range = 6 },
			new Fighter(40, new Dice(3, 2), 6),
			new Pathfinder(Singleton.Get<Grid>()).Goal(pos),
			new PathMovement(),
			new Team { Value = 1 }
		);
		player.AddSignalHandler<DeathSignal>(Combat.PlayerDeath);
		return player;
	}

	internal static void SpawnStartingItems(Vec2I pos, Entity entt) {
		// entt.AddRelation(new InventoryItem { Item = PrefabTransformations.PickupItem(SpawnFireballScroll(pos)) });
		// entt.AddRelation(new InventoryItem { Item = PrefabTransformations.PickupItem(SpawnLightningScroll(pos)) });
		// entt.AddRelation(new InventoryItem { Item = PrefabTransformations.PickupItem(SpawnConfusionScroll(pos)) });
		// entt.AddRelation(new InventoryItem { Item = PrefabTransformations.PickupItem(SpawnHealingPotion(pos)) });
		// entt.AddRelation(new InventoryItem { Item = PrefabTransformations.PickupItem(SpawnRagePotion(pos)) });
		// entt.AddRelation(new InventoryItem { Item = PrefabTransformations.PickupItem(SpawnNecromancyScroll(pos)) });
		entt.AddRelation(new InventoryItem { Item = PrefabTransformations.PickupItem(SpawnRandomConsumable(pos)) });
		// SpawnConfusionScroll(pos + (1, 1));
		// SpawnLightningScroll(pos + (-1, 1));
		// SpawnFireballScroll(pos + (-1, -1));
		// SpawnHealingPotion(pos + (1, -1));
		// SpawnNecromancyScroll(pos + (1, -1));
	}

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
			case EnemyType.Malthael:
				entt.Add(
					new EntityName { value = "Malthael" },
					new TextureWithSource(Assets.monsterTexture) {
						TileIdx = new Vec2I(0, 11)
					},
					new Fighter(15, new Dice(2, 2), 13),
					new Energy() { GainPerTick = 4 }
				);
				break;
			case EnemyType.Dragon:
				entt.Add(
					new EntityName { value = "Dragon" },
					new TextureWithSource(Assets.monsterTexture) {
						TileIdx = new Vec2I(2, 8)
					},
					new Fighter(30, new Dice(3, 3), 15),
					new Energy() { GainPerTick = 2 }
				);
				break;
			default:
				Debug.Fail("Unhandled case");
				break;
		}
		EnemyAIModule.AddEnemyAI(entt);
		return entt;
	}

	static Entity PrepEnemyCommon(Vec2I pos) {
		var entt = Singleton.World.CreateEntity(
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new RotationSingle(0f),
			new Scale3(1, 1, 1),
			new Billboard(),
			new ColorComp(),
			new Pathfinder(Singleton.Get<Grid>()),
			new PathMovement(),
			new Team { Value = 2 },
			Tags.Get<Enemy, Character, BlocksPathing>()
		);
		entt.OnTagsChanged += Movement.OnEnemyVisibilityChange;
		entt.AddSignalHandler<DeathSignal>(Combat.EnemyDeath);
		return entt;
	}

	internal static Entity SpawnRandomConsumable(Vec2I pos) =>
		Helpers.GetRandomEnum<ConsumableType>() switch {
			ConsumableType.HealingPotion => SpawnHealingPotion(pos),
			ConsumableType.LightningDamageScroll => SpawnLightningScroll(pos),
			ConsumableType.ConfusionScroll => SpawnConfusionScroll(pos),
			ConsumableType.FireballScroll => SpawnFireballScroll(pos),
			ConsumableType.RagePotion => SpawnRagePotion(pos),
			ConsumableType.NecromancyScroll => SpawnNecromancyScroll(pos),
		};

	static Entity PrepConsumableCommon(Vec2I pos) {
		return Singleton.World.CreateEntity(
			new GridPosition(pos.X, pos.Y),
			new Position(pos.X, 0, pos.Y),
			new RotationSingle(0f),
			new Scale3(1, 1, 1),
			new ColorComp(),
			new Item() { Consumable = default }, // add default item to avoid the terrible buffer API
			Tags.Get<ItemTag>()
		);
	}

	static Entity SpawnLightningScroll(Vec2I pos, int damage = 10, int range = 10) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(1, 21)
			},
			new EntityName($"Lightning scroll"),
			new Item() { Consumable = new LightningDamageConsumable { Damage = damage, MaximumRange = range } }
		);
		return entt;
	}

	static Entity SpawnHealingPotion(Vec2I pos, int health = 15) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(1, 19)
			},
			new EntityName($"Healing potion +3/4HP"),
			new Item() { Consumable = new HealingConsumable { Amount = health } }
		);
		return entt;
	}

	static int DefaultRageGain(Energy e) => 15;
	static Entity SpawnRagePotion(Vec2I pos, Func<Energy, int>? gainSetter = null) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(4, 20)
			},
			new EntityName($"Rage"),
			new Item() { Consumable = new RageConsumable { GainCalc = gainSetter ?? DefaultRageGain, Duration = 6 } }
		);
		return entt;
	}

	private static Entity SpawnConfusionScroll(Vec2I pos) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(0, 21)
			},
			new EntityName($"Confusion scroll"),
			new Item() { Consumable = new ConfusionConsumable { Turns = 5 } }
		);
		return entt;
	}

	private static Entity SpawnFireballScroll(Vec2I pos) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(2, 21)
			},
			new EntityName($"Fireball scroll"),
			new Item() { Consumable = new FireballConsumable { Damage = 5, Range = 3 } }
		);
		return entt;
	}

	private static Entity SpawnNecromancyScroll(Vec2I pos) {
		Entity entt = PrepConsumableCommon(pos);
		entt.Add(
			new Billboard(), new TextureWithSource(Assets.itemsTexture) {
				TileIdx = new Vec2I(3, 21)
			},
			new EntityName($"Necromancy scroll"),
			new Item() { Consumable = new NecromancyConsumable { } }
		);
		return entt;
	}

	internal static Entity SpawnProjectile(Vec2I pos, ConsumableType consumable, Vector3 offset = default) {
		return Singleton.World.CreateEntity(
			new Position(pos.X + offset.X, offset.Y, pos.Y + offset.Z),
			new RotationSingle(),
			new Scale3(1, 1, 1),
			new ColorComp(),
			new Billboard() { Origin = new(0.5f, 0.5f), Offset = new(0, 0, 0.1f) }, // this should render the sprite on top, but it doesn't...
			new TextureWithSource(Assets.itemsTexture) {
				TileIdx = consumable switch {
					// TODO use vec map for this
					ConsumableType.LightningDamageScroll => new Vec2I(1, 21),
					ConsumableType.FireballScroll => new Vec2I(2, 21),
					ConsumableType.RagePotion => new Vec2I(4, 20),
					ConsumableType.ConfusionScroll => new Vec2I(0, 21),
				}
			},
			new EntityName("Projectile"),
			Tags.Get<Projectile, IsVisible>()
		);
	}
}

static class PrefabTransformations {
	internal static Entity PickupItem(Entity entt) {
		entt.Remove<GridPosition, Position, RotationSingle, Scale3>();
		return entt;
	}

	internal static Entity TurnCharacterToCorpse(Entity entt) {
		Vec2I pos = entt.GetComponent<GridPosition>().Value;
		Grid grid = Singleton.Get<Grid>();
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

	// Only works for enemies
	internal static void ResurrectCorpse(Entity entt, Vec2I mouseTarget) {
		entt.RemoveTag<Corpse>();
		entt.RemoveComponent<GridPosition>(); // not ideal, but this retriggers add to the grid
		entt.Add(
			new GridPosition() { Value = mouseTarget },
			new EnemyAI() { isOnPlayerSide = true },
			new Billboard(),
			new RotationSingle(),
			new Energy() { GainPerTick = 4 },
			new Team() { Value = 1 },
			Tags.Get<BlocksPathing, Character>()
		);
		ref var f = ref entt.GetComponent<Fighter>();
		f.HP = f.MaxHP;

		ref var name = ref entt.GetComponent<EntityName>();
		name.value = $"Reanimated {name.value.Replace("Remains of", "")}";
	}
}