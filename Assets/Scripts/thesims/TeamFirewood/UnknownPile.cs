using UnityEngine;
using Infra.Utils;
using System.Collections.Generic;
using Ai.Goap;

namespace TeamFirewood {
	public class UnknownPile : PointOfInterest {

		public List<Item> possibleItemsInPile;
		private readonly State state = new State();

		protected override void Awake() {
			state ["searchedHere"] = new StateValue(false);
			foreach(Item item in possibleItemsInPile){
				bool val = RandomUtils.RandBool (0.2f);
				state["has" + item] = new StateValue(val);
				Debug.LogError (val);
			}
			base.Awake();
		}

		public override State GetState() {
			// Enable to check again if has branches.
			enabled = true;
			return state;
		}

		protected void Update() {
		}
	}
}
