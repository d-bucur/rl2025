using System.Numerics;
using Friflo.Engine.ECS;
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

static class Progression {
	// TODO handle other cases where hero can gain XP, like spells
	public static void GainXP(Entity winner, Entity loser) {
		if (!loser.HasComponent<XPGiven>() || !winner.HasComponent<Level>()) return;
		var xp = loser.GetComponent<XPGiven>();
		ref var level = ref winner.GetComponent<Level>();
		level.CurrentXP += xp.XPOnDeath;
		int toNext = level.XPToNextLevel();
		
		Vector3 loserPos = loser.GetComponent<Position>().value;
		GUI.SpawnDamageFx(xp.XPOnDeath, loserPos, Color.Blue, winner.GetComponent<Position>().value - loserPos);

		if (level.CurrentXP < toNext) return;
		level.CurrentXP -= toNext;
		level.CurrentLevel++;
		MessageLog.Print($"Hero advances to level {level.CurrentLevel}");
	}
}