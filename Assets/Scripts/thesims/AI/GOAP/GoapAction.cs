using UnityEngine;
using System;
using System.Collections.Generic;
using Infra;
using Infra.Collections;

namespace Ai.Goap {
	public abstract class GoapAction : MonoBehaviour {
	    /// <summary>
	    /// The cost of performing the action.
	    /// </summary>
	    [Tooltip("The cost of performing the action")]
	    public float cost = 1f;
	    public float workDuration;

		public static String actionName = "None";
		public String setStaticActionName = "None";

	    private Goal preconditions = new Goal();
	    private Goal targetPreconditions = new Goal();
	    private readonly Effects effects = new Effects();
	    private readonly Effects targetEffects = new Effects();

		protected void Awake(){
			actionName = setStaticActionName;
		}
	
	    /// <summary>
	    /// Default implementation returns the agent as the target.
	    /// </summary>
	    public virtual List<IStateful> GetAllTargets(GoapAgent agent) {
	        var targets = new List<IStateful>();
	        targets.Add(agent);
	        return targets;
	    }

	    /// <summary>
	    /// Returns false if the action can not be performed.
	    /// </summary>
	    protected virtual bool OnDone(GoapAgent agent, WithContext context) {
	        context.isDone = true;
	        DebugUtils.Log(GetType().Name + " DONE!");
	        return true;
	    }

	    protected virtual bool CanDoNow(GoapAgent agent, IStateful target) {
	        var worldState = WorldState.pool.Borrow();
	        worldState.Clear();
	        worldState[agent] = agent.GetState();
	        worldState[target] = target.GetState();
	        var conditions = GetDependentPreconditions(agent, target);
	        bool result = GoapPlanner.DoConditionsApplyToWorld(conditions, worldState);
	        WorldState.pool.Return(worldState);
	        return result;
	    }

	    /// <summary>
	    /// Does this action need to be within range of a target game object?
	    /// If not then the moveTo state will not need to run for this action.
	    /// </summary>
	    public abstract bool RequiresInRange();

	    public void AddPrecondition(string key, CompareType comparison, object value) {
	        preconditions[key] = new Condition(comparison, value);
	    }

	    public void AddTargetPrecondition(string key, CompareType comparison, object value) {
	        targetPreconditions[key] = new Condition(comparison, value);
	    }

	    public void AddEffect(string key, ModificationType modifier, object value) {
	        effects[key] = new Effect(modifier, value);
	    }

	    public void AddTargetEffect(string key, ModificationType modifier, object value) {
	        targetEffects[key] = new Effect(modifier, value);
	    }

	    public static List<IStateful> GetTargets<T>() where T : Component, IStateful {
	        var candidates = FindObjectsOfType<T>();
	        var list = new List<IStateful>(candidates.Length);
	        foreach (var item in candidates) {
	            list.Add(item);
	        }
	        return list;
	    }

	    /// <summary>
	    /// Returns a WorldGoal that contains all the preconditions that the agent
	    /// must satisfy.
	    /// </summary>
	    public virtual WorldGoal GetIndependentPreconditions(IStateful agent) {
	        var worldPreconditions = new WorldGoal();
	        if (preconditions.Count > 0) {
	            worldPreconditions[agent] = preconditions;
	        }
	        return worldPreconditions;
	    }

	    /// <summary>
	    /// Returns a WorldGoal that contains all the preconditions that the target
	    /// of the action must satisfy.
	    /// </summary>
	    public virtual WorldGoal GetDependentPreconditions(IStateful agent, IStateful target) {
	        var worldPreconditions = new WorldGoal();
	        if (targetPreconditions.Count > 0) {
	            worldPreconditions[target] = targetPreconditions;
	        }
	        return worldPreconditions;
	    }

	    /// <summary>
	    /// Returns the effects that should be applied to the agent and the target
	    /// of the action.
	    /// </summary>
	    public virtual WorldEffects GetDependentEffects(IStateful agent, IStateful target) {
	        var worldEffects = new WorldEffects();
	        if (effects.Count > 0) {
	            worldEffects[agent] = effects;
	        }
	        if (targetEffects.Count > 0) {
	            worldEffects[target] = targetEffects;
	        }
	        return worldEffects;
	    }

