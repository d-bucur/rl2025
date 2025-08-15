using Friflo.Engine.ECS.Systems;

namespace RayLikeShared;

static class UpdatePhases {
	static internal SystemGroup ProgressTurns = new("ProgressTurnsPhase");
	static internal SystemGroup TurnStart = new("TurnStartPhase");
	static internal SystemGroup Input = new("InputPhase");
	static internal SystemGroup ApplyActions = new("ApplyActionsPhase");
	static internal SystemGroup PostApplyActions = new("PostApplyActions");
	static internal SystemGroup Animations = new("AnimationsPhase");

	static internal List<SystemGroup> All = [
		ProgressTurns,
		TurnStart,
		Input,
		ApplyActions,
		PostApplyActions,
		Animations,
	];
}

static class RenderPhases {
	static internal SystemGroup PreRender = new("PreRenderPhase");
	static internal SystemGroup Render = new("RenderPhase");

	static internal List<SystemGroup> All = [
		PreRender,
		Render,
	];
}