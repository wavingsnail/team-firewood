using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood {
	public class IncrementItemAction : GoapAction {
		public Item resource = Item.Strength;
		public List<Item> unlessHas = null;
		public int amountToIncrement;
		private List<IStateful> targets;

		protected virtual void Awake() {
			//yoel: no preconditions or target effects for pushups.
			if (unlessHas != null) {
				foreach (Item item in unlessHas) {
					AddPrecondition(item.ToString(), CompareType.Equal, false);	
				}
			}
			AddEffect(resource.ToString(), ModificationType.Add, amountToIncrement);
			base.Awake();
		}

		protected void Start() {
			targets = GetTargets<PointOfInterest>(); //TODO: actually no need for target..
		}

		public override bool RequiresInRange() {
			return false;
		}

		public override List<IStateful> GetAllTargets(GoapAgent agent) {
			return targets;
		}

		protected override bool OnDone(GoapAgent agent, WithContext context) {
			// Done harvesting.
			var backpack = agent.GetComponent<Container>();
			backpack.items[resource] += amountToIncrement;
			return base.OnDone(agent, context);
		}
	}
}