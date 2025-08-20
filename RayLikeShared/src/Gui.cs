using System.Numerics;
using BehaviorTree;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

struct MessageLog() : IComponent {
	internal struct Message {
		public string Text;
		public Color Color;
		public double Time;
	}
	public List<Message> Messages = new();
	public int MaxCount = 200;
	public int DisplayCount = 4;
	private bool replaceNext = false;

	void Add(string message, Color? color = null) {
		if (replaceNext) {
			replaceNext = false;
			return;
		}
		Messages.Add(new Message {
			Text = message,
			Color = color ?? Color.White,
			Time = Raylib.GetTime()
		});
	}

	internal static void Print(string message, Color? color = null) {
		Singleton.Get<MessageLog>().Add(message, color);
	}

	internal static void ReplaceLast(string message, Color? color = null) {
		RemoveLast();
		Singleton.Get<MessageLog>().Add(message, color);
	}

	internal static void ReplaceNext(string message, Color? color = null) {
		ref var messages = ref Singleton.Get<MessageLog>();
		messages.Add(message, color);
		messages.replaceNext = true;
	}

	internal static void RemoveLast() {
		ref List<Message> messages = ref Singleton.Get<MessageLog>().Messages;
		if (messages.Count > 0) messages.RemoveAt(messages.Count - 1);
	}
}

struct TextFX : IComponent {
	public string Text;
	public RenderTexture2D RenderTex;
}

class GuiModule : IModule {
	public void Init(EntityStore world) {
		Singleton.Entity.AddComponent(new MessageLog());

		RenderPhases.Render.Add(new RenderMinimap());
		RenderPhases.Render.Add(new RenderInventory());
		RenderPhases.Render.Add(new RenderTurnOrder());
		RenderPhases.Render.Add(new RenderHealth());
		RenderPhases.Render.Add(new RenderGameOver());
		RenderPhases.Render.Add(new RenderMessageLog());
		RenderPhases.Render.Add(new MouseSelect()); // should be in input phase
		RenderPhases.Render.Add(new RenderDamageFx());
		RenderPhases.Render.Add(new DebugBehaviorTree());
	}
}

static class GUI {
	internal static void RenderText(string text, int posX, int posY, int FontSize, int offset = 5, Color? fgColor = null, Color? bgColor = null) {
		Raylib.DrawText(
			text,
			posX + offset,
			posY + offset,
			FontSize, bgColor ?? Raylib.Fade(Color.Black, 0.75f)
		);
		Raylib.DrawText(
			text,
			posX,
			posY,
			FontSize, fgColor ?? Color.White
		);
	}

	// Should move this somewhere else
	internal static void SpawnDamageFx(int damage, Vector3 originPos, Color color, Vector3 dir) {
		const int Size = 20;
		// TODO use texture pool
		var renderTex = Raylib.LoadRenderTexture(Size, Size);
		// Draw text to texture
		Raylib.BeginTextureMode(renderTex);
		Raylib.ClearBackground(new Color(0, 0, 0, 0));
		RenderText($"{damage}", 0, 0, Size, 1, color);
		Raylib.EndTextureMode();

		Vector3 fxPos = originPos + new Vector3(0, 1f, 0);
		Position startPos = new(fxPos.X, fxPos.Y, fxPos.Z);
		Scale3 startScale = new(0, 0, 0);
		var fxEntt = Singleton.World.CreateEntity(
			new TextFX() { Text = $"{damage}", RenderTex = renderTex },
			new Billboard(),
			startPos,
			startScale
		);
		Animations.DamageFx(fxEntt, startPos, startScale, dir,
			() => Raylib.UnloadRenderTexture(fxEntt.GetComponent<TextFX>().RenderTex)
		);
	}
}

file static class GUIValues {
	public const int Padding = 10;
	public const int TextHeight = 20;
	public const int LineHeight = 25;
}

file class RenderHealth : QuerySystem<Fighter> {
	public const int HealthHeight = 30;
	public RenderHealth() => Filter.AllTags(Tags.Get<Player>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref Fighter fighter, Entity playerEntt) => {
			const int width = 300;
			Rectangle r = RenderInventory.GetRect();
			var rect = new Rectangle(r.X, r.Y - HealthHeight - GUIValues.Padding, r.Width, HealthHeight);
			Raylib.DrawRectangleRec(rect, Color.Red);
			rect.Width = (float)fighter.HP / fighter.MaxHP * width;
			Raylib.DrawRectangleRec(rect, Color.Green);
			GUI.RenderText($"HP {fighter.HP}/{fighter.MaxHP}", (int)rect.X + 90, (int)rect.Y + 3, 25, 3);
		});
	}
}

