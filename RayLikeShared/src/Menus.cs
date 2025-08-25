using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

class Menus : IModule {
	public void Init(EntityStore world) {
		world.CreateEntity(
			new InputReceiver(),
			new CharacterSelection(),
			Tags.Get<InputEnabled>()
		);

		UpdatePhases.Input.Add(new CharacterSelectionInput());
		RenderPhases.Render.Add(new CharacterSelectionRender());
	}
}

struct PlayerChoices : IComponent {
	public struct Choice {
		public Vec2I SpriteIndex;
		public string Name;
		public Prefabs.ConsumableType[] StartingItems;
	}

	public Choice[] Values;
}

struct CharacterSelection : IComponent {
	public int SelectedIdx;
}

internal class CharacterSelectionRender : QuerySystem<CharacterSelection> {
	const int ItemSize = 32;
	const int DestSize = 96;
	public CharacterSelectionRender() => Filter.AllTags(Tags.Get<InputEnabled>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref CharacterSelection selection, Entity entt) => {
			var choices = Singleton.Get<PlayerChoices>();
			var anchorY = (Raylib.GetScreenHeight() - DestSize) / 2;
			Raylib.DrawText("Choose your rogue", 100, anchorY - DestSize, 40, Color.White);
			Raylib.DrawRectangle(DestSize * selection.SelectedIdx, anchorY, DestSize, DestSize, Raylib.Fade(Color.Gray, 0.5f));
			for (int i = 0; i < choices.Values.Length; i++) {
				Vec2I idx = choices.Values[i].SpriteIndex;
				var source = new Rectangle(ItemSize * idx.X, ItemSize * idx.Y, ItemSize, ItemSize);
				var dest = new Rectangle(DestSize * i, anchorY, DestSize, DestSize);
				Raylib.DrawTexturePro(
					Assets.heroesTexture,
					source,
					dest,
					Vector2.Zero,
					0,
					Color.White
				);
			}
		});
	}
}

internal class CharacterSelectionInput : QuerySystem<CharacterSelection> {
	public CharacterSelectionInput() => Filter.AllTags(Tags.Get<InputEnabled>());

	protected override void OnUpdate() {
		Query.ThrowOnStructuralChange = false;
		Query.ForEachEntity((ref CharacterSelection selection, Entity entt) => {
			var choices = Singleton.Get<PlayerChoices>();
			if (Raylib.IsKeyPressed(KeyboardKey.A) || Raylib.IsKeyPressed(KeyboardKey.Left)) {
				selection.SelectedIdx--;
				if (selection.SelectedIdx < 0) selection.SelectedIdx += choices.Values.Length;
			}
			if (Raylib.IsKeyPressed(KeyboardKey.D) || Raylib.IsKeyPressed(KeyboardKey.Right)) {
				selection.SelectedIdx = (selection.SelectedIdx + 1) % choices.Values.Length;
			}
			if (Raylib.IsKeyPressed(KeyboardKey.Space) || Raylib.IsKeyPressed(KeyboardKey.Enter)) {
				CommandBuffer.DeleteEntity(entt.Id);
				LevelModule.StartLevel(choices.Values[selection.SelectedIdx]);
			}
		});
	}
}