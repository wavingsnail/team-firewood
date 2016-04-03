using UnityEngine;
using System.Collections.Generic;
using Infra.Utils;
using Ai.Goap;

namespace TeamFirewood {
/**
 * A general labourer class.
 * You should subclass this for specific Labourer classes and implement
 * the createGoalState() method that will populate the goal for the GOAP
 * planner.
 */
public abstract class Worker : GoapAgent {
    public Container inventory;
    public float moveSpeed = 1;
    private readonly State state = new State();


    protected override void Awake() {
        base.Awake();

        foreach (var item in EnumUtils.EnumValues<Item>()) {
            if (item == Item.None) continue;
			state[item.ToString()] = new StateValue(inventory.items[item]);
        }
        state["hasTool"] = new StateValue(inventory.tool != null);
    }

    protected void Start() {
        if (inventory == null) {
            inventory = gameObject.GetComponent<Container>();
        }
        if (inventory.tool == null) {
            var prefab = Resources.Load<GameObject>(inventory.toolType);
            var tool = Instantiate(prefab, transform.position, transform.rotation) as GameObject;
            inventory.tool = tool;
            tool.transform.parent = transform;
        }
    }

    public override State GetState() {
        foreach (var item in EnumUtils.EnumValues<Item>()) {
            if (item == Item.None) continue;
            state[item.ToString()].value = inventory.items[item];
        }
        state["hasTool"].value = inventory.tool != null;
        state["x"] = new StateValue(transform.position.x);
        state["y"] = new StateValue(transform.position.y);

        return state;
    }


    public override void PlanFailed(WorldGoal failedGoal) {
        // Not handling this here since we are making sure our goals will always
        // succeed.
        // TODO: Make sure the world state has changed before running the same
        //       goal again.
        // TODO: Support multiple goals and select the next one.
    }

    public override void PlanFound(WorldGoal goal, Queue<GoapAction.WithContext> actions) {
        // Yay we found a plan for our goal
        Debug.Log("<color=green>Plan found</color> " + GoapAgent.PrettyPrint(actions));
    }

    public override void ActionsFinished() {
        // Everything is done, we completed our actions for this gool. Hooray!
        Debug.Log("<color=blue>Actions completed</color>");
    }

    public override void PlanAborted(GoapAction.WithContext aborter) {
        // An action bailed out of the plan. State has been reset to plan again.
        // Take note of what happened and make sure if you run the same goal again
        // that it can succeed.
        Debug.Log("<color=red>Plan Aborted</color> " + GoapAgent.PrettyPrint(aborter));
    }

    public override bool MoveAgent(GoapAction.WithContext nextAction) {
        // Move towards the NextAction's target.
        float step = moveSpeed * Time.deltaTime;
        var target = nextAction.target as Component;
        Vector2 position = target.transform.position;
        // TODO: Move by setting the velocity of a rigid body to allow collisions.
        gameObject.transform.position = Vector2.MoveTowards(gameObject.transform.position, position, step);
        
        if (position.Approximately((Vector2)gameObject.transform.position)) {
            // We are at the target location, we are done.
            nextAction.isInRange = true;
            return true;
        }
        return false;
    }
}
}
