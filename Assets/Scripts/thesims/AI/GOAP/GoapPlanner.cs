using UnityEngine;
using System;
using System.Collections.Generic;
using Infra;
using Infra.Collections;
using Priority_Queue;

namespace Ai.Goap
{
	/**
 	* Plans what actions can be completed in order to fulfill a goal state.
 	*/
	public static class GoapPlanner
	{
		// This seems like enough...
		private const int MAX_FRINGE_NODES = 100;

		/// <summary>
		/// A* forward search for a plan that satisfies the given goal.
		/// </summary>
		/// <returns>Returns null if a plan could not be found, or a list of the
		/// actions that must be performed, in order.</returns>
		public static Queue<GoapAction.WithContext> Plan (
			GoapAgent agent,
			List<GoapAction> availableActions,
			WorldGoal goal)
		{

			var exploredNodes = new Dictionary<WorldState, Node> (WorldStateComparer.instance);
			var closedSet = new HashSet<WorldState> (WorldStateComparer.instance);
			var openSet = new FastPriorityQueue<Node> (MAX_FRINGE_NODES);

			var currentNode = Node.pool.Borrow ();
			currentNode.Init (null, 0f, goal, null, null);
			currentNode.position = Vector2.zero;

			openSet.Enqueue (currentNode, 0f);
			int iterations = 0;

			while (openSet.Count > 0 && iterations < MAX_FRINGE_NODES-10) {
				iterations++;

				currentNode = openSet.Dequeue ();

				//TODO: delete print
				//Debug.Log ("current world goal: " + GoapAgent.PrettyPrint(currentNode.goal));

				if (DoConditionsApply (currentNode.goal[agent], agent.GetState ())) {
					//DebugUtils.LogError("Selected plan with cost: " + currentNode.Score);
					var plan = UnwrapPlan (currentNode);

					// Return all nodes.
					Node.pool.ReturnAll ();

					// Check for leaks in the pools:
					//DebugUtils.LogError("Nodes: " + Node.pool.Count);
					//DebugUtils.LogError("WithContext: " + GoapAction.WithContext.pool.Count);
					//DebugUtils.LogError("WorldState: " + WorldState.pool.Count);

					return plan;
				}

				foreach (GoapAction action in availableActions) {
					
					//TODO: delete print
					//Debug.Log("considering " + action.name);

					WorldGoal possibleChildGoal = action.reverseApplyToWorldGoal (currentNode.goal);

					//Debug.Log ("new goal will be: " + GoapAgent.PrettyPrint(possibleChildGoal));

					if (agent.GetState().isGoalCloser(currentNode.goal, possibleChildGoal)) {

						List<IStateful> targets = action.GetAllTargets (agent);

						// No targets, move to next action
						if (targets.Count == 0) {
							continue;
						}

						IStateful closestTarget = null;
						float travelCost = 0f;
						foreach (var target in targets) {
							//TODO: save only closest target #omri
							closestTarget = target;

							//TODO: check target preconds, make sure this works
							if (goal.ContainsKey (target)) {
								if (!DoConditionsApply (goal [target], target.GetPerceivedState ())) {
									continue;
								}
							}

							if (action.RequiresInRange ()) {
								var obj = target as Component;
								// TODO: Move this to the action's effects. Instead of
								//       action.cost, use action.CalculateCost(state, target)
								//       that will return action.cost + travelCost (or
								//       something else if the specific action requires
								//       it).
//								currentPosition.Set ((int)childState [agent] ["x"].value, (int)childState [agent] ["y"].value);
								currentNode.position.Set (currentNode.position.x, currentNode.position.y);
								var travelVector = (Vector2)obj.transform.position - currentNode.position;
								travelCost = travelVector.magnitude;

//								var x = StateValue.NormalizeValue (obj.transform.position.x);
//								var y = StateValue.NormalizeValue (obj.transform.position.y);
//								childState [agent] ["x"] = new StateValue (x);
//								childState [agent] ["y"] = new StateValue (y);
								//DebugUtils.LogError(travelCost + " to " + obj.name);
							}
						}

						float cost = currentNode.runningCost + action.cost + action.workDuration + travelCost;

						Node newChiledNode = Node.pool.Borrow ();
						newChiledNode.Init (currentNode, cost, possibleChildGoal, action, closestTarget);
						openSet.Enqueue (newChiledNode, newChiledNode.runningCost);
					}

					//TODO: delete 'else' scope
					else {
						//Debug.Log (action.name + " doesnt improve goal");
					}
						
				}

			}
			//TODO: return plan failed #yoel
			return null;	
		}


