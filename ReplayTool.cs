// Mod
using MelonLoader;
using ReplayMod;
// Unity
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
// Megagon
using Il2CppMegagon.Downhill.Players;
using Il2CppMegagon.Downhill.Vehicle.Controller;

using System.IO;

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
		private Transform _playerBikeTransform;
		private BikeLocomotion _bikeMoveScript;
		private GameObject _playerReplayClone;

		private bool _isRecording;
		private bool _isReplaying;
		private int _index1;
		private int _index2;
		private float _timer;
		private float _timeValue;
		private List<Snapshot> _frames;

		private string _activeSceneName;
		private string _replaySavePath;

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

			// Get the path of the Game folder
			string _replaySavePath = Path.GetDirectoryName(Application.dataPath) + "\\Replays";
			//dataPath : D:\SteamLibrary\steamapps\common\Lonely Mountains - Downhill\Replays

			// Output the Game data path to the console
			LoggerInstance.Msg("Replay save location: " + _replaySavePath);
		}

		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			//LoggerInstance.Msg($"Scene {sceneName} with build index {buildIndex} has been loaded!");
			string[] blacklistedLoadScenes = {"Menu_Alps_01", "Menu_Autumn_01", "Menu_Canyon_01", "Menu_Rockies_01", "Menu_Island_01"};
			if (Array.IndexOf(blacklistedLoadScenes, sceneName) == -1)
			{
				LoggerInstance.Msg($"Scene {sceneName} with build index {buildIndex} has been loaded!");
				_activeSceneName = sceneName;
			}
		}

		public override void OnUpdate()
		{
			if (forceDisable) {
				// Used for OnDeinitializeMelon()
				return;
			}

			HandleInputs();
			if (_isRecording)
			{
				Record();
			}
			if (_isReplaying)
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
				if (!_isRecording) {
					StartRecording();
				}
				else {
					StopRecording();
				}
			}

			// Replaying
			if (Input.GetKeyDown(KeyCode.Keypad9))
			{
				if (!_isReplaying) {
					StartReplay();
				}
				else {
					StopReplay();
				}
			}
		}

		public void StartRecording()
		{
			if (_playerBikeTransform == null) {
				_playerBikeTransform = GameObject.Find("Bike(Clone)").GetComponent<Transform>();
			}
			if (_bikeMoveScript == null) {
				_bikeMoveScript = _playerBikeTransform.GetComponent<BikeLocomotion>();
			}

			if (_isRecording) StopReplay();

			_frames = new List<Snapshot>();
			_timeValue = 0;
			_timer = 0;
			_isRecording = true;
			_isReplaying = false;

			MelonEvents.OnGUI.Subscribe(DrawRecordingText, 100);

			LoggerInstance.Msg("Replay recording started.");
		}
		public void StopRecording()
		{
			_isRecording = false;
			_isReplaying = false;

			MelonEvents.OnGUI.Unsubscribe(DrawRecordingText);

			LoggerInstance.Msg("Replay recording stopped. Saved " + _frames.Count + " frames.");
		}

		public void StartReplay()
		{
			if (_isRecording) {
				LoggerInstance.Msg("Error starting replay. Currently recording.");
				return;
			}
			if (_frames.Count <= 0) {
				LoggerInstance.Msg("Error starting replay. No frames to play.");
				return;
			}

			MelonEvents.OnGUI.Subscribe(DrawInfoText, 100);

			_timeValue = 0;
			_index1 = 0;
			_index2 = 0;
			_isRecording = false;
			_isReplaying = true;

			// Disable interfering stuff
			CreateReplayClone();

			LoggerInstance.Msg("Replay started. Playing " + _frames.Count + " frames.");
		}

		public void CreateReplayClone()
		{
			_playerReplayClone = GameObject.Instantiate(_playerBikeTransform.gameObject);
			GameObject.Destroy(_playerReplayClone.GetComponent<BikeLocomotion>());
			GameObject.Destroy(_playerReplayClone.GetComponent<PlayerCameraTarget>());
			GameObject.Destroy(_playerReplayClone.GetComponent<Stamina>());
		}

		public void StopReplay()
		{
			MelonEvents.OnGUI.Unsubscribe(DrawInfoText);
			_index1 = 0;
			_index2 = 0;

			_isRecording = false;
			_isReplaying = false;

			// Remove the clone
			GameObject.Destroy(_playerReplayClone);

			LoggerInstance.Msg("Replay was stopped.");
		}

		public void Record()
		{
			_timer += Time.unscaledDeltaTime;
			_timeValue += Time.unscaledDeltaTime;

			if (_timer >= 1 / cfg_recordFrequency.Value)
			{
				Snapshot newFrame = new Snapshot(
					_timeValue,
					_playerBikeTransform.position,
					_playerBikeTransform.rotation
				);

				_frames.Add(newFrame);
				_timer = 0;
			}
		}

		public void Replay()
		{
			if (_timeValue <= _frames[_frames.Count - 1].timestamp)
			{
				_timeValue += Time.unscaledDeltaTime;
				GetIndex();
				//SetTransformState();	// FIXME
				SetTransformStateClone();
			}
			else {
				LoggerInstance.Msg("Replay ended.");
				StopReplay();
			}
		}

		public void GetIndex()
		{
			for (int i = 0; i < _frames.Count - 2; i++)
			{
				if (_frames[i].timestamp == _timeValue)
				{
					_index1 = i;
					_index2 = i;
					return;
				}
				else if (_frames[i].timestamp < _timeValue & _timeValue < _frames[i + 1].timestamp)
				{
					_index1 = i;
					_index2 = i + 1;
					return;
				}
			}
			_index1 = _frames.Count - 1;
			_index2 = _frames.Count - 1;
		}

		/// <summary>
		/// Applies the position and rotation of a frame to the bike object.
		/// </summary>
		public void SetTransformState()
		{
			if (_index1 == _index2)
			{
				_playerBikeTransform.position = _frames[_index1].pos;
				_playerBikeTransform.rotation = _frames[_index1].rot;
			}
			else
			{
				float interpolationFactor = (_timeValue - _frames[_index1].timestamp) / (_frames[_index2].timestamp - _frames[_index1].timestamp);

				_playerBikeTransform.position = Vector3.Lerp(_frames[_index1].pos, _frames[_index2].pos, interpolationFactor);
				_playerBikeTransform.rotation = Quaternion.Slerp(_frames[_index1].rot, _frames[_index2].rot, interpolationFactor);
			}
		}

		public void SetTransformStateClone()
		{
			if (_index1 == _index2)
			{
				_playerReplayClone.transform.position = _frames[_index1].pos;
				_playerReplayClone.transform.rotation = _frames[_index1].rot;
			}
			else
			{
				float interpolationFactor = (_timeValue - _frames[_index1].timestamp) / (_frames[_index2].timestamp - _frames[_index1].timestamp);

				_playerReplayClone.transform.position = Vector3.Lerp(_frames[_index1].pos, _frames[_index2].pos, interpolationFactor);
				_playerReplayClone.transform.rotation = Quaternion.Slerp(_frames[_index1].rot, _frames[_index2].rot, interpolationFactor);
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

/* REFERENCES
 * https://videlais.com/2021/02/25/using-jsonutility-in-unity-to-save-and-load-game-data/
 */