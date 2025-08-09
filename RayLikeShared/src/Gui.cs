using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

struct MessageLog() : IComponent {
	internal struct Message {
		public string Text;
		public Color Color;
	}
	public List<Message> Messages = new();
	public int MaxCount = 200;
	public int DisplayCount = 3;

	public void Add(string message, Color? color = null) {
		Messages.Add(new Message {
			Text = message,
			Color = color ?? Color.White,
		});
	}

	internal static void Print(string message, Color? color = null) {
		Singleton.Entity.GetComponent<MessageLog>().Add(message, color);
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
		RenderPhases.Render.Add(new RenderHealth());
		RenderPhases.Render.Add(new RenderGameOver());
		RenderPhases.Render.Add(new RenderMessageLog());
		RenderPhases.Render.Add(new MouseSelect()); // should be in input phase
		RenderPhases.Render.Add(new RenderDamageFx());
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

	internal static void SpawnDamageFx(int damage, Position originPos, Color color, Vector3 dir) {
		const int Size = 20;
		var renderTex = Raylib.LoadRenderTexture(Size, Size);
		// TODO texture render not working in web version. Same with pathfinding debug
		// Draw text to texture
		Raylib.BeginTextureMode(renderTex);
		Raylib.ClearBackground(new Color(0, 0, 0, 0));
		RenderText($"{damage}", 0, 0, Size, 1, color);
		Raylib.EndTextureMode();

		Vector3 fxPos = originPos.value + new Vector3(0, 1f, 0);
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
	public const int HealthHeight = 30;
	public const int Padding = 10;
	public const int TextHeight = 20;
	public const int LineHeight = 25;
}

file class RenderHealth : QuerySystem<Fighter> {
	public RenderHealth() => Filter.AllTags(Tags.Get<Player>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref Fighter fighter, Entity playerEntt) => {
			const int width = 300;
			var rect = new Rectangle(
				GUIValues.Padding,
				// Raylib.GetScreenHeight() - height - GUIValues.Padding,
				GUIValues.Padding,
				width,
				GUIValues.HealthHeight
			);
			Raylib.DrawRectangleRec(rect, Color.Red);
			rect.Width = (float)fighter.HP / fighter.MaxHP * width;
			Raylib.DrawRectangleRec(rect, Color.Green);
			GUI.RenderText($"HP {fighter.HP}/{fighter.MaxHP}", (int)rect.X + 90, (int)rect.Y + 3, 25, 3);
		});
	}
}

file class RenderMessageLog : QuerySystem<MessageLog> {
	protected override void OnUpdate() {
		Query.ForEachEntity((ref MessageLog log, Entity entt) => {
			int pos = GUIValues.HealthHeight + GUIValues.Padding + GUIValues.LineHeight;
			for (int i = Math.Max(0, log.Messages.Count - log.DisplayCount); i < log.Messages.Count; i++) {
				var message = log.Messages[i];
				Raylib.DrawText(message.Text, GUIValues.Padding, pos, GUIValues.TextHeight, message.Color);
				pos += GUIValues.LineHeight;
			}
		});
	}
}

