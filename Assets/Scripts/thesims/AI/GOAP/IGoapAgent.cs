using System.Collections.Generic;

namespace Ai.Goap {
/// <summary>
/// An agent that can plan with GOAP.
/// Provides information for the planner and allows receiving feedback from the
/// planner regarding the planning process.
/// </summary>
public interface IGoapAgent : IStateful {
    /// <summary>
    /// Return the current goal. The planner will search for a plan that fulfill
    /// it.
    /// </summary>
    WorldGoal CreateGoalState();

    /// <summary>
    /// No sequence of actions could be found for the supplied goal.
    /// Can be used to select another goal for next time.
    /// </summary>
    void PlanFailed(WorldGoal failedGoal);

    /// <summary>
    /// A plan was found for the given goal.
    /// The plan is a queue of actions that should be performed in order to
    /// fulfill the goal.
    /// </summary>
    void PlanFound(WorldGoal goal, Queue<GoapAction.WithContext> actions);

    /// <summary>
    /// All actions are complete or no valid actions left to be performed.
    /// </summary>
    void ActionsFinished();

    /// <summary>
    /// One of the actions caused the plan to abort.
    /// </summary>
    /// <param name="aborter">The action that failed the plan.</param>
    void PlanAborted(GoapAction.WithContext aborter);

    /// <summary>
    /// Moves the agent towards the target of the action.
    /// </summary>
    /// <returns><c>true</c>, if agent is at the target, <c>false</c> otherwise.</returns>
    bool MoveAgent(GoapAction.WithContext nextAction);
}
}
