namespace RayLikeShared;

static class Helpers {
	internal static T GetRandomEnum<T>(int? min = null, int? max = null) {
		var allValues = Enum.GetValues(typeof(T));
		int rand = Random.Shared.Next(min ?? 0, max ?? allValues.Length);
		return (T)allValues.GetValue(rand)!;
	}
}

public static class DictionaryExtensions {
	public static T Get<T>(this Dictionary<string, object> instance, string name) {
		// saving this for later usage
		// ref var v = ref CollectionsMarshal.GetValueRefOrNullRef(instance, name);
		return (T)instance[name];
	}
}