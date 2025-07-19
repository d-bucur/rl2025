using Friflo.Engine.ECS.Systems;

class UpdatePhases {
	static internal SystemGroup Input = new("InputPhase");
	static internal SystemGroup ApplyActions = new("ApplyActionsPhase");
	static internal SystemGroup Animations = new("AnimationsPhase");

	static internal List<SystemGroup> All = [Input, ApplyActions, Animations];
}

class RenderPhases {
	static internal SystemGroup Render = new("RenderPhase");
}