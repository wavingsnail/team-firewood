using UnityEngine;
using System;
using System.Collections.Generic;

namespace Ai.Goap {
/// <summary>
/// A target for an action.
/// Maintains its state and can be acted upon.
/// </summary>
public abstract class ActionTarget : MonoBehaviour, IStateful {
    [Serializable]
    public class ActionEffect {
        public GoapAction actionOnTarget;
        public GoapAction effect;
    }
    public ActionEffect[] _actionEffects;
    public Dictionary<GoapAction, GoapAction> actionEffects = new Dictionary<GoapAction, GoapAction>();

    protected virtual void Awake() {
        foreach (var item in _actionEffects) {
            SetActionEffect(item.actionOnTarget, item.effect);
        }
    }

    /// <summary>
    /// An action's effect's preconditions must apply to this object in order
    /// for the action to be applied. Also, the action's effect's target
    /// preconditions must apply to the agent.
    /// </summary>
    public void SetActionEffect(GoapAction actionOnTarget, GoapAction effect) {
        actionEffects[actionOnTarget] = effect;
    }

    public void RemoveActionEffect(GoapAction action) {
        actionEffects.Remove(action);
    }

    public abstract State GetState();
}
}
