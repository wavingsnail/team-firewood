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
				int number = val ? 1 : 0;
				state[item.ToString()] = new StateValue(number);
				Debug.LogError (val);
			}
			base.Awake();
		}

		public void removeItem (Item item){
			state[item.ToString()] = new StateValue(0);
		}

		public void setSearched(bool searchedHere){
			state ["searchedHere"] = new StateValue(searchedHere);
		}

		public override State GetState() {
			return state;
		}

		public State GetPerceivedState (){
			
			if ((bool)state ["searchedHere"].value) {
				return state;
			} else {
				State perceived = new State ();
				perceived ["searchedHere"] = state ["searchedHere"];
				foreach(Item item in possibleItemsInPile){
					perceived[item.ToString()] = new StateValue(1);
				}
				return perceived;
			}


		}

		protected void Update() {
		}
	}
}