file class RenderMessageLog : QuerySystem<MessageLog> {
	private const int MessageDuration = 5;
	protected override void OnUpdate() {
		Query.ForEachEntity((ref MessageLog log, Entity entt) => {
			int pos = GUIValues.Padding;
			for (int i = Math.Max(0, log.Messages.Count - log.DisplayCount); i < log.Messages.Count; i++) {
				var message = log.Messages[i];
				double timeDiff = Raylib.GetTime() - message.Time;
				// Only draw recent messages
				if (timeDiff < MessageDuration) {
					// Fade out effect
					float ratio = (float)(timeDiff / MessageDuration);
					float alpha = Ease.QuartOut(1 - ratio);
					Color color = Raylib.Fade(message.Color, alpha);
					Raylib.DrawText(message.Text, GUIValues.Padding, pos, GUIValues.TextHeight, color);
					pos += GUIValues.LineHeight;
				}
			}
		});
	}
}

file class RenderGameOver : QuerySystem<EntityName> {
	public RenderGameOver() => Filter.AllTags(Tags.Get<Player, Corpse>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref EntityName name, Entity playerEntt) => {
			GUI.RenderText(
				"You're dead!",
				Raylib.GetScreenWidth() / 2 - 140,
				Raylib.GetScreenHeight() / 2 + 100,
				50
			);
		});
	}
}

// Optimization: could only redraw on change
file class RenderMinimap : QuerySystem {
	public RenderMinimap() => Filter.AllTags(Tags.Get<Player>());
	Image MinimapImage;
	Texture2D MinimapTexture;
	private static bool explorationHack;

	protected override void OnAddStore(EntityStore store) {
		MinimapImage = Raylib.GenImageColor(Config.MapSizeX, Config.MapSizeY, Palette.Transparent);
		MinimapTexture = Raylib.LoadTextureFromImage(MinimapImage);
	}

	protected override unsafe void OnUpdate() {
		if (!Singleton.Get<Settings>().MinimapEnabled)
			return;
		explorationHack = Singleton.Get<Settings>().ExplorationHack;

		var grid = Singleton.Get<Grid>();
		for (int x = 0; x < grid.Tile.GetLength(0); x++) {
			for (int y = 0; y < grid.Tile.GetLength(1); y++) {
				Raylib.ImageDrawPixel(ref MinimapImage, x, y, GetColor(grid, x, y));
			}
		}
		Raylib.UpdateTexture(MinimapTexture, Raylib.LoadImageColors(MinimapImage));
		const int SCALE = 5;
		Raylib.DrawTextureEx(MinimapTexture,
			new Vector2(Raylib.GetScreenWidth() - MinimapImage.Width * SCALE, 0),
			0,
			SCALE,
			Raylib.Fade(Color.White, 0.5f)
		);
	}

	static unsafe Color GetColor(Grid grid, int x, int y) {
		Entity tile = grid.Tile[x, y];
		if (!tile.Tags.Has<IsExplored>())
			return Palette.Transparent;
		var color = grid.Tile[x, y].GetComponent<ColorComp>().Value;
		if (grid.CheckOthers<ItemTag>((x, y))) color = Color.Yellow;
		Entity character = grid.Character[x, y];
		if (!character.IsNull) {
			if (character.Tags.Has<Player>())
				color = Color.Green;
			if (character.Tags.Has<Enemy>() && (tile.Tags.Has<IsVisible>() || explorationHack))
				color = Color.Red;
		}
		return color;
	}
}

