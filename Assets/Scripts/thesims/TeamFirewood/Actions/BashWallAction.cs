using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood {
	public class BashWallAction : GoapAction {

		public int strengthRequired = 5;

		private List<IStateful> targets;

		protected void Awake() {
			AddPrecondition(Item.Strength.ToString(), CompareType.MoreThanOrEqual, strengthRequired);	
			AddEffect(Item.Part2.ToString(), ModificationType.Add, 1);
		}

		protected void Start() {
			targets = GetTargets<Wall>();
		}

		public override bool RequiresInRange() {
			return true;
		}

		public override List<IStateful> GetAllTargets(GoapAgent agent) {
			return targets;
		}

		protected override bool OnDone(GoapAgent agent, WithContext context) {
			// Done harvesting.
			var backpack = agent.GetComponent<Container>();
			backpack.items[Item.Part2] += 1;
			return base.OnDone(agent, context);
		}
	}
}
