using UnityEngine;
using MelonLoader;

namespace ReplayMod
{
	[RegisterTypeInIl2Cpp]
	public class FinishLineTriggerReader : MonoBehaviour
	{
		public FinishLineTriggerReader(IntPtr ptr) : base(ptr) { }

		public ReplayTool replayToolScript;

		public void OnTriggerEnter(Collider other)
		{
			Debug.Log("TriggerReader() OnTriggerEnter! Other: " + other.gameObject.name);
			if (replayToolScript != null) {
				replayToolScript.OnFinishLineEnter(other);
			}
		}
	}
}