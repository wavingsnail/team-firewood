using Ai.Goap;

//New Character

namespace TeamFirewood {
	public class Pirate : Worker {
		private readonly WorldGoal worldGoal = new WorldGoal();

		protected override void Awake() {
			base.Awake();
			var goal = new Goal();
			//yoel: maybe this needs to be phrased differently (hasTool / hasTreasure / hasItem & greaterThan etc.)
			goal["has" + Item.Treasure] = new Condition(CompareType.Equal, true);
			worldGoal[this] = goal;
		}

		public override WorldGoal CreateGoalState() {
			return worldGoal;
		}
	}
}
