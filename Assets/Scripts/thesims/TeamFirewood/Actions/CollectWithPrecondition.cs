using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood
{
	public class CollectWithPrecondition : GoapAction
	{
		public Item required;
		public Item resource;
		public PointOfInterestType collectPOI;
		public int amountToCollect;
		private List<IStateful> targets;

		protected virtual void Awake ()
		{
			AddPrecondition (required.ToString (), CompareType.MoreThanOrEqual, 1);
			AddEffect (resource.ToString(), ModificationType.Add, amountToCollect);
		}

		protected void Start ()
		{
			// TODO: Use HarvestPoint and have resources like trees and rocks deplete
			//       over time as they are being consumed.
			targets = PointOfInterest.GetPointOfInterest (collectPOI);
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
			var backpack = agent.GetComponent<Container> ();
			backpack.items [resource] += amountToCollect;
			return base.OnDone (agent, context);
		}
	}
}
