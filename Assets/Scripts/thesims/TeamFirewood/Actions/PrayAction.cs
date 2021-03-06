﻿using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood {
	public class PrayAction: GoapAction {
		public Item Achievement;
		public Item resource;
		public PointOfInterestType meditationPoint;
		public int neededAmount;
		private List<IStateful> targets;

		protected virtual void Awake() {
//			AddPrecondition("mushroomsAmount", CompareType.Equal, neededAmount);
			AddPrecondition(resource.ToString(), CompareType.MoreThanOrEqual, neededAmount);
			AddEffect(Achievement.ToString(), ModificationType.Set, true);
		}

		protected void Start() {
			// TODO: Use HarvestPoint and have resources like trees and rocks deplete
			//       over time as they are being consumed.
			targets = PointOfInterest.GetPointOfInterest(meditationPoint);
		}

		public override bool RequiresInRange() {
			return true;
		}

		public override List<IStateful> GetAllTargets(GoapAgent agent) {
			return targets;
		}

		protected override bool OnDone(GoapAgent agent, WithContext context) {
			// Done harvesting.
			var inventory = agent.GetComponent<Container>();
			inventory.items[resource] -= neededAmount;
			return base.OnDone(agent, context);
		}
	}
}
