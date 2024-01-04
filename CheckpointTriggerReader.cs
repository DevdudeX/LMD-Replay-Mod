using UnityEngine;
using MelonLoader;

namespace ReplayMod
{
	[RegisterTypeInIl2Cpp]
	public class CheckpointTriggerReader : MonoBehaviour
	{
		public CheckpointTriggerReader(IntPtr ptr) : base(ptr) { }

		public ReplayTool replayToolScript;

		public void OnTriggerEnter(Collider other)
		{
			Debug.Log($"OnTriggerEnter! {this.gameObject.name}, Other: {other.gameObject.name}");
			if (replayToolScript != null) {
				replayToolScript.OnCheckpointEnter(other, this.gameObject.name);
			}
		}
	}
}