file class RenderGameOver : QuerySystem<Name> {
	public RenderGameOver() => Filter.AllTags(Tags.Get<Player, Corpse>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref Name name, Entity playerEntt) => {
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
	Image MinimapImage;
	Texture2D MinimapTexture;
	private static bool explorationHack;

	protected override void OnAddStore(EntityStore store) {
		MinimapImage = Raylib.GenImageColor(Config.MAP_SIZE_X, Config.MAP_SIZE_Y, Palette.Transparent);
		MinimapTexture = Raylib.LoadTextureFromImage(MinimapImage);
	}

	protected override unsafe void OnUpdate() {
		if (!Singleton.Entity.GetComponent<Settings>().MinimapEnabled)
			return;
		explorationHack = Singleton.Entity.GetComponent<Settings>().ExplorationHack;

		var grid = Singleton.Entity.GetComponent<Grid>();
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

// TODO Move to input module?
file class MouseSelect : QuerySystem {
	List<string> InspectStrings = new();

	protected override void OnUpdate() {
		Camera3D camera = Singleton.Camera.GetComponent<Camera>().Value;
		var ray = Raylib.GetScreenToWorldRay(Raylib.GetMousePosition(), camera);

		// Avoid div0
		if (Math.Abs(ray.Direction.Y) < 1e-6)
			return;

		// Get plane intersection
		Vector3 tileOffset = new(0.5f, -0.5f, 0.5f);
		float t = -ray.Position.Y / ray.Direction.Y;
		Vector3 intersection = ray.Position + t * ray.Direction + tileOffset;
		Vec2I mousePosI = Vec2I.FromWorldPos(intersection);
		Grid grid = Singleton.Entity.GetComponent<Grid>();

		if (!grid.IsInside(mousePosI))
			return;

		// Draw outline at tile
		Raylib.BeginMode3D(camera);
		Raylib.DrawCubeWiresV(
			mousePosI.ToWorldPos() - tileOffset,
			new Vector3(1.1f, 1f, 1.1f),
			Raylib.Fade(Color.Red, 0.3f));
		if (grid.CheckTile<IsExplored>(mousePosI))
			PathTo(mousePosI);
		Raylib.EndMode3D();


		// Get all entities at position
		InspectStrings.Clear();
		var charAtPos = grid.Character[mousePosI.X, mousePosI.Y];
		if (!charAtPos.IsNull) {
			string name = charAtPos.GetComponent<Name>().Value;
			var fighter = charAtPos.GetComponent<Fighter>();
			InspectStrings.Add($"{name} {fighter.HP}/{fighter.MaxHP} HP");
		}

		var others = grid.Others[mousePosI.X, mousePosI.Y];
		foreach (var other in others?.Value ?? [])
			InspectStrings.Add($"{other.GetComponent<Name>().Value}");

		// Render text of objects at position
		for (int i = 0; i < InspectStrings.Count; i++) {
			GUI.RenderText(
				InspectStrings[i],
				GUIValues.Padding,
				Raylib.GetScreenHeight() - GUIValues.TextHeight - GUIValues.Padding - i * GUIValues.LineHeight,
				GUIValues.TextHeight,
				3
			);
		}
	}

	private void PathTo(Vec2I posI) {
		ref var pathfinder = ref Singleton.Player.GetComponent<Pathfinder>();
		if (Raylib.IsMouseButtonPressed(MouseButton.Right))
			pathfinder.Reset();
		var path = pathfinder
			.Goal(Singleton.Player.GetComponent<GridPosition>().Value)
			.PathFrom(posI)
			.Reverse().Skip(1).ToList(); // garbage
		foreach (var p in path) {
			Raylib.DrawSphere(p.ToWorldPos() - new Vector3(0.5f, 0, 0.5f), 0.15f,
				Raylib.Fade(Color.RayWhite, 0.3f));
		}
		if (Raylib.IsMouseButtonPressed(MouseButton.Left)) {
			// move player to target destination
			ref var movement = ref Singleton.Player.GetComponent<PathMovement>();
			movement.NewDestination(posI, path);
		}
	}
}

file class RenderDamageFx : QuerySystem<TextFX, Billboard, Position, Scale3> {
	protected override void OnUpdate() {
		Camera camera = Singleton.Camera.GetComponent<Camera>();
		Raylib.BeginMode3D(camera.Value);
		Raylib.BeginShaderMode(Assets.billboardShader);
		Query.ForEachEntity((ref TextFX fx, ref Billboard bill, ref Position pos, ref Scale3 scale, Entity entt) => {
			Raylib.DrawBillboardPro(
				camera.Value,
				fx.RenderTex.Texture,
				// height has to be inverted because weird opengl stuff
				new Rectangle(0, 0, fx.RenderTex.Texture.Width, -fx.RenderTex.Texture.Height),
				pos.value, camera.GetUpVec(),
				new Vector2(scale.x, scale.z), new Vector2(0.5f, 0), 0, Color.Orange
			);
		});
		Raylib.EndShaderMode();
		Raylib.EndMode3D();
	}
}