using UnityEngine;
using System;
using System.Collections.Generic;
using Infra;
using Infra.Collections;
using Priority_Queue;

namespace Ai.Goap {
/**
 * Plans what actions can be completed in order to fulfill a goal state.
 */
public static class GoapPlanner {
    // This seems like enough...
    private const int MAX_FRINGE_NODES = 2000;

    /// <summary>
    /// A* forward search for a plan that satisfies the given goal.
    /// </summary>
    /// <returns>Returns null if a plan could not be found, or a list of the
    /// actions that must be performed, in order.</returns>
    public static Queue<GoapAction.WithContext> Plan(
            GoapAgent agent,
            List<GoapAction> availableActions,
            WorldGoal goal) {
        var worldState = WorldState.pool.Borrow();
        worldState.Clear();
        worldState[agent] = agent.GetState();

        DebugUtils.Assert(worldState[agent].ContainsKey("x")
            && worldState[agent].ContainsKey("x"),
            "Agent's state must contain his position as 'x' and 'y' keys");

        var exploredNodes = new Dictionary<WorldState, Node>(WorldStateComparer.instance);
        var closedSet = new HashSet<WorldState>(WorldStateComparer.instance);
        var openSet = new FastPriorityQueue<Node>(MAX_FRINGE_NODES);

        var currentNode = Node.pool.Borrow();
        currentNode.Init(null, 0, worldState, null, null);

        openSet.Enqueue(currentNode, 0f);

        // TODO: Implement reverse search. Start from the goal and only consider
        //       actions that satisfy an unmet condition. Each step creates a
        //       a new goal (instead of creating a new state). When the initial
        //       state satisfies one of the found goals, we have a plan!

        WorldState childState;
        Node childNode;
        Vector2 currentPosition = Vector2.zero;
        while (openSet.Count > 0) {
            currentNode = openSet.Dequeue();
            // If the node satisfies the goal.
            if (DoConditionsApplyToWorld(goal, currentNode.state)) {
                //DebugUtils.LogError("Selected plan with cost: " + currentNode.Score);
                var plan = UnwrapPlan(currentNode);
                // Return all nodes.
                Node.pool.ReturnAll();
                WorldState.pool.Return(worldState);

                // Check for leaks in the pools:
                //DebugUtils.LogError("Nodes: " + Node.pool.Count);
                //DebugUtils.LogError("WithContext: " + GoapAction.WithContext.pool.Count);
                //DebugUtils.LogError("WorldState: " + WorldState.pool.Count);

                return plan;
            }
            // Mark this node as closed - don't explore it again.
            closedSet.Add(currentNode.state);
            exploredNodes.Remove(currentNode.state);
            foreach (var action in availableActions) {
                if (!DoConditionsApplyToWorld(action.GetIndependentPreconditions(agent), currentNode.state)) {
                    // Action does not apply to this state.
                    //DebugUtils.LogError("Action: " + GoapAgent.PrettyPrint(action) + " does not apply to agent");
                    continue;
                }
                var targets = action.GetAllTargets(agent);
                //DebugUtils.LogError("Checking Action: " + GoapAgent.PrettyPrint(action) + " got targets: " + targets.Count);
                foreach (var target in targets) {
                    if (agent != target
                        && !DoConditionsApplyToWorld(action.GetDependentPreconditions(agent, target), currentNode.state)) {
                        // Action does not apply to this target.
                        //DebugUtils.LogError("Action does not apply for target: " + (target as Component).name);
                        continue;
                    }

                    // SPECIAL CASE: Target can be used in a specific manner.
                    // Only certain action can be applied to it.
                    // Each action (e.g. TurnOn) automatically triggers another
                    // action. We must check that this action can be applied.
                    var actionTarget = target as ActionTarget;
                    GoapAction triggeredAction = null;
                    if (actionTarget != null && actionTarget.actionEffects.Count > 0) {
                        // Action target defined what actions can be applied to it.
                        if (!actionTarget.actionEffects.ContainsKey(action)) {
                            // Current action can not be applied to target.
                            continue;
                        }
                        triggeredAction = actionTarget.actionEffects[action];
                        // The triggered action is applied by the target onto the
                        // agent (reverse than normal).
                        if (!DoConditionsApplyToWorld(triggeredAction.GetIndependentPreconditions(target), currentNode.state)) {
                            // Triggered action does not apply to this target.
                            continue;
                        }
                        if (!DoConditionsApplyToWorld(triggeredAction.GetDependentPreconditions(target, agent), currentNode.state)) {
                            // Triggered action does not apply to this state.
                            continue;
                        }
                    }

                    // Apply the action's effects to the parent state.
                    childState = ChangeWorldState(currentNode.state, action.GetDependentEffects(agent, target));
                    if (triggeredAction != null) {
                        // Apply the triggered action.
                        childState = ChangeWorldState(childState, triggeredAction.GetDependentEffects(target, agent), true);
                    }

                    // Calculate travel cost and apply the movement to the state.
                    var travelCost = 0f;
                    if (action.RequiresInRange()) {
                        var obj = target as Component;
                        // TODO: Move this to the action's effects. Instead of
                        //       action.cost, use action.CalculateCost(state, target)
                        //       that will return action.cost + travelCost (or
                        //       something else if the specific action requires
                        //       it).
                        currentPosition.Set((int)childState[agent]["x"].value, (int)childState[agent]["y"].value);
                        var travelVector = (Vector2)obj.transform.position - currentPosition;
                        travelCost = travelVector.magnitude;
                        var x = StateValue.NormalizeValue(obj.transform.position.x);
                        var y = StateValue.NormalizeValue(obj.transform.position.y);
                        childState[agent]["x"] = new StateValue(x);
                        childState[agent]["y"] = new StateValue(y);
                        //DebugUtils.LogError(travelCost + " to " + obj.name);
                    }
                    //DebugUtils.LogError(GoapAgent.PrettyPrint(childState));
                    //Debug.Break();

                    // Now that we have the new state, we can check if it was
                    // already evaluated and if so, we ignore it.
                    if (closedSet.Contains(childState)) continue;

                    float tempRunningCost = currentNode.runningCost + action.cost + travelCost;
                    if (!exploredNodes.TryGetValue(childState, out childNode)) {
                        childNode = Node.pool.Borrow();
                        childNode.Init(currentNode, tempRunningCost, childState, action, target);
                        // Cache the node for later.
                        exploredNodes[childState] = childNode;

                        DebugUtils.Assert(!openSet.Contains(childNode), "Open set contains new node... How can this be?");

                        // This is a new node.
                        childNode.CalculateHeuristicCost(goal);
                        openSet.Enqueue(childNode, childNode.Score);
                        //DebugUtils.LogError("New node cost: " + tempRunningCost + " " + childNode.PathToString());
                    } else if (tempRunningCost < childNode.runningCost) {
                        // Found the best path so far to this node.
                        childNode.parent = currentNode;
                        childNode.runningCost = tempRunningCost;
                        childNode.action.Init(target, action);
                        //DebugUtils.LogError("!!! Best cost: " + tempRunningCost + " " + childNode.PathToString());
                    }
                }
            }
        }
        DebugUtils.Log("NO PLAN");
        Node.pool.ReturnAll();
        WorldState.pool.Return(worldState);
        return null;
    }

