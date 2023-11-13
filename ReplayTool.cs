// Mod
using MelonLoader;
using ReplayMod;
// Unity
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
// Megagon
using Il2CppMegagon.Downhill.Cameras;
using Il2CppMegagon.Downhill.Vehicle.Controller;

[assembly: MelonInfo(typeof(ReplayTool), "Replay Tool", "0.0.1", "DevdudeX")]
[assembly: MelonGame()]
namespace ReplayMod
{
	public class ReplayTool : MelonMod
	{
		// Keep this updated!
		private const string MOD_VERSION = "0.0.1";
		public static ReplayTool instance;
		private static bool forceDisable = false;

		private MelonPreferences_Category mainSettingsCat;
		private MelonPreferences_Entry<int> cfg_recordFrequency;

		// Object References
		private Transform playerBikeTransform;
		private BikeLocomotion bikeMoveScript;


		private bool isRecording;
		private bool isReplaying;
		private int index1;
		private int index2;
		private float timer;
		private float timeValue;
		private List<Snapshot> frames;

		public override void OnEarlyInitializeMelon()
		{
			instance = this;
			MelonEvents.OnGUI.Subscribe(DrawVersionText, 100);
		}

		public override void OnInitializeMelon()
		{
			mainSettingsCat = MelonPreferences.CreateCategory("Main Settings");
			mainSettingsCat.SetFilePath("UserData/ReplayToolSettings.cfg");

			// Main Settings
			cfg_recordFrequency = mainSettingsCat.CreateEntry<int>("RecordingFrequency", 45);

			mainSettingsCat.SaveToFile();
		}

		public override void OnUpdate()
		{
			if (forceDisable) {
				// Used for OnDeinitializeMelon()
				return;
			}

			HandleInputs();
			if (isRecording)
			{
				Record();
			}
			if (isReplaying)
			{
				// TODO: disable BikeLocomotion during replay
				Replay();
			}
		}

		public void HandleInputs()
		{
			// Recording
			if (Input.GetKeyDown(KeyCode.Keypad7))
			{
				if (!isRecording) {
					StartRecording();
				}
				else {
					StopRecording();
				}
			}

			// Replaying
			if (Input.GetKeyDown(KeyCode.Keypad9))
			{
				if (!isReplaying) {
					StartReplay();
				}
				else {
					StopReplay();
				}
			}
		}

		public void StartRecording()
		{
			if (playerBikeTransform == null)
			{
				Debug.Log("[ReplayMod]Debug: Grabbing player bike transform.");
				playerBikeTransform = GameObject.Find("Bike(Clone)").GetComponent<Transform>();
			}
			if (bikeMoveScript == null)
			{
				bikeMoveScript = playerBikeTransform.GetComponent<BikeLocomotion>();
			}

			if (isRecording) StopReplay();

			frames = new List<Snapshot>();
			timeValue = 0;
			timer = 0;
			isRecording = true;
			isReplaying = false;

			MelonEvents.OnGUI.Subscribe(DrawRecordingText, 100);

			LoggerInstance.Msg("Replay recording started.");
		}
		public void StopRecording()
		{
			isRecording = false;
			isReplaying = false;

			MelonEvents.OnGUI.Unsubscribe(DrawRecordingText);

			LoggerInstance.Msg("Replay recording stopped. Saved " + frames.Count + " frames.");
		}

		public void StartReplay()
		{
			if (isRecording) {
				LoggerInstance.Msg("Error starting replay. Currently recording.");
				return;
			}
			if (frames.Count <= 0) {
				LoggerInstance.Msg("Error starting replay. No frames to play.");
				return;
			}

			MelonEvents.OnGUI.Subscribe(DrawInfoText, 100);

			timeValue = 0;
			index1 = 0;
			index2 = 0;
			isRecording = false;
			isReplaying = true;

			// Disable interfering stuff
			//camScript.enabled = false;
			bikeMoveScript.enabled = false;

			LoggerInstance.Msg("Replay started. Playing " + frames.Count + " frames.");
		}
		public void StopReplay()
		{
			MelonEvents.OnGUI.Unsubscribe(DrawInfoText);
			index1 = 0;
			index2 = 0;

			isRecording = false;
			isReplaying = false;

			// Reenable interfering stuff
			//camScript.enabled = true;
			bikeMoveScript.enabled = true;

			LoggerInstance.Msg("Replay was stopped.");
		}

