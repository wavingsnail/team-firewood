using Infra.Utils;
using Ai.Goap;

namespace TeamFirewood {
	public class Door : PointOfInterest {
		private readonly State state = new State();

		protected override void Awake() {
			base.Awake();
		}

		public override State GetState() {
			// Enable to check again if has branches.
			enabled = true;
			return state;
		}

		protected void Update() {
			enabled = false;
		}
	}
}