		public virtual WorldEffects GetEffectsOnAgent(IStateful agent) {
			var worldEffects = new WorldEffects();
			if (effects.Count > 0) {
				worldEffects[agent] = effects;
			}
			return worldEffects;
		}

		public WorldGoal applyToWorldGoal(WorldGoal goal){

			WorldGoal newGoal = (WorldGoal) new Dictionary<IStateful, Goal> (goal);

			foreach (KeyValuePair<IStateful, Goal> agentGoal in goal) {
				GoapAgent currAgent = (GoapAgent)agentGoal.Key;
				Goal currGoal = agentGoal.Value; 
				newGoal [currAgent] = this.effects.applyEffectsToGoal (currGoal);
			}

			return newGoal;
		}

		/// <summary>
		/// Reverses the action.
		/// </summary>
		/// <returns>The action.</returns>
		public GoapAction ReverseAction(){
			GoapAction reversed;
			reversed = this;

			foreach (KeyValuePair<string, Effect> effect in reversed.effects) {
				// TODO change to the opposite 
				if (effect.Value.modifier == ModificationType.Add) {
					effect.Value = Effect (ModificationType.Add, -effect.Value.value);
				} else if (effect.Value.modifier == ModificationType.Set) {
					effect.Value = Effect (ModificationType.Add, !effect.Value.value);
				}
			}

			foreach (KeyValuePair<string, Effect> targetEffect in reversed.targetEffects) {
				// TODO change to the opposite 
				if (targetEffect.Value.modifier == ModificationType.Add) {
					targetEffect.Value = Effect (ModificationType.Add, -targetEffect.Value.value);
				} else if (targetEffect.Value.modifier == ModificationType.Set) {
					targetEffect.Value = Effect (ModificationType.Add, !targetEffect.Value.value);
				}
			}

			return reversed;
		}


	    [Serializable]
	    public class WithContext {
	        public static ObjectPool<WithContext> pool = new ObjectPool<WithContext>(100, 25);

	        public GoapAction actionData;
	        /// <summary>
	        /// The target the action acts upon. Optional.
	        /// </summary>
	        [Tooltip("The target the action acts upon. Optional")]
	        public IStateful target;

	        private float startTime = 0;

	        /// <summary>
	        /// Are we in range of the target?
	        /// The MoveTo state will set this and it gets reset each time this action is performed.
	        /// </summary>
	        public bool isInRange;

	        public bool isDone;

	        public void Init(IStateful target, GoapAction action) {
	            actionData = action;
	            this.target = target;
	            startTime = 0;
	            isInRange = false;
	            isDone = false;
	        }



	        public WithContext Clone() {
	            var clone = pool.Borrow();
	            clone.Init(target, actionData);
	            return clone;
	        }

	        /// <summary>
	        /// Run the action.
	        /// Returns True if the action performed successfully or false
	        /// if something happened and it can no longer perform. In this case
	        /// the action queue should clear out and the goal cannot be reached.
	        /// </summary>
	        public bool Perform(GoapAgent agent) {
				
				SpriteRenderer sr = agent.transform.GetChild (0).GetComponent<SpriteRenderer> ();
				if (agent.actionImages.ContainsKey (actionName)) {
					sr.sprite = agent.actionImages [actionName];
				}

	            if (Mathf.Approximately(startTime, 0f)) {
	                if (!actionData.CanDoNow(agent, target)) {
	                    return false;
	                }

	                startTime = Time.time;
	            }

	            if (Time.time - startTime > actionData.workDuration) {
					sr.sprite = agent.actionImages ["Default"];
	                DebugUtils.Log(GetType().Name + " Time is up - am I done?");
	                return actionData.OnDone(agent, this);
	            }
	            return true;
	        }
	    }


	}
}