		public void Record()
		{
			timer += Time.unscaledDeltaTime;
			timeValue += Time.unscaledDeltaTime;

			if (timer >= 1 / cfg_recordFrequency.Value)
			{
				Snapshot newFrame = new Snapshot(
					timeValue,
					playerBikeTransform.position,
					playerBikeTransform.rotation
				);

				frames.Add(newFrame);
				timer = 0;
			}
		}

		public void Replay()
		{
			if (timeValue <= frames[frames.Count - 1].timestamp)
			{
				timeValue += Time.unscaledDeltaTime;
				GetIndex();
				SetTransformState();
			}
			else {
				LoggerInstance.Msg("Replay ended.");
				StopReplay();
			}
		}

		public void GetIndex()
		{
			for (int i = 0; i < frames.Count - 2; i++)
			{
				if (frames[i].timestamp == timeValue)
				{
					index1 = i;
					index2 = i;
					return;
				}
				else if (frames[i].timestamp < timeValue & timeValue < frames[i + 1].timestamp)
				{
					index1 = i;
					index2 = i + 1;
					return;
				}
			}
			index1 = frames.Count - 1;
			index2 = frames.Count - 1;
		}

		/// <summary>
		/// Applies the position and rotation of a frame to the bike object.
		/// </summary>
		public void SetTransformState()
		{
			if (index1 == index2)
			{
				playerBikeTransform.position = frames[index1].pos;
				playerBikeTransform.rotation = frames[index1].rot;
			}
			else
			{
				float interpolationFactor = (timeValue - frames[index1].timestamp) / (frames[index2].timestamp - frames[index1].timestamp);

				playerBikeTransform.position = Vector3.Lerp(frames[index1].pos, frames[index2].pos, interpolationFactor);
				playerBikeTransform.rotation = Quaternion.Slerp(frames[index1].rot, frames[index2].rot, interpolationFactor);
			}
		}


		public void DrawInfoText()
		{
			float xOffset = 10;
			float xOffset2 = 200;
			float xOffset3 = 450;

						string keyboardBinds = @"<b><color=cyan><size=20>
KEYBOARD
------------------
Keypad 7
Keypad 9
</size></color></b>";

			string gamepadBinds = @"<b><color=cyan><size=20>
| GAMEPAD
-----------------------
|
|
</size></color></b>";

			string bindDescriptions = @"<b><color=cyan><size=20>
| Action
--------------------------------
| Start / Stop Recording
| Start / Stop Replay
</size></color></b>";

			GUI.Label(new Rect(xOffset, 200, 1000, 200), "<b><color=lime><size=30>Replay in progress</size></color></b>");

			GUI.Label(new Rect(xOffset, 230, 2000, 2000), keyboardBinds);
			GUI.Label(new Rect(xOffset2, 230, 2000, 2000), gamepadBinds);
			GUI.Label(new Rect(xOffset3, 230, 2000, 2000), bindDescriptions);
		}

		public static void DrawRecordingText()
		{
			GUI.Label(new Rect(10, 200, 1000, 200), "<b><color=lime><size=30>Recording</size></color></b>");
		}

		public static void DrawVersionText()
		{
			GUI.Label(new Rect(20, 8, 1000, 200), "<b><color=white><size=15>Replay Tool v"+ MOD_VERSION +"</size></color></b>");
		}
		public override void OnDeinitializeMelon()
		{
			// In case the melon gets unregistered
			forceDisable = true;
			MelonEvents.OnGUI.Unsubscribe(DrawRecordingText);
			MelonEvents.OnGUI.Unsubscribe(DrawInfoText);
			MelonEvents.OnGUI.Unsubscribe(DrawVersionText);
		}
	}

	/// <summary>
	/// Stores the state of an object at a single point in time.
	/// </summary>
	public struct Snapshot
	{
		public float timestamp { get; private set; }
		public Vector3 pos { get; private set; }
		public Quaternion rot { get; private set; }

		public Snapshot(float timestamp, Vector3 position, Quaternion rotation)
		{
			this.timestamp = timestamp;
			this.pos = position;
			this.rot = rotation;
		}
	}
}