using UnityEngine;
using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood {
	public class TryToFindAction : GoapAction {
		public Item item;
		public PointOfInterestType whereToLook;
		private List<IStateful> targets;	

		protected virtual void Awake() {
			//AddPrecondition(item.ToString(), CompareType.Equal, 0);
			AddEffect(item.ToString(), ModificationType.Add, 1);

			AddTargetPrecondition (item.ToString(), CompareType.Equal, 0);
			AddTargetEffect (item.ToString(), ModificationType.Add, -1);

			AddTargetPrecondition ("searchedHere", CompareType.Equal, false);
			AddTargetEffect ("searchedHere", ModificationType.Set, true);

		}

		protected void Start() {
			// TODO: Use HarvestPoint and have resources like trees and rocks deplete
			//       over time as they are being consumed.
			targets = PointOfInterest.GetPointOfInterest(whereToLook);
		}

		public override bool RequiresInRange() {
			return true;
		}

		public override List<IStateful> GetAllTargets(GoapAgent agent) {
			return targets;
		}

		protected override bool OnDone(GoapAgent agent, WithContext context) {
			
			var target = context.target as UnknownPile;
			target.removeItem (item);
			target.setSearched (true);

			// Done harvesting.
			var backpack = agent.GetComponent<Container>();
			backpack.items[item] += 1;

			return base.OnDone(agent, context);
		}
	}
}
