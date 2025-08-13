namespace RayLikeShared;

static class Helpers {
	internal static T GetRandomEnum<T>() {
		var monsterTypes = Enum.GetValues(typeof(T));
		return (T)monsterTypes.GetValue(Random.Shared.Next(monsterTypes.Length))!;
	}
}