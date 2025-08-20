using Friflo.Engine.ECS;
using RayLikeShared;

namespace BehaviorTree;

public enum BTStatus {
	Success,
	Failure,
	Running,
}

public abstract class Behavior {
	public string Name = "";
	public abstract BTStatus Tick(ref Context ctx);
	public Behavior Named(string name) {
		Name = name;
		return this;
	}
	// public void OnStart() { }
	// public void OnEnd() { }
}

public partial struct Context {
	// TODO add as tick parameters, define specifics for game in other file
	public required Entity Entt;
	public required CommandBuffer cmds;
	public required Vec2I Pos;
}

public class BehaviorTree {
	public Behavior Root;
	public BTStatus Tick(ref Context ctx) => Root.Tick(ref ctx);
}

public delegate bool ConditionFunc(ref Context ctx);
public delegate BTStatus ActionFunc(ref Context ctx);

public class Condition(ConditionFunc F) : Behavior {
	public override BTStatus Tick(ref Context ctx) {
		return F.Invoke(ref ctx) ? BTStatus.Success : BTStatus.Failure;
	}
}

// TODO Rename to Do to avoid conflict with system.Action
public class Action(ActionFunc F) : Behavior {
	public override BTStatus Tick(ref Context ctx) {
		return F.Invoke(ref ctx);
		// return BTStatus.Success;
	}
}

public class Repeat(int Limit, Behavior Child) : Behavior {
	private int Counter;
	public override BTStatus Tick(ref Context ctx) {
		while (true) {
			var status = Child.Tick(ref ctx);
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

public class Sequence(Behavior[] Children) : Behavior {
	private int Counter;
	public override BTStatus Tick(ref Context ctx) {
		while (Counter < Children.Length) {
			var status = Children[Counter].Tick(ref ctx);
			Counter++;
			if (status == BTStatus.Running) return BTStatus.Running;
			if (status == BTStatus.Failure) {
				Counter = 0;
				return BTStatus.Failure;
			}
		}
		Counter = 0;
		return BTStatus.Success;
	}
	public Sequence(string name, Behavior[] Children) : this(Children) {
		Name = name;
	}
}

public class Select(Behavior[] Children) : Behavior {
	private int Counter;
	public override BTStatus Tick(ref Context ctx) {
		while (Counter < Children.Length) {
			var status = Children[Counter].Tick(ref ctx);
			Counter++;
			if (status == BTStatus.Running) return BTStatus.Running;
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
}

public class Invert(Behavior Child) : Behavior {
	public override BTStatus Tick(ref Context ctx) {
		return Child.Tick(ref ctx) switch {
			BTStatus.Success => BTStatus.Failure,
			BTStatus.Failure => BTStatus.Success,
			BTStatus.Running => BTStatus.Running,
		};
	}
}

public class Force(BTStatus Status, Behavior Child) : Behavior {
	public override BTStatus Tick(ref Context ctx) {
		Child.Tick(ref ctx);
		return Status;
	}
}