using Ai.Goap;

//New Character

namespace TeamFirewood {
	public class Pirate : Worker {
		private readonly WorldGoal worldGoal = new WorldGoal();

		protected override void Awake() {
			base.Awake();
			var goal = new Goal();
			//yoel: maybe this needs to be phrased differently (hasTool / hasTreasure / hasItem & greaterThan etc.)

			//TODO: delete this goal: 
			goal[Item.Advice.ToString()] = new Condition(CompareType.MoreThanOrEqual, 1);

			//TODO: uncomment this goal:
			//goal["has" + Item.Treasure] = new Condition(CompareType.Equal, true);
			worldGoal[this] = goal;
		}

		public override WorldGoal CreateGoalState() {
			return worldGoal;
		}
	}
}
