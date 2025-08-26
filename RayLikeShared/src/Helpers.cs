namespace RayLikeShared;

static class Helpers {
	internal static T GetRandomEnum<T>(int? min = null, int? max = null) {
		var allValues = Enum.GetValues(typeof(T));
		int rand = Random.Shared.Next(min ?? 0, max ?? allValues.Length);
		return (T)allValues.GetValue(rand)!;
	}

	/// <summary>
	/// Weights for enemy distribution
	/// Generates tons of garbage, don't use in hot paths
	/// </summary>
	internal static float[] MakeWeights(int count, int step) {
		// formula used: https://www.desmos.com/calculator/tdpitcdlfu
		return Enumerable.Range(0, count)
			.Select(i => {
				var v = Math.Max(0, MathF.Log(step - i) * 4.3f * MathF.Pow(1.7f, i) + 2);
				if (v >= 0) return v;
				else return 0;
			})
			.ToArray();
	}

	internal static int GetRandomWeighted(float[] weights) {
		var total = weights.Sum();
		var rand = Random.Shared.NextSingle() * total;
		for (int i = 0; i < weights.Length; i++) {
			if (rand < weights[i]) return i;
			rand -= weights[i];
		}
		return -1;
	}
}

public static class DictionaryExtensions {
	public static T Get<T>(this Dictionary<string, object> instance, string name) {
		// saving this for later usage
		// ref var v = ref CollectionsMarshal.GetValueRefOrNullRef(instance, name);
		return (T)instance[name];
	}
}