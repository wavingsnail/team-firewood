using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood {
public class CraftItemAction : BasicCraftItemAction {
    public float toolDamage;
    public PointOfInterestType craftingStation;
    private List<IStateful> targets;

    protected override void Awake() {
        AddPrecondition("hasTool", CompareType.Equal, true);
        base.Awake();
    }

    protected void Start() {
        targets = PointOfInterest.GetPointOfInterest(craftingStation);
    }
    
    public override bool RequiresInRange() {
        return true;
    }

    public override List<IStateful> GetAllTargets(GoapAgent agent) {
        return targets;
    }

    protected override bool OnDone(GoapAgent agent, WithContext context) {
        var inventory = agent.GetComponent<Container>();
        var tool = inventory.tool.GetComponent<ToolComponent>();
        tool.use(toolDamage);
        if (tool.destroyed()) {
            Destroy(inventory.tool);
            inventory.tool = null;
        }
        return base.OnDone(agent, context);
    }
}
}
