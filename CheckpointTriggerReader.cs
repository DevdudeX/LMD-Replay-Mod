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
			replayToolScript?.OnCheckpointEnter(other, gameObject.name);
		}
	}
}