    /// <summary>
    /// Check that all items in 'test' are in 'state'. If just one does not
    /// match or is not there then this returns false.
    /// </summary>
    private static bool DoConditionsApply(Goal test, State state) {
        foreach (var t in test) {
            if (!state.ContainsKey(t.Key) || !state[t.Key].CheckCondition(t.Value)) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Check that all items in 'test' are in 'state'. If just one does not
    /// match or is not there then this returns false.
    /// </summary>
    public static bool DoConditionsApplyToWorld(WorldGoal test, WorldState state) {
        foreach (var targetGoal in test) {
            if (!state.ContainsKey(targetGoal.Key)) {
                state[targetGoal.Key] = targetGoal.Key.GetState();
            }
            var targetState = state[targetGoal.Key];
            if (!DoConditionsApply(targetGoal.Value, targetState)) {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Apply the stateChange to the currentState.
    /// </summary>
    private static State ChangeState(State currentState, Effects stateChange) {
        // Copy state.
        var state = new State();
        foreach (var item in currentState) {
            state[item.Key] = item.Value;
        }
        // Apply change.
        foreach (var change in stateChange) {
            if (!state.ContainsKey(change.Key)) {
                state[change.Key] = new StateValue(change.Value.value);
            } else {
                // Only copy StateValue when changing it.
                state[change.Key] = new StateValue(state[change.Key].value);
                state[change.Key].ApplyEffect(change.Value);
            }
        }
        return state;
    }

    /// <summary>
    /// Apply the stateChange to the currentState.
    /// </summary>
    private static WorldState ChangeWorldState(WorldState currentWorldState, WorldEffects stateChange, bool inPlace = false) {
        // Copy state.
        WorldState worldState = currentWorldState;
        if (!inPlace) {
            worldState = new WorldState();
            foreach (var kvp in currentWorldState) {
                worldState[kvp.Key] = kvp.Value;
            }
        }
        // Apply change.
        foreach (var change in stateChange) {
            if (!worldState.ContainsKey(change.Key)) {
                worldState[change.Key] = change.Key.GetState();
            }
            worldState[change.Key] = ChangeState(worldState[change.Key], change.Value);
        }
        return worldState;
    }

    private static Queue<GoapAction.WithContext> UnwrapPlan(Node endNode) {
        var result = new List<GoapAction.WithContext>();
        var node = endNode;
        while (node.parent != null) {
            result.Add(node.action.Clone());
            node = node.parent;
        }
        result.Reverse();
        return new Queue<GoapAction.WithContext>(result);
    }


    /// <summary>
    /// This is needed to deep-compare the states by value and not by reference.
    /// </summary>
    private class StateComparer : IEqualityComparer<State> {
        public static StateComparer instance = new StateComparer();
        public bool Equals(State state1, State state2) {
            if (state1 == state2) return true;
            if ((state1 == null) || (state2 == null)) return false;
            if (state1.Count != state2.Count) return false;

            foreach (var kvp in state1) {
                StateValue value2;
                if (!state2.TryGetValue(kvp.Key, out value2)) return false;
                if (!kvp.Value.value.Equals(value2.value)) return false;
            }
            return true;
        }
        public int GetHashCode(State obj) {
            return GoapAgent.PrettyPrint(obj).GetHashCode();
        }
    }

    /// <summary>
    /// This is needed to deep-compare the states by value and not by reference.
    /// </summary>
    private class WorldStateComparer : IEqualityComparer<WorldState> {
        public static WorldStateComparer instance = new WorldStateComparer();
        public bool Equals(WorldState state1, WorldState state2) {
            if (state1 == state2) return true;
            if ((state1 == null) || (state2 == null)) return false;

            foreach (var kvp in state1) {
                State value2;
                if (!state2.TryGetValue(kvp.Key, out value2)) {
                    state2[kvp.Key] = kvp.Key.GetState();
                }
                if (!StateComparer.instance.Equals(kvp.Value, value2)) return false;
            }

            foreach (var kvp in state2) {
                State value1;
                if (!state1.TryGetValue(kvp.Key, out value1)) {
                    state1[kvp.Key] = kvp.Key.GetState();
                }
                if (!StateComparer.instance.Equals(kvp.Value, value1)) return false;
            }
            return true;
        }
        public int GetHashCode(WorldState obj) {
            return GoapAgent.PrettyPrint(obj).GetHashCode();
        }
    }

    /// <summary>
    /// Used for building up the graph and holding the running costs of actions.
    /// </summary>
    private class Node : FastPriorityQueueNode {
        public static ObjectPool<Node> pool = new ObjectPool<Node>(100, 25);

        public Node parent;
        /// <summary>
        /// Cost to reach this node.
        /// </summary>
        public float runningCost;
        public float cachedHeuristicCost = float.NegativeInfinity;
        public WorldState state;
        public GoapAction.WithContext action = GoapAction.WithContext.pool.Borrow();

        public float Score {
            get {
                return runningCost + cachedHeuristicCost;
            }
        }

        public void Init(Node parent, float runningCost, WorldState state, GoapAction action, IStateful target) {
            this.parent = parent;
            this.runningCost = runningCost;
            this.state = state;
            this.action.Init(target, action);
        }

        public float CalculateHeuristicCost(WorldGoal goal, bool useCachedValue = true) {
            if (useCachedValue && cachedHeuristicCost >= 0f) {
                return cachedHeuristicCost;
            }
            cachedHeuristicCost = 0f;
            // TODO: Calculate heuristic for A*.
            return cachedHeuristicCost;
        }

        public string PathToString() {
            var node = this;
            var s = string.Empty;
            while (node.parent != null) {
                s = GoapAgent.PrettyPrint(node.action) + ">" + s;
                node = node.parent;
            }
            return s;
        }
    }
}
}
