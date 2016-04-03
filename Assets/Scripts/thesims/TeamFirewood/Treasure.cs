using Infra.Utils;
using Ai.Goap;

namespace TeamFirewood {
	public class Treasure : PointOfInterest {
		private readonly State state = new State();

		protected override void Awake() {
			state[Item.Treasure.ToString()] = new StateValue(1);
			base.Awake();
		}

		public override State GetState() {
			return state;
		}

		protected void Update() {
		}
	}
}
