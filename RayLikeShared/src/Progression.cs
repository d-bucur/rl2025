using System.Numerics;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using Raylib_cs;

namespace RayLikeShared;

struct Level() : IComponent {
	public int CurrentLevel = 1;
	public int CurrentXP = 0;
	public static int LevelUpBase = 10;
	public static int LevelUpFactor = 100;

	public int XPToNextLevel() => LevelUpBase + CurrentLevel * LevelUpFactor;
}

struct XPGiven : IComponent {
	required public int XPOnDeath;
}

struct PowerupSelector : IComponent {
	internal delegate void PowerupApplier(Entity e);
	required internal (PowerupApplier, string)[] Choices;
}

class Progression : IModule {
	public void Init(EntityStore world) {
		UpdatePhases.Input.Add(new ProgressScreenInput());
		RenderPhases.Render.Add(new ProgressScreenRender());
	}

	// TODO handle other cases where hero can gain XP, like spells
	internal static void GainXP(Entity winner, Entity loser) {
		if (!loser.HasComponent<XPGiven>() || !winner.HasComponent<Level>()) return;
		var xp = loser.GetComponent<XPGiven>();

		Vector3 loserPos = loser.GetComponent<Position>().value;
		GUI.SpawnDamageFx(xp.XPOnDeath, loserPos, Color.Blue, winner.GetComponent<Position>().value - loserPos);

		ref var level = ref winner.GetComponent<Level>();
		bool flowControl = AddXP(ref level, xp.XPOnDeath);
		if (!flowControl) {
			return;
		}
	}

	internal static bool AddXP(ref Level level, int xp) {
		level.CurrentXP += xp;
		int toNext = level.XPToNextLevel();
		if (level.CurrentXP < toNext) return false;
		level.CurrentXP -= toNext;
		level.CurrentLevel++;
		MessageLog.Print($"Hero advances to level {level.CurrentLevel}");

		var selector = SpawnProgressSelector();
		var cmds = Singleton.World.GetCommandBuffer();
		InputModule.PushReceiver(selector, ref cmds);
		cmds.Playback();
		return true;
	}

	private static Entity SpawnProgressSelector() {
		return Singleton.World.CreateEntity(
			new InputReceiver(),
			new PowerupSelector() {
				Choices = [
					// HP powerup
					(e => {
						ref var f = ref e.GetComponent<Fighter>();
						f.HP += 5;
						f.MaxHP += 5;
					}, "+5 HP"),
					// Power powerup
					(e => {
						ref var f = ref e.GetComponent<Fighter>();
						f.Power += 1;
					}, "+1 Power"),
					// Defense powerup
					(e => {
						ref var f = ref e.GetComponent<Fighter>();
						f.Defense.Faces += 1;
					}, "+1 Defense"),
				]
			}
		);
	}
}

internal class ProgressScreenInput : QuerySystem<InputReceiver, PowerupSelector> {
	public ProgressScreenInput() => Filter.AllTags(Tags.Get<InputEnabled>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref InputReceiver receiver, ref PowerupSelector selector, Entity e) => {
			int selected = -1;
			if (Raylib.IsKeyReleased(KeyboardKey.One)) selected = 0;
			if (Raylib.IsKeyReleased(KeyboardKey.Two)) selected = 1;
			if (Raylib.IsKeyReleased(KeyboardKey.Three)) selected = 2;
			if (selected < 0) return;

			// Apply upgrade
			selector.Choices[selected].Item1(Singleton.Player);
			var cmds = CommandBuffer;
			InputModule.PopReceiver(e, ref cmds);
			cmds.DeleteEntity(e.Id);
		});
	}
}

internal class ProgressScreenRender : QuerySystem<InputReceiver, PowerupSelector> {
	public ProgressScreenRender() => Filter.AllTags(Tags.Get<InputEnabled>());

	protected override void OnUpdate() {
		Query.ForEachEntity((ref InputReceiver receiver, ref PowerupSelector selector, Entity e) => {
			// horrible layout code
			const int fontSize = 20;
			const int anchorX = 200;
			const int anchorY = 200;
			const int spacing = 10;
			var rect = new Rectangle(
				anchorX - spacing,
				anchorY - (fontSize + spacing) - spacing,
				200 + spacing * 2,
				4 * (fontSize + spacing) + spacing * 2
			);
			Raylib.DrawRectangle((int)rect.X - spacing, (int)rect.Y - spacing, (int)rect.Width + spacing * 2, (int)rect.Height + spacing * 2, Raylib.Fade(Color.RayWhite, 0.5f));
			Raylib.DrawRectangleRec(rect, Raylib.Fade(Color.DarkGray, 0.75f));
			Raylib.DrawText("Choose an Upgrade", anchorX, anchorY - (fontSize + spacing), fontSize, Color.White);
			for (int i = 0; i < selector.Choices.Length; i++) {
				Raylib.DrawText($"{i + 1}) {selector.Choices[i].Item2}",
					anchorX, anchorY + i * (fontSize + spacing), fontSize, Color.White);
			}
		});
	}
}