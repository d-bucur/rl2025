global using BTLog = System.Collections.Generic.List<(string, BehaviorTree.ExecutionLogEnum, BehaviorTree.BTStatus?, System.Type)>;
using Friflo.Engine.ECS;
using RayLikeShared;

namespace BehaviorTree;

public enum BTStatus {
	Success,
	Failure,
	Running,
}

public enum ExecutionLogEnum {
	Begin,
	End,
}

public abstract class Behavior {
	public string Name = "";
	public abstract BTStatus Tick(ref Context ctx);
	public BTStatus Execute(ref Context ctx) {
		// Could disable collection of data with directive
		ctx.ExecutionLog.Add((Name, ExecutionLogEnum.Begin, null, GetType()));
		var status = Tick(ref ctx);
		ctx.ExecutionLog.Add((Name, ExecutionLogEnum.End, status, GetType()));
		return status;
	}
	public Behavior Named(string name) {
		Name = name;
		return this;
	}
	// public void OnStart() { }
	// public void OnEnd() { }
}

// Can be defined outside of this file to keep coupling to game minimal
public partial struct Context() {
	public required Entity Entt;
	public required CommandBuffer cmds;
	public required Vec2I Pos;
	public BTLog ExecutionLog = new(10);
}

public class BehaviorTree {
	public Behavior Root;
	public BTStatus Tick(ref Context ctx) {
		return Root.Execute(ref ctx);
	}
}

public delegate bool ConditionFunc(ref Context ctx);
public delegate BTStatus ActionFunc(ref Context ctx);

public class Condition(ConditionFunc F) : Behavior {
	public override BTStatus Tick(ref Context ctx) {
		return F.Invoke(ref ctx) ? BTStatus.Success : BTStatus.Failure;
	}
}

public class Do(ActionFunc F) : Behavior {
	public override BTStatus Tick(ref Context ctx) {
		return F.Invoke(ref ctx);
	}
}

public class Repeat(int Limit, Behavior Child) : Behavior {
	private int Counter;
	public override BTStatus Tick(ref Context ctx) {
		while (true) {
			var status = Child.Execute(ref ctx);
			Counter++;
			if (status == BTStatus.Running) return BTStatus.Running;
			if (status == BTStatus.Failure) {
				Counter = 0;
				return BTStatus.Failure;
			}
			if (Counter >= Limit) {
				Counter = 0;
				return BTStatus.Success;
			}
		}
	}
}

// https://www.behaviortree.dev/docs/nodes-library/SequenceNode
public class Sequence(Behavior[] Children, Sequence.Type type = default) : Behavior {
	public enum Type {
		Default,
		Reactive,
		Memory,
	}
	private int Counter;
	public override BTStatus Tick(ref Context ctx) {
		while (Counter < Children.Length) {
			var status = Children[Counter].Execute(ref ctx);
			Counter++;
			if (status == BTStatus.Running) {
				switch (type) {
					case Type.Default:
					case Type.Memory:
						return BTStatus.Running;
					case Type.Reactive:
						Counter = 0;
						return BTStatus.Failure;
				}
			}
			if (status == BTStatus.Failure) {
				switch (type) {
					case Type.Default:
					case Type.Reactive:
						Counter = 0;
						return BTStatus.Failure;
					case Type.Memory:
						return BTStatus.Failure;
				}
			}
		}
		Counter = 0;
		return BTStatus.Success;
	}
	public Sequence(string name, Behavior[] Children) : this(Children) {
		Name = name;
	}
}

// https://www.behaviortree.dev/docs/nodes-library/FallbackNode
public class Select(Behavior[] Children, bool Reactive = true) : Behavior {
	private int Counter;
	public override BTStatus Tick(ref Context ctx) {
		while (Counter < Children.Length) {
			var status = Children[Counter].Execute(ref ctx);
			Counter++;
			if (status == BTStatus.Running) {
				if (Reactive) Counter = 0;
				return BTStatus.Running;
			}
			if (status == BTStatus.Success) {
				Counter = 0;
				return BTStatus.Success;
			}
		}
		Counter = 0;
		return BTStatus.Failure;
	}
	public Select(string name, Behavior[] Children) : this(Children) {
		Name = name;
	}
	public Select(string name, bool reactive, Behavior[] Children) : this(Children, reactive) {
		Name = name;
	}
}

public class Invert(Behavior Child) : Behavior {
	public override BTStatus Tick(ref Context ctx) {
		return Child.Execute(ref ctx) switch {
			BTStatus.Success => BTStatus.Failure,
			BTStatus.Failure => BTStatus.Success,
			BTStatus.Running => BTStatus.Running,
		};
	}
}

public class Force(BTStatus Status, Behavior Child) : Behavior {
	public override BTStatus Tick(ref Context ctx) {
		Child.Execute(ref ctx);
		return Status;
	}
}