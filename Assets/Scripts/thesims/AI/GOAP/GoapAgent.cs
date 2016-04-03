using UnityEngine;
using System.Collections.Generic;
using System.Text;
using Ai.Fsm;

namespace Ai.Goap {
/// <summary>
/// An abstract GOAP agent that can figure out plans based on a goal and a set
/// of actions.
/// </summary>
	public abstract class GoapAgent : MonoBehaviour, IGoapAgent {
	// NOTE: DEBUG_PLAN is defined at the Edit > Project Settings > Player > Other Settings.
	#if DEBUG_PLAN
	    [Tooltip("Used for debugging")]
	    public List<string> currentPlan;
	#endif

	    [Tooltip("List of possible actions the agent can perform")]
	    public List<GoapAction> availableActions;
		public Sprite defaultSprite;

	    private FSM stateMachine = new FSM();
	    private Queue<GoapAction.WithContext> currentActions = new Queue<GoapAction.WithContext>();

	    protected virtual void Awake() {
	        stateMachine.PushState(IdleState);
	    }

	    protected void Update() {
	        stateMachine.Update(gameObject);
	    }


		public void changeSprite(Sprite newSprite){
			transform.GetChild (0).GetComponent <SpriteRenderer>().sprite = newSprite;
		}

	    /// <summary>
	    /// Returns the current state of the object for planning purposes.
	    /// </summary>
	    public abstract State GetState();

		public State GetPerceivedState (){
			//default implementation does the same as getState
			return GetState ();
		}

	    /// <summary>
	    /// Return the current goal. The planner will search for a plan that fulfill
	    /// it.
	    /// </summary>
	    public abstract WorldGoal CreateGoalState();

	    /// <summary>
	    /// No sequence of actions could be found for the supplied goal.
	    /// Can be used to select another goal for next time.
	    /// </summary>
	    public abstract void PlanFailed(WorldGoal failedGoal);

	    /// <summary>
	    /// A plan was found for the given goal.
	    /// The plan is a queue of actions that should be performed in order to
	    /// fulfill the goal.
	    /// </summary>
	    public abstract void PlanFound(WorldGoal goal, Queue<GoapAction.WithContext> actions);

	    /// <summary>
	    /// All actions are complete or no valid actions left to be performed.
	    /// </summary>
	    public abstract void ActionsFinished();

	    /// <summary>
	    /// One of the actions caused the plan to abort.
	    /// </summary>
	    /// <param name="aborter">The action that failed the plan.</param>
	    public abstract void PlanAborted(GoapAction.WithContext aborter);

	    /// <summary>
	    /// Moves the agent towards the target of the action.
	    /// </summary>
	    /// <returns><c>true</c>, if agent is at the target, <c>false</c> otherwise.</returns>
	    public abstract bool MoveAgent(GoapAction.WithContext nextAction);

	    private bool HasActionPlan() {
	        return currentActions.Count > 0;
	    }

	#region FSM States
	    private void IdleState(FSM fsm, GameObject gameObj) {
	        // GOAP planning.
	        // Get the goal we want to plan for.
	        var goal = CreateGoalState();

	        // Plan.
	        var plan = GoapPlanner.Plan(this, availableActions, goal);
	        if (plan != null) {
	            // We have a plan, hooray!
	            // Clear old plan.
	            while (currentActions.Count > 0) {
	                var context = currentActions.Dequeue();
	                GoapAction.WithContext.pool.Return(context);
	            }
	            currentActions = plan;
	            PlanFound(goal, plan);

	#if DEBUG_PLAN
	            currentPlan.Clear();
	            foreach (var action in currentActions) {
	                currentPlan.Add(action.actionData.name + " " + (action.target as Component).name);
	            }
	#endif

	            // Move to PerformAction state.
	            fsm.PopState();
	            fsm.PushState(PerformActionState);
	        } else {
	            // Couldn't get a plan.
	            Debug.Log("<color=orange>Failed Plan:</color>" + PrettyPrint(goal));
	            PlanFailed(goal);
	            // Move back to IdleAction state.
	            fsm.PopState();
	            fsm.PushState(IdleState);
	        }
	    }

