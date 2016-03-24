using Infra.Utils;
using Ai.Goap;

namespace TeamFirewood {
	public class MushroomsPile : PointOfInterest {
		private readonly State state = new State();

		protected override void Awake() {
			state["has" + Item.Mushrooms] = new StateValue(RandomUtils.RandBool(0.5f));
			base.Awake();
		}

		public override State GetState() {
			// Enable to check again if has branches.
			enabled = true;
			return state;
		}

		protected void Update() {
			state["has" + Item.Mushrooms].value = RandomUtils.RandBool(0.5f);
			enabled = false;
		}
	}
}