// A lot of cross cutting concerns here: input, rendering and gui. Should break up
file class MouseSelect : QuerySystem {
	public MouseSelect() => Filter.AllTags(Tags.Get<Player>());
	List<string> InspectStrings = new();

	protected override void OnUpdate() {
		var mouseTarget = Singleton.Get<MouseTarget>();
		if (!mouseTarget.Value.HasValue) return;
		var mousePosI = mouseTarget.Value.Value;
		ref Grid grid = ref Singleton.Get<Grid>();

		var isTileVisible = grid.CheckTile<IsVisible>(mousePosI);
		var charAtPos = grid.Character[mousePosI.X, mousePosI.Y];

		// Draw outline at tile and path to destination
		// Could separate this part into pathfinding module?
		if (grid.CheckTile<IsExplored>(mousePosI)) {
			Camera3D camera = Singleton.Camera.GetComponent<Camera>().Value;
			Raylib.BeginMode3D(camera);
			Color color = (!charAtPos.IsNull && isTileVisible) ? Color.Red : Color.Green;
			Raylib.DrawCubeWiresV(
				mousePosI.ToWorldPos() - MouseTarget.tileOffset,
				new Vector3(1.1f, 1f, 1.1f),
				Raylib.Fade(color, 0.3f));

			// Draw target tiles
			DrawTargetTiles();

			DrawPathTo(mousePosI, color);
			Raylib.EndMode3D();
		}

		// Get all entities at position
		InspectStrings.Clear();
		if (!charAtPos.IsNull && isTileVisible) {
			string name = charAtPos.GetComponent<EntityName>().value;
			var fighter = charAtPos.GetComponent<Fighter>();
			InspectStrings.Add($"{name} {fighter.HP}/{fighter.MaxHP} HP");
		}

		if (isTileVisible) {
			var others = grid.Others[mousePosI.X, mousePosI.Y];
			foreach (var other in others?.Value ?? [])
				InspectStrings.Add($"{other.GetComponent<EntityName>().value}");
		}

		// Render text of objects at position
		for (int i = 0; i < InspectStrings.Count; i++) {
			// align with turn order
			int posY = Raylib.GetScreenHeight()
				- RenderTurnOrder.AncorAbove.Y
				- GUIValues.TextHeight
				- GUIValues.Padding
				- i * GUIValues.LineHeight;
			GUI.RenderText(
				InspectStrings[i],
				RenderTurnOrder.AncorAbove.X + GUIValues.Padding,
				posY,
				GUIValues.TextHeight,
				3
			);
		}
	}

	private static void DrawTargetTiles() {
		for (int i = 0; i < Config.InventoryLimit; i++) {
			if (!Raylib.IsKeyDown(i + KeyboardKey.One)) continue;

			var items = Singleton.Player.GetRelations<InventoryItem>();
			if (items.Length <= i) continue;
			Vec2I playerPos = Singleton.Player.GetComponent<GridPosition>().Value;
			foreach (var pos in items[i].Item.GetComponent<Item>().Consumable.AffectedTiles(playerPos)) {
				Raylib.DrawCubeWiresV(
					pos.ToWorldPos() - MouseTarget.tileOffset,
					new Vector3(1f, 1f, 1f),
					Raylib.Fade(Color.Orange, 0.3f));
			}
		}
	}

	private void DrawPathTo(Vec2I posI, Color color) {
		ref var pathfinder = ref Singleton.Player.GetComponent<Pathfinder>();
		if (Raylib.IsMouseButtonPressed(MouseButton.Right))
			pathfinder.Reset();
		var path = pathfinder
			.Goal(Singleton.Player.GetComponent<GridPosition>().Value)
			.PathFrom(posI)
			.Reverse().Skip(1).ToList(); // garbage
		foreach (var p in path) {
			Raylib.DrawSphere(p.ToWorldPos() - new Vector3(0.5f, 0, 0.5f), 0.15f,
				Raylib.Fade(color, 0.3f));
		}
		if (Raylib.IsMouseButtonReleased(MouseButton.Left)) {
			// move player to target destination
			ref var movement = ref Singleton.Player.GetComponent<PathMovement>();
			if (!Singleton.Get<Grid>().CheckTile<BlocksPathing>(posI)) movement.NewDestination(posI, path);
		}
	}
}

file class RenderDamageFx : QuerySystem<TextFX, Billboard, Position, Scale3> {
	protected override void OnUpdate() {
		Camera camera = Singleton.Camera.GetComponent<Camera>();
		Raylib.BeginShaderMode(Assets.billboardShader);
		Raylib.BeginMode3D(camera.Value);
		Query.ForEachEntity((ref TextFX fx, ref Billboard bill, ref Position pos, ref Scale3 scale, Entity entt) => {
			Raylib.DrawBillboardPro(
				camera.Value,
				fx.RenderTex.Texture,
				// height has to be inverted because weird opengl stuff
				new Rectangle(0, fx.RenderTex.Texture.Height, fx.RenderTex.Texture.Width, -fx.RenderTex.Texture.Height),
				pos.value, camera.GetUpVec(),
				new Vector2(scale.x, scale.z), new Vector2(0.5f, 0), 0, Color.Orange
			);
		});
		Raylib.EndMode3D();
		Raylib.EndShaderMode();
	}
}