	    private void MoveToState(FSM fsm, GameObject gameObj) {
	        // Move the game object.
	        var action = currentActions.Peek();
	        if (action.actionData.RequiresInRange() && action.target == null) {
	            Debug.Log("<color=red>Fatal error:</color> Action requires a target but has none. Planning failed. You did not assign the target in your Action.checkProceduralPrecondition()");
	            // Move.
	            fsm.PopState();
	            // Perform.
	            fsm.PopState();
	            fsm.PushState(IdleState);
	            return;
	        }

	        // Get the agent to move itself.
	        if (MoveAgent(action)) {
	            fsm.PopState();
	        }
	    }

	    private void PerformActionState(FSM fsm, GameObject gameObj) {
	        // Perform the action.
	        if (!HasActionPlan()) {
	            // No actions to perform.
	            Debug.Log("<color=red>Done actions</color>");
	            fsm.PopState();
	            fsm.PushState(IdleState);
	            ActionsFinished();
	            return;
	        }

	        var action = currentActions.Peek();
	        if (action.isDone) {
	            // The action is done. Remove it so we can perform the next one.
	            var context = currentActions.Dequeue();
	            GoapAction.WithContext.pool.Return(context);
	#if DEBUG_PLAN
	            currentPlan.RemoveAt(0);
	#endif
	        }

	        if (HasActionPlan()) {
	            // Perform the next action.
	            action = currentActions.Peek();
	            bool inRange = !action.actionData.RequiresInRange() || action.isInRange;

	            if (inRange) {
	                // We are in range, so perform the action.
	                bool success = action.Perform(gameObj.GetComponent<GoapAgent>());

	                if (!success) {
	                    // Action failed, we need to plan again.
	                    fsm.PopState();
	                    fsm.PushState(IdleState);
	                    PlanAborted(action);
	                }
	            } else {
	                // We need to move there first.
	                // Push moveTo state.
	                fsm.PushState(MoveToState);
	            }
	            return;
	        }
	        // No valid actions left, move to Plan state.
	        fsm.PopState();
	        fsm.PushState(IdleState);
	        ActionsFinished();
	    }
	#endregion

	#region Printing GOAP structures
	    public static string PrettyPrint(WorldState state) {
	        var s = new StringBuilder();
	        foreach (var kvp in state) {
	            var target = kvp.Key as Component;
	            s.Append(target.name).Append(": ").Append(PrettyPrint(kvp.Value)).Append('\n');
	        }
	        return s.ToString();
	    }

	    public static string PrettyPrint(State state) {
	        var s = new StringBuilder();
	        foreach (var kvp in state) {
	            s.Append(kvp.Key).Append(':').Append(kvp.Value.value).Append(", ");
	        }
	        return s.ToString();
	    }

	    public static string PrettyPrint(WorldGoal state) {
	        var s = new StringBuilder();
	        foreach (var kvp in state) {
	            var target = kvp.Key as Component;
	            s.Append(target.name).Append(": ").Append(PrettyPrint(kvp.Value)).Append('\n');
	        }
	        return s.ToString();
	    }

	    public static string PrettyPrint(Goal state) {
	        var s = new StringBuilder();
	        foreach (var kvp in state) {
	            s.Append(kvp.Key).Append(':')
	                .Append(kvp.Value.comparison).Append(' ')
	                .Append(kvp.Value.value).Append(", ");
	        }
	        return s.ToString();
	    }

	    public static string PrettyPrint(IEnumerable<GoapAction.WithContext> actions) {
	        var s = new StringBuilder();
	        foreach (var a in actions) {
	            s.Append(PrettyPrint(a)).Append("-> ");
	        }
	        s.Append("GOAL");
	        return s.ToString();
	    }

	    public static string PrettyPrint(GoapAction.WithContext[] actions) {
	        var s = new StringBuilder();
	        foreach (var a in actions) {
	            s.Append(PrettyPrint(a)).Append(", ");
	        }
	        return s.ToString();
	    }

	    public static string PrettyPrint(GoapAction.WithContext action) {
	        var s = new StringBuilder();
	        return s.Append(action.actionData.name).Append('@').Append((action.target as Component).name).ToString();
	    }

	    public static string PrettyPrint(GoapAction action) {
	        return action.name;
	    }
		public static string PrettyPrint(Condition condition) {
			var s = new StringBuilder();
			s.Append(condition.comparison)
				.Append(": ")
				.Append(condition.value);
			return s.ToString();
		}
	#endregion
	}
}
