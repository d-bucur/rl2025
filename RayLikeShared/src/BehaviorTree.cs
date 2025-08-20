namespace BehaviorTree;

public enum BTStatus {
	Success,
	Failure,
	Running,
}

public interface Behavior {
	public BTStatus Tick();
	// public void OnStart() { }
	// public void OnEnd() { }
}

public class BehaviorTree {
	public Behavior Root;
	public void Tick() => Root.Tick();
}

public class Condition(Func<bool> F) : Behavior {
	public BTStatus Tick() {
		return F.Invoke() ? BTStatus.Success : BTStatus.Failure;
	}
}

public class Action(System.Action F) : Behavior {
	public BTStatus Tick() {
		F.Invoke();
		return BTStatus.Success;
	}
}

public class Repeat(int Limit, Behavior Child) : Behavior {
	private int Counter;
	public BTStatus Tick() {
		while (true) {
			var status = Child.Tick();
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
	public BTStatus Tick() {
		while (Counter < Children.Length) {
			var status = Children[Counter].Tick();
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
}

public class Select(Behavior[] Children) : Behavior {
	private int Counter;
	public BTStatus Tick() {
		while (Counter < Children.Length) {
			var status = Children[Counter].Tick();
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
}