using UnityEngine;
using System;
using System.Collections.Generic;
using Infra;
using Infra.Collections;

namespace Ai.Goap {
	public enum CompareType {
	    Equal,
	    NotEqual,
	    MoreThan,
	    MoreThanOrEqual,
	    LessThan,
	    LessThanOrEqual,
	}

	public enum ModificationType {
	    Set,
	    // Add is supported only for ints.
	    Add,
	}

	/// <summary>
	/// A condition that a StateValue can meet.
	/// </summary>
	public class Condition {
	    public CompareType comparison;
	    public object value;

	    public Condition() {}
	    public Condition(CompareType comparison, object value) {
	        this.comparison = comparison;
	        this.value = StateValue.NormalizeValue(value);
	    }
	}

	/// <summary>
	/// Can be applied to change a StateValue.
	/// </summary>
	public class Effect {
	    public ModificationType modifier;
	    public object value;

	    public Effect() {}
	    public Effect(ModificationType modifier, object value) {
	        this.modifier = modifier;
	        this.value = StateValue.NormalizeValue(value);
	    }

		public Condition applyEffectToCondition(Condition cond){

			switch(this.modifier){
				
			case ModificationType.Set:
				return new Condition (CompareType.Equal, cond.value);
				break;
			case ModificationType.Add:
				return new Condition (cond.comparison, cond.value + this.value);
				break;
			}
		}
	}

	public class StateValue {
	    public object value;

	    public StateValue() {}
	    /// <summary>
	    /// Supports int or bool values.
	    /// </summary>
	    public StateValue(object value) {
	        this.value = NormalizeValue(value);
	    }

	    public static object NormalizeValue(object value) {
	        if (value is float) {
	            return Mathf.FloorToInt((float)value);
	        }
	        return value;
	    }

	    public void ApplyEffect(Effect e) {
	        switch (e.modifier) {
	        case ModificationType.Set:
	            value = e.value;
	            break;
	        case ModificationType.Add:
	            var v1 = (int)e.value;
	            var v2 = (int)value;
	            value = v2 + v1;
	            break;
			
			//New effects
			case ModificationType.Subtract:
				var v3 = (int)e.value;
				var v4 = (int)value;
				value = v4 - v3;
				break;
			case ModificationType.Truncate:
				value = 0;
				break;
	        }
	    }

	    public bool CheckCondition(Condition c) {
	        double v1;
	        double v2;
	        switch (c.comparison) {
	        case CompareType.Equal:
	            return value.Equals(c.value);
	        case CompareType.NotEqual:
	            return !value.Equals(c.value);
	        case CompareType.MoreThan:
	            v1 = (int)c.value;
	            v2 = (int)value;
	            return v2 > v1;
	        case CompareType.MoreThanOrEqual:
	            v1 = (int)c.value;
	            v2 = (int)value;
	            return v2 > v1 || value.Equals(c.value);
	        case CompareType.LessThan:
	            v1 = (int)c.value;
	            v2 = (int)value;
	            return v2 <= v1;
	        case CompareType.LessThanOrEqual:
	            v1 = (int)c.value;
	            v2 = (int)value;
	            return v2 <= v1 || value.Equals(c.value);
	        }
	        return false;
	    }
	}

	// TODO: Pool more stuff like we do with the WorldState and GoapAction.WithContext.
	//       Be careful not to leak anything!

	public class State : Dictionary<string, StateValue> {

		public bool isGoalCloser(WorldGoal currentGoal, WorldGoal possibleGoal){
			bool res = false;

			foreach (KeyValuePair<GoapAgent, Goal> agentGoal in possibleGoal) {

				Goal possible = agentGoal [1];
				if (currentGoal.ContainsKey (agentGoal [0])) {
					Goal current = currentGoal [agentGoal [0]];

					foreach (KeyValuePair<string, Condition> kvp in possible) {
						if (this.ContainsKey (kvp [0]) && current.ContainsKey (kvp[0])) {
							StateValue sv = this [kvp [0]];
							Condition currCond = current [kvp [0]];
							Condition possCond = kvp [1];

							//if int check if new closer to wanted
							if (sv.value.GetType () == typeof(int)) {


								if (sv.CheckCondition (currCond) && sv.CheckCondition (possCond)) {
									continue;
								}
								if (sv.CheckCondition (currCond) && !sv.CheckCondition (possCond)) {
									return false;
								}

								//now both conditions dont hold, check if possCond improves
								//assume both goals have same compare type
								switch(currCond.comparison){
								case CompareType.Equal:
									res = (Mathf.Abs(sv.value - currCond.value)) < (Mathf.Abs(sv.value - possCond.value));
									break;
								case CompareType.LessThan:
									res = (possCond.value - sv.value) < (currCond.value - sv.value);
									break;
								case CompareType.LessThanOrEqual:
									res = (possCond.value - sv.value) <= (currCond.value - sv.value);
									break;
								case CompareType.MoreThan:
									res = (possCond.value - sv.value) > (currCond.value - sv.value);
									break;
								case CompareType.MoreThanOrEqual:
									res = (possCond.value - sv.value) >= (currCond.value - sv.value);
									break;
								case CompareType.NotEqual:
									res = (Mathf.Abs(sv.value - currCond.value)) > (Mathf.Abs(sv.value - possCond.value));
									break;
								}

								//if bool check if new matches while old doesnt
							} else if (sv.value.GetType () == typeof(bool)) {
								//assuming booleans will only get ModificationType.Set
								res = (sv.value != currCond.value && sv.value == possCond.value);
							}

							//if new cond worse - return false
							if (res == false) {
								return false;
							}
						}
					}


				} else {
					continue;
				}
				
			}


			return res;
		}
	}

	/// <summary>
	/// A dictionary of stateful objects and their state.
	/// </summary>
	public class WorldState : Dictionary<IStateful, State> {
	    public static ObjectPool<WorldState> pool = new ObjectPool<WorldState>(1, 1);
	}

	public class Goal : Dictionary<string, Condition> {}

	/// <summary>
	/// A dictionary of stateful objects and the goal we want them to be at.
	/// </summary>
	public class WorldGoal : Dictionary<IStateful, Goal> {}

	public class Effects : Dictionary<string, Effect> {

		public Goal applyEffectsToGoal(Goal goal){
			Goal newGoal = (Goal)new Dictionary<string, Condition>(goal);
			foreach (KeyValuePair<string, Condition> kvp in newGoal) {
				if (this.ContainsKey (kvp [0])) {
					newGoal [kvp [0]] = this [kvp [0]].applyEffectToCondition (goal[kvp[0]]);	
				}
			}
			return newGoal;
		}
	}

	/// <summary>
	/// A dictionary of stateful objects and how an action might change their state.
	/// </summary>
	public class WorldEffects : Dictionary<IStateful, Effects> {}

}
