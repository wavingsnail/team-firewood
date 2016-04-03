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

		public Condition reverseApplyEffectToCondition(Condition cond){

			switch(this.modifier){
				
			case ModificationType.Set:
				return null; //it could have been anything before
			case ModificationType.Add:
				return new Condition (cond.comparison, (int)cond.value - (int)this.value);
			default:
				return null;
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
			foreach (KeyValuePair<IStateful, Goal> agentGoal in possibleGoal) {
				Goal possible = agentGoal.Value;
				// TODO: what if possible goal doesnt contain this person
				if (!currentGoal.ContainsKey (agentGoal.Key)) {
					continue;
				}
				else{
					Goal current = currentGoal [agentGoal.Key];

					//go through parent node's conditions
					foreach (KeyValuePair<string, Condition> kvp in current) {

						// If initial state without this parameter, irrelevant and continue
						if(this.ContainsKey (kvp.Key)) {
							
							// If child node doesnt contain this goal, it is satisfied!
							if (!possible.ContainsKey (kvp.Key)) {res = true;} 

							// Else - check if child goal is closer to inital state
							else {
								StateValue sv = this [kvp.Key];
								Condition currCond = kvp.Value; 
								Condition possCond = possible [kvp.Key];
								//if int check if new closer to wanted
								if (sv.value.GetType () == typeof(int)) {
									if (sv.CheckCondition (currCond) && sv.CheckCondition (possCond)) {
										continue;
									}
									if (sv.CheckCondition (currCond) && !sv.CheckCondition (possCond)) {
										return false;
									}
									if (!sv.CheckCondition (currCond) && sv.CheckCondition (possCond)) {
										res = true;
										continue;
									}

									//now both conditions dont hold, check if possCond improves
									//assume both goals have same compare type
									switch (currCond.comparison) {
									case CompareType.Equal:
										res = (Mathf.Abs ((int)sv.value - (int)currCond.value)) < (Mathf.Abs ((int)sv.value - (int)possCond.value));
										break;
									case CompareType.LessThan:
										res = ((int)possCond.value - (int)sv.value) > ((int)currCond.value - (int)sv.value);
										break;
									case CompareType.LessThanOrEqual:
										res = ((int)possCond.value - (int)sv.value) > ((int)currCond.value - (int)sv.value);
										break;
									case CompareType.MoreThan:
										res = ((int)possCond.value - (int)sv.value) < ((int)currCond.value - (int)sv.value);
										break;
									case CompareType.MoreThanOrEqual:
										res = ((int)possCond.value - (int)sv.value) < ((int)currCond.value - (int)sv.value);
										break;
									case CompareType.NotEqual:
										res = (Mathf.Abs ((int)sv.value - (int)currCond.value)) > (Mathf.Abs ((int)sv.value - (int)possCond.value));
										break;
									}

									//if bool check if new matches while old doesnt
								} else if (sv.value.GetType () == typeof(bool)) {
									//assuming booleans will only get ModificationType.Set
									res = (sv.value != currCond.value && sv.value == possCond.value);
								}

								//if new cond worse - return false
								if (res == false) {
									//Debug.Log ("this Goal isnt any better! abort!");
									return false;
								}
							}
						}
					}
				} 
			}
			if (res == false) {
				//Debug.Log ("this Goal isnt any better! abort!");
			} else {
				//Debug.Log ("this Goal is better!");
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

	public class Goal : Dictionary<string, Condition> {

		public static ObjectPool<Goal> pool = new ObjectPool<Goal>(1, 1);

		public Goal (Goal other) : base(){
			foreach (KeyValuePair<string, Condition> kvp in other) {
				this.Add (kvp.Key, new Condition(kvp.Value.comparison, kvp.Value.value)); // For deep copy
			}
		}

		public Goal () : base(){
		}

	}

	/// <summary>
	/// A dictionary of stateful objects and the goal we want them to be at.
	/// </summary>
	public class WorldGoal : Dictionary<IStateful, Goal> {

		public static ObjectPool<WorldGoal> pool = new ObjectPool<WorldGoal>(1, 1);

		public WorldGoal (WorldGoal other) : base(){
			foreach (KeyValuePair<IStateful, Goal> kvp in other) {
				Goal goalCopy = new Goal (kvp.Value); // For deep copy
				this.Add (kvp.Key, goalCopy);
			}
		}

		public WorldGoal () : base(){
		}

	}

	public class Effects : Dictionary<string, Effect> {

		public Goal reverseApplyEffectsToGoal(Goal goal){

			//Start with empty new goal, fill it with reversed conditions.
			Goal newGoal = Goal.pool.Borrow ();
			newGoal.Clear ();

			List<string> keys = new List<string> (goal.Keys);
			foreach(string k in keys){
				
				if (this.ContainsKey (k)) {
					Condition possibleNewCond = this [k].reverseApplyEffectToCondition (goal [k]);	

					//If it's null means condition filled by action.
					if (possibleNewCond != null) {
						newGoal.Add (k, possibleNewCond);
					}
				} else {
					//Debug.Log (k + " unchanged by this action. keep it as is in new goal.");
					Condition newCond = new Condition (goal [k].comparison, goal [k].value);
					newGoal.Add (k, newCond);
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
