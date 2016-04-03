using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood
{
	public class CollectAction : GoapAction
	{
		//    public float toolDamage;
		public Item resource;
		public PointOfInterestType harvestSource;
		public int amountToCollect;
		private List<IStateful> targets;

		protected virtual void Awake ()
		{
//			AddPrecondition (resource.ToString (), CompareType.LessThan, maxAmountToCarry);
			AddEffect (resource.ToString(), ModificationType.Add, amountToCollect);
		}

		protected void Start ()
		{
			// TODO: Use HarvestPoint and have resources like trees and rocks deplete
			//       over time as they are being consumed.
			targets = PointOfInterest.GetPointOfInterest (harvestSource);
		}

		public override bool RequiresInRange ()
		{
			return true;
		}

		public override List<IStateful> GetAllTargets (GoapAgent agent)
		{
			return targets;
		}

		protected override bool OnDone (GoapAgent agent, WithContext context)
		{
			// Done harvesting.
			var inventory = agent.GetComponent<Container> ();
			inventory.items [resource] += amountToCollect;
			return base.OnDone (agent, context);
		}
	}
}
