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
	public int DisplayCount = 4;

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

class GuiModule : IModule {
	public void Init(EntityStore world) {
		Singleton.Entity.AddComponent(new MessageLog());

		RenderPhases.Render.Add(new RenderMinimap());
		RenderPhases.Render.Add(new RenderHealth());
		RenderPhases.Render.Add(new RenderGameOver());
		RenderPhases.Render.Add(new RenderMessageLog());
	}
}

static class GUI {
	internal static void RenderText(string text, int posX, int posY, int FontSize, int offset = 5) {
		Raylib.DrawText(
			text,
			posX + offset,
			posY + offset,
			FontSize, Color.Black
		);
		Raylib.DrawText(
			text,
			posX,
			posY,
			FontSize, Color.White
		);
	}
}

file static class GUIValues {
	public const int HealthHeight = 30;
	public const int Padding = 10;
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
		int TextHeight = 20;
		int LineHeight = 25;
		Query.ForEachEntity((ref MessageLog log, Entity entt) => {
			int pos = GUIValues.HealthHeight + GUIValues.Padding + LineHeight;
			for (int i = log.Messages.Count - 1; i >= 0 && i >= log.Messages.Count - log.DisplayCount; i--) {
				var message = log.Messages[i];
				Raylib.DrawText(message.Text, GUIValues.Padding, pos, TextHeight, message.Color);
				pos += LineHeight;
			}
		});
	}
}

file class RenderGameOver : QuerySystem<Fighter> {
	public RenderGameOver() => Filter.AllTags(Tags.Get<Player>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref Fighter fighter, Entity playerEntt) => {
			if (fighter.HP <= 0) {
				GUI.RenderText(
					"You're dead!",
					Raylib.GetScreenWidth() / 2 - 140,
					Raylib.GetScreenHeight() / 2 + 100,
					50
				);
			}
		});
	}
}

// Optimization: could only redraw on change
file class RenderMinimap : QuerySystem {
	Image MinimapImage;
	Texture2D MinimapTexture;

	protected override void OnAddStore(EntityStore store) {
		MinimapImage = Raylib.GenImageColor(Config.MAP_SIZE_X, Config.MAP_SIZE_Y, Palette.Transparent);
		MinimapTexture = Raylib.LoadTextureFromImage(MinimapImage);
	}

	protected override unsafe void OnUpdate() {
		if (!Singleton.Entity.GetComponent<Settings>().MinimapEnabled)
			return;

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
			if (character.Tags.Has<Enemy>() && tile.Tags.Has<IsVisible>())
				color = Color.Red;
		}
		return color;
	}
}