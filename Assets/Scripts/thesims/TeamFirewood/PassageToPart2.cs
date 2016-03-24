using UnityEngine;
using Infra.Utils;
using Ai.Goap;

namespace TeamFirewood {
	/// <summary>
	/// A point of interest where resources can be collected.
	/// </summary>
	[RequireComponent(typeof(Container))]
	public class PassageToPart2 : PointOfInterest {

		private readonly State worldData = new State();

		protected override void Awake() {
			base.Awake();
		}

		protected void Start() {
		}

		public override State GetState() {
			return worldData;
		}
	}
}
