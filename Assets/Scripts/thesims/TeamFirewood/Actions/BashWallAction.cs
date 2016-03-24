﻿using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood {
	public class BashWallAction : GoapAction {

		public int strengthRequired = 5;

		private List<IStateful> targets;

		protected void Awake() {
			// TODO: Add a way to reference target state values in effects. For example,
			//       add all of the agent's items to the state of the target or
			//       vice versa.
			AddPrecondition(Item.Strength.ToString(), CompareType.MoreThanOrEqual, strengthRequired);	
			AddEffect("inPart2", ModificationType.Set, true);
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
			return base.OnDone(agent, context);
		}
	}
}