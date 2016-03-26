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
		
		//New modifiers
		Subtract, 
		Truncate
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

	public class State : Dictionary<string, StateValue> {}

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

	public class Effects : Dictionary<string, Effect> {}

	/// <summary>
	/// A dictionary of stateful objects and how an action might change their state.
	/// </summary>
	public class WorldEffects : Dictionary<IStateful, Effects> {}

}
