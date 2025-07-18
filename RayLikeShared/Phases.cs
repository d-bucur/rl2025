using Friflo.Engine.ECS.Systems;

class UpdatePhases {
	static internal SystemGroup Input = new("InputPhase");
	static internal SystemGroup ApplyActions = new("ApplyActionsPhase");
}

class RenderPhases {
	static internal SystemGroup Render = new("RenderPhase");
}