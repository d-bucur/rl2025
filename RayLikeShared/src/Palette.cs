using Raylib_cs;

class Palette {
	// https://lospec.com/palette-list/pnp-gb
	public static Color[] Palette1 = [
		Raylib.GetColor(0xf2ccffff),
		Raylib.GetColor(0xff66b2ff),
		Raylib.GetColor(0x7451c8ff),
		Raylib.GetColor(0x130026ff),
	];

	// https://lospec.com/palette-list/bilkent-metro
	public static Color[] Palette2 = [
		Raylib.GetColor(0xede1c7ff),
		Raylib.GetColor(0xd8b887ff),
		Raylib.GetColor(0xd17f74ff),
		Raylib.GetColor(0x2f2f4bff),
	];

	// https://lospec.com/palette-list/alien-microbes
	public static Color[] Palette3 = [
		Raylib.GetColor(0x8cff96ff),
		Raylib.GetColor(0xfff3f2ff),
		Raylib.GetColor(0x8f7fb0ff),
		Raylib.GetColor(0x1a181aff),
	];

	// https://lospec.com/palette-list/oil-6
	public static Color[] Palette4 = [
		Raylib.GetColor(0xfbf5efff),
		Raylib.GetColor(0xc69fa5ff),
		Raylib.GetColor(0x494d7eff),
		Raylib.GetColor(0x272744ff),
	];

	// https://lospec.com/palette-list/twilight-

	// https://lospec.com/palette-list/midnight-ablaze

	public static Color[] Colors = Palette3;

	public static Color Background = Colors[3];
	public static Color Wall = Color.Gray;
	public static Color Floor = Color.DarkGray;
	public static Color DebugFOVBlocked = Color.DarkGreen;
	public static Color DebugFOVCorner = Color.DarkBlue;
}