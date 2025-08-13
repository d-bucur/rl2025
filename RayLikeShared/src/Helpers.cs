namespace RayLikeShared;

static class Helpers {
	internal static T GetRandomEnum<T>(int? min = null, int? max = null) {
		var allValues = Enum.GetValues(typeof(T));
		int rand = Random.Shared.Next(min ?? 0, max ?? allValues.Length);
		return (T)allValues.GetValue(rand)!;
	}
}