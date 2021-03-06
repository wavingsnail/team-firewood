using System;
using Ai.Goap;

namespace TeamFirewood {
public class BasicCraftItemAction : GoapAction {
    [Serializable]
    public class ItemCount {
        public Item item;
        public int amount;
    }

    public ItemCount[] ingredients;
    public ItemCount[] products;

    protected virtual void Awake() {
        foreach (var item in ingredients) {
            AddPrecondition(item.item.ToString(), CompareType.MoreThanOrEqual, item.amount);
            AddEffect(item.item.ToString(), ModificationType.Add, -item.amount);
        }
        foreach (var item in products) {
            AddPrecondition(item.item.ToString(), CompareType.LessThan, item.amount);
            AddEffect(item.item.ToString(), ModificationType.Add, item.amount);
        }
    }
    
    public override bool RequiresInRange() {
        return false;
    }

    protected override bool OnDone(GoapAgent agent, WithContext context) {
        var inventory = agent.GetComponent<Container>();
        foreach (var item in ingredients) {
            inventory.items[item.item] -= item.amount;
        }
        foreach (var item in products) {
            inventory.items[item.item] += item.amount;
        }
        return base.OnDone(agent, context);
    }
}
}
