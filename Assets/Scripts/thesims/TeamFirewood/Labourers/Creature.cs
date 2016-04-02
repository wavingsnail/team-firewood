using Ai.Goap;

//New Character


namespace TeamFirewood {
	public class Creature : Worker {
		private readonly WorldGoal worldGoal = new WorldGoal();
			
		protected override void Awake() {
			base.Awake();
			var goal = new Goal();
			//yoel: maybe this needs to be phrased differently (hasTool / hasNirvana / hasItem & greaterThan etc.)
			goal[Item.Nirvana.ToString()] = new Condition(CompareType.MoreThanOrEqual, 1);
			worldGoal[this] = goal;
		}

		public override WorldGoal CreateGoalState() {
			return worldGoal;
		}
	}
}