		/// <summary>
		/// Check that all items in 'test' are in 'state'. If just one does not
		/// match or is not there then this returns false.
		/// </summary>
		private static bool DoConditionsApply (Goal test, State state)
		{
			foreach (var t in test) {
				if (!state.ContainsKey (t.Key) || !state [t.Key].CheckCondition (t.Value)) {
					return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Check that all items in 'test' are in 'state'. If just one does not
		/// match or is not there then this returns false.
		/// </summary>
		public static bool DoConditionsApplyToWorld (WorldGoal test, WorldState state)
		{
			foreach (var targetGoal in test) {
				if (!state.ContainsKey (targetGoal.Key)) {
					state [targetGoal.Key] = targetGoal.Key.GetState ();
				}
				var targetState = state [targetGoal.Key];
				if (!DoConditionsApply (targetGoal.Value, targetState)) {
					return false;
				}
			}
			return true;
		}
			
		/// <summary>
		/// Apply the stateChange to the currentState.
		/// </summary>
		private static State ChangeState (State currentState, Effects stateChange)
		{
			// Copy state.
			var state = new State ();
			foreach (var item in currentState) {
				state [item.Key] = item.Value;
			}
			// Apply change.
			foreach (var change in stateChange) {
				if (!state.ContainsKey (change.Key)) {
					state [change.Key] = new StateValue (change.Value.value);
				} else {
					// Only copy StateValue when changing it.
					state [change.Key] = new StateValue (state [change.Key].value);
					state [change.Key].ApplyEffect (change.Value);
				}
			}
			return state;
		}

		/// <summary>
		/// Apply the stateChange to the currentState.
		/// </summary>
		private static WorldState ChangeWorldState (WorldState currentWorldState, WorldEffects stateChange, bool inPlace = false)
		{
			// Copy state.
			WorldState worldState = currentWorldState;
			if (!inPlace) {
				worldState = new WorldState ();
				foreach (var kvp in currentWorldState) {
					worldState [kvp.Key] = kvp.Value;
				}
			}
			// Apply change.
			foreach (var change in stateChange) {
				if (!worldState.ContainsKey (change.Key)) {
					worldState [change.Key] = change.Key.GetState ();
				}
				worldState [change.Key] = ChangeState (worldState [change.Key], change.Value);
			}
			return worldState;
		}

		private static Queue<GoapAction.WithContext> UnwrapPlan (Node endNode)
		{
			var result = new List<GoapAction.WithContext> ();
			var node = endNode;
			while (node.parent != null) {
				result.Add (node.action.Clone ());
				node = node.parent;
			}
			//result.Reverse ();
			return new Queue<GoapAction.WithContext> (result);
		}
			
		/// <summary>
		/// This is needed to deep-compare the states by value and not by reference.
		/// </summary>
		private class StateComparer : IEqualityComparer<State>
		{
			public static StateComparer instance = new StateComparer ();

			public bool Equals (State state1, State state2)
			{
				if (state1 == state2)
					return true;
				if ((state1 == null) || (state2 == null))
					return false;
				if (state1.Count != state2.Count)
					return false;

				foreach (var kvp in state1) {
					StateValue value2;
					if (!state2.TryGetValue (kvp.Key, out value2))
						return false;
					if (!kvp.Value.value.Equals (value2.value))
						return false;
				}
				return true;
			}

			public int GetHashCode (State obj)
			{
				return GoapAgent.PrettyPrint (obj).GetHashCode ();
			}
		}

		/// <summary>
		/// This is needed to deep-compare the states by value and not by reference.
		/// </summary>
		private class WorldStateComparer : IEqualityComparer<WorldState>
		{
			public static WorldStateComparer instance = new WorldStateComparer ();

			public bool Equals (WorldState state1, WorldState state2)
			{
				if (state1 == state2)
					return true;
				if ((state1 == null) || (state2 == null))
					return false;

				foreach (var kvp in state1) {
					State value2;
					if (!state2.TryGetValue (kvp.Key, out value2)) {
						state2 [kvp.Key] = kvp.Key.GetState ();
					}
					if (!StateComparer.instance.Equals (kvp.Value, value2))
						return false;
				}

				foreach (var kvp in state2) {
					State value1;
					if (!state1.TryGetValue (kvp.Key, out value1)) {
						state1 [kvp.Key] = kvp.Key.GetState ();
					}
					if (!StateComparer.instance.Equals (kvp.Value, value1))
						return false;
				}
				return true;
			}

			public int GetHashCode (WorldState obj)
			{
				return GoapAgent.PrettyPrint (obj).GetHashCode ();
			}
		}

		/// <summary>
		/// Used for building up the graph and holding the running costs of actions.
		/// </summary>
		private class Node : FastPriorityQueueNode
		{
			public static ObjectPool<Node> pool = new ObjectPool<Node> (100, 25);

			public Node parent;
			/// <summary>
			/// Cost to reach this node.
			/// </summary>
			public float runningCost;
			public float cachedHeuristicCost = float.NegativeInfinity;
			public WorldGoal goal;
			public WorldState state;
			public Vector2 position;
			public GoapAction.WithContext action = GoapAction.WithContext.pool.Borrow ();

			public float Score {
				get {
					return runningCost + cachedHeuristicCost;
				}
			}

			public void Init (Node parent, float runningCost, WorldGoal goal, GoapAction action, IStateful target)
			{
				this.parent = parent;
				this.runningCost = runningCost;
				this.goal = goal;
				this.state = state;
				this.action.Init (target, action);
			}

			public float CalculateHeuristicCost (WorldGoal goal, bool useCachedValue = true)
			{
				if (useCachedValue && cachedHeuristicCost >= 0f) {
					return cachedHeuristicCost;
				}
				cachedHeuristicCost = 0f;
				// TODO: Calculate heuristic for A*.
				return cachedHeuristicCost;
			}

			public string PathToString ()
			{
				var node = this;
				var s = string.Empty;
				while (node.parent != null) {
					s = GoapAgent.PrettyPrint (node.action) + ">" + s;
					node = node.parent;
				}
				return s;
			}
		}
	}
}