file class RenderInventory : QuerySystem {
	static int ItemSize = 64;
	internal static Vec2I AncorAbove = new(0, ItemSize);
	public RenderInventory() => Filter.AllTags(Tags.Get<Player>());

	internal static Rectangle GetRect() => new Rectangle(Raylib.GetScreenWidth() - ItemSize * Config.InventoryLimit, Raylib.GetScreenHeight() - ItemSize, ItemSize * Config.InventoryLimit, ItemSize);
	protected override void OnUpdate() {
		var inventory = Singleton.Player.GetRelations<InventoryItem>();
		var anchor = new Vector2(Raylib.GetScreenWidth(), Raylib.GetScreenHeight())
			- new Vector2(Config.InventoryLimit * ItemSize, ItemSize);

		for (int i = 0; i < Config.InventoryLimit; i++) {
			var tileStart = new Vector2(anchor.X + i * ItemSize, anchor.Y);
			var tileRect = new Rectangle(tileStart, ItemSize, ItemSize);
			Raylib.DrawRectangleV(tileStart, new Vector2(ItemSize), Raylib.Fade(Color.DarkGray, 0.3f));
			Raylib.DrawRectangleLinesEx(tileRect, 5, Raylib.Fade(Color.LightGray, 0.3f));

			if (i < inventory.Length) {
				TextureWithSource itemTexture = inventory[i].Item.GetComponent<TextureWithSource>();
				Raylib.DrawTexturePro(
					itemTexture.Texture,
					itemTexture.Source,
					tileRect,
					Vector2.Zero,
					0,
					Color.White
				);
			}
			Raylib.DrawText($"{i + 1}", (int)tileStart.X + 5, (int)tileStart.Y + 5, 20, Color.White);
		}
	}
}

file class RenderTurnOrder : QuerySystem {
	static int ItemSize = 64;
	internal static Vec2I AncorAbove = new(0, ItemSize);
	const int TileCount = 6;

	public RenderTurnOrder() => Filter.AllTags(Tags.Get<Player>());
	protected override void OnUpdate() {
		// Can cache in turns management
		var anchor = new Vector2(0, Raylib.GetScreenHeight() - ItemSize);
		var i = 0;
		// TODO SimTurns is pretty heavy. Should cache result
		foreach (var (entt, energy) in TurnsManagement.SimTurns(TileCount)) {
			var itemTexture = entt.GetComponent<TextureWithSource>();
			Vector2 tileStart = anchor + new Vector2(i * ItemSize, 0);
			Rectangle tileRect = new(tileStart, ItemSize, ItemSize);
			Raylib.DrawRectangleV(tileStart, new Vector2(ItemSize), Raylib.Fade(Color.DarkGray, 0.3f));
			if (Singleton.Get<MouseTarget>().Value is Vec2I target) {
				var targetChar = Singleton.Get<Grid>().Character[target.X, target.Y];
				if (!targetChar.IsNull && targetChar == entt)
					Raylib.DrawRectangleLinesEx(tileRect, 5, Raylib.Fade(Color.SkyBlue, 0.3f));
			}
			Raylib.DrawTexturePro(
				itemTexture.Texture,
				itemTexture.Source,
				tileRect,
				Vector2.Zero,
				0,
				Color.White
			);
			// Debug turn index
			// Raylib.DrawText($"{energy}", (int)tileStart.X, (int)tileStart.Y, 20, Color.White);
			i++;
		}
		// Turns text is kind of redundant
		// Raylib.DrawText("Turns",
		// 	(int)anchor.X + GUIValues.Padding,
		// 	(int)anchor.Y - GUIValues.TextHeight - GUIValues.Padding,
		// 	GUIValues.TextHeight,
		// 	Color.White
		// );
	}
}

file class DebugBehaviorTree : QuerySystem<EnemyAI> {
	Entity? Displayed;
	protected override void OnUpdate() {
		if (!Singleton.Get<Settings>().DebugAI) return;

		Entity? targeted = Singleton.Get<MouseTarget>().Entity;
		if (targeted.HasValue && targeted.Value.HasComponent<EnemyAI>()) Displayed = targeted;
		var entt = Displayed ?? Query.Entities.First(); // TODO can crash when no enemies

		if (!entt.HasComponent<EnemyAI>()) {
			Displayed = null;
			return;
		}
		ref var ai = ref entt.GetComponent<EnemyAI>();
		int nesting = 0;
		Dictionary<(string, Type), int> open = new();
		int y = 0;
		for (int i = 0; i < ai.LastExecution.Count; i++) {
			var newLog = ai.LastExecution[i];
			var (name, log, status, type) = newLog;
			if (log == ExecutionLogEnum.End) nesting--;
			if (log == ExecutionLogEnum.Begin) {
				open[(name, type)] = y++;
			}
			else {
				var usedY = open[(name, type)];
				string text = $"{type.Name} {name}";
				Raylib.DrawText(
					text,
					10 + nesting * 20,
					100 + usedY * 20,
					20,
					status switch {
						BTStatus.Success => Color.Green,
						BTStatus.Running => Color.Yellow,
						BTStatus.Failure => Color.Red,
						null => Color.White,
					}
				);
			}
			if (log == ExecutionLogEnum.Begin) nesting++;
		}
	}
}