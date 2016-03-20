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
        var backpack = agent.GetComponent<Container>();
        var tool = backpack.tool.GetComponent<ToolComponent>();
        tool.use(toolDamage);
        if (tool.destroyed()) {
            Destroy(backpack.tool);
            backpack.tool = null;
        }
        return base.OnDone(agent, context);
    }
}
}
