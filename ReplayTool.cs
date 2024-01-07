// Mod
using ReplayMod;
using MelonLoader;
using MelonLoader.TinyJSON;

// Unity
using UnityEngine;

// Megagon
using Il2CppMegagon.Downhill.Players;
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
		private MelonPreferences_Entry<bool> cfg_autoStart;
		private MelonPreferences_Entry<bool> cfg_autoStop;
		private MelonPreferences_Entry<bool> cfg_autoSave;

		// Object References
		private Transform _playerBikeTransform;
		private BikeLocomotion _bikeMoveScript;
		private Animator _bikeAnimator;

		private GameObject _playerReplayClone;

		private GameObject _finishLine;
		FinishLineTriggerReader _finishTriggerReader;
		private List<GameObject> _foundCheckpoints;

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
			cfg_recordFrequency = mainSettingsCat.CreateEntry<int>("RecordingFrequency", 30);
			cfg_autoStart = mainSettingsCat.CreateEntry<bool>("StartRecordingOnLevelLoad", false);
			cfg_autoStop = mainSettingsCat.CreateEntry<bool>("StopRecordingOnTrackFinish", false);
			cfg_autoSave = mainSettingsCat.CreateEntry<bool>("AutoSaveOnTrackFinish", false);

			mainSettingsCat.SaveToFile();



			// Output the Game data path to the console
			//LoggerInstance.Msg("Replay save location: " + _replaySavePath);
		}

		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			string[] blacklistedLoadScenes = {
				"Menu_Alps_01", "Menu_Autumn_01", "Menu_Canyon_01", "Menu_Rockies_01", "Menu_Island_01",
				"gameplay", "DontDestroyOnLoad", "HideAndDontSave"
			};
			if (Array.IndexOf(blacklistedLoadScenes, sceneName) == -1)
			{
				LoggerInstance.Msg($"Scene {sceneName} with build index {buildIndex} has been loaded!");
				_activeSceneName = sceneName;

				// Get a list of all checkpoint GO's
				_foundCheckpoints = new List<GameObject>();
				FindEnabledObjectsWithPartial("GameObject_Checkpoint");

				// Add the TriggerReader to each checkpoint
				for (var i = 0; i < _foundCheckpoints.Count; i++)
				{
					GameObject currentObject = _foundCheckpoints[i];
					if (currentObject.active)
					{
						CheckpointTriggerReader _triggerScript = currentObject.AddComponent<CheckpointTriggerReader>();
						_triggerScript.replayToolScript = this;
					}
				}

				// Find the finish line and add the trigger
				_finishLine = GameObject.Find("GameObject_FinishLine");
				_finishTriggerReader = _finishLine.AddComponent<FinishLineTriggerReader>();
				_finishTriggerReader.replayToolScript = this;

				// FIXME:
				//MelonEvents.OnGUI.Subscribe(DrawInfoText, 100);


				if (cfg_autoStart.Value && !_isRecording) {
					StartRecording();
				}
			}
		}

		public void OnCheckpointEnter(Collider other, string checkpointName)
		{
			int checkpointNum;

			if (checkpointName == "GameObject_Checkpoint") {
				checkpointNum = 0;
			}
			else {
				int.TryParse(checkpointName, out checkpointNum);
			}

			LoggerInstance.Msg($"OnCheckpointEnter! Name: {checkpointName}, Value: {checkpointNum}");
		}

		public void OnFinishLineEnter(Collider other)
		{
			LoggerInstance.Msg($"OnFinishLineEnter! Other: {other.gameObject.name}");

			if ((cfg_autoStop.Value || cfg_autoSave.Value) && _isRecording)
			{
				StopRecording();
			}
			if (cfg_autoSave.Value)
			{
				SaveReplay();
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
				Replay();
			}
		}

		public void HandleInputs()
		{
			// Recording
			if (Input.GetKey(KeyCode.R) && Input.GetKeyDown(KeyCode.Keypad7))
			{
				if (!_isRecording) {
					StartRecording();
				}
				else {
					StopRecording();
				}
			}

			// Replaying
			if (Input.GetKey(KeyCode.R) && Input.GetKeyDown(KeyCode.Keypad8))
			{
				if (!_isReplaying) {
					StartReplay();
				}
				else {
					StopReplay();
				}
			}

			// Saving to file
			if (Input.GetKey(KeyCode.R) && Input.GetKeyDown(KeyCode.Keypad9))
			{
				SaveReplay();
			}

			// Loading from file
			if (Input.GetKey(KeyCode.R) && Input.GetKeyDown(KeyCode.Keypad6))
			{
				LoadAndPlayReplay();
			}
		}

		private void SaveReplay()
		{
			if (_frames.Count > 0)
			{
				ReplaySegment newSegment = new ReplaySegment(_activeSceneName, 0, _frames);
				SaveToCompactJson(newSegment);
				LoggerInstance.Msg($"Replay saved to 'Replays/{_activeSceneName}.json' with {_frames.Count} frames!");
			}
		}

		private void LoadAndPlayReplay()
		{
			GetGameObjects();
			if (_activeSceneName != null)
			{
				try
				{
					ReplaySegment loadedSegment = LoadFromCompactJson(_activeSceneName);
					_frames = loadedSegment.Frames;
					LoggerInstance.Msg($"Replay loaded: '{_activeSceneName}.json' with {_frames.Count} frames!");
				}
				catch (Exception e)
				{
					LoggerInstance.Warning(e);
					//throw;
				}

				if (!_isReplaying) {
					StartReplay();
				}
				else {
					StopReplay();
					StartReplay();
				}
			}
		}

		private void GetGameObjects()
		{
			if (_playerBikeTransform == null) {
				_playerBikeTransform = GameObject.Find("Bike(Clone)").GetComponent<Transform>();
			}
			if (_bikeMoveScript == null) {
				_bikeMoveScript = _playerBikeTransform.GetComponent<BikeLocomotion>();
			}
		}

		public void StartRecording()
		{
			GetGameObjects();

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
			GetGameObjects();

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
			CreateReplayGhost();

			LoggerInstance.Msg("Replay started. Playing " + _frames.Count + " frames.");
		}

		public void CreateReplayGhost()
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

				//_bikeAnimator

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
			if (_timeValue <= _frames[^1].Timestamp)
			{
				_timeValue += Time.unscaledDeltaTime;
				GetIndex();
				//SetTransformState();	// FIXME
				SetGhostTransformState();
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
				if (_frames[i].Timestamp == _timeValue)
				{
					_index1 = i;
					_index2 = i;
					return;
				}
				else if (_frames[i].Timestamp < _timeValue & _timeValue < _frames[i + 1].Timestamp)
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
		/// Applies the position and rotation of a frame to the players bike object.
		/// </summary>
		public void SetTransformState()
		{
			if (_index1 == _index2)
			{
				_playerBikeTransform.position = _frames[_index1].Pos;
				_playerBikeTransform.rotation = _frames[_index1].Rot;
			}
			else
			{
				float interpolationFactor = (_timeValue - _frames[_index1].Timestamp) / (_frames[_index2].Timestamp - _frames[_index1].Timestamp);

				_playerBikeTransform.position = Vector3.Lerp(_frames[_index1].Pos, _frames[_index2].Pos, interpolationFactor);
				_playerBikeTransform.rotation = Quaternion.Slerp(_frames[_index1].Rot, _frames[_index2].Rot, interpolationFactor);
			}
		}

		/// <summary>
		/// Applies the position and rotation of a frame to the replay ghost bike.
		/// </summary>
		public void SetGhostTransformState()
		{
			if (_index1 == _index2)
			{
				_playerReplayClone.transform.position = _frames[_index1].Pos;
				_playerReplayClone.transform.rotation = _frames[_index1].Rot;
			}
			else
			{
				float interpolationFactor = (_timeValue - _frames[_index1].Timestamp) / (_frames[_index2].Timestamp - _frames[_index1].Timestamp);

				_playerReplayClone.transform.position = Vector3.Lerp(_frames[_index1].Pos, _frames[_index2].Pos, interpolationFactor);
				_playerReplayClone.transform.rotation = Quaternion.Slerp(_frames[_index1].Rot, _frames[_index2].Rot, interpolationFactor);
			}
		}



		private void FindEnabledObjectsWithPartial(string objectName)
		{
			GameObject[] gameObjects = GameObject.FindObjectsOfType<GameObject>();

			for (var index = 0; index < gameObjects.Length; index++)
			{
				GameObject currentObject = gameObjects[index];
				if (!currentObject.active) {
					continue;
				}

				string gameObjectName = gameObjects[index].name.ToLower();
				if (gameObjectName.Contains(objectName.ToLower()))
				{
					_foundCheckpoints.Add(gameObjects[index]);
				}
			}
		}
		/// <summary>
		/// Saves a replay segment to a json file.
		/// </summary>
		/// <param name="segment"></param>
		private static void SaveToJson(ReplaySegment segment)
		{
			// Get the path of the Game folder
			//dataPath : D:\SteamLibrary\steamapps\common\Lonely Mountains - Downhill\Replays
			string replaySaveFolder = Path.GetDirectoryName(Application.dataPath) + "\\Replays";
			string replaySavePath = replaySaveFolder + "\\" + segment.MapName + ".json";

			string jsonReplayData = JSON.Dump(segment);
			File.WriteAllText(replaySavePath, jsonReplayData);
		}

		private static ReplaySegment LoadFromJson(string fileName)
		{
			string replaySaveFolder = Path.GetDirectoryName(Application.dataPath) + "\\Replays";
			string replaySavePath = replaySaveFolder + "\\" + fileName + ".json";

			string replayAsText = File.ReadAllText(replaySavePath);
			var loadedSegmentJson = JSON.Load(replayAsText);

			ReplaySegment loadedSegment;
			JSON.MakeInto(loadedSegmentJson, out loadedSegment);

			return loadedSegment;
		}

		/// <summary>
		/// Saves a replay segment to a compacted json file.
		/// </summary>
		/// <param name="segment"></param>
		private static void SaveToCompactJson(ReplaySegment segment)
		{
			// Get the path of the Game folder
			string replaySaveFolder = Path.GetDirectoryName(Application.dataPath) + "\\Replays";
			string replaySavePath = replaySaveFolder + "\\" + segment.MapName + "_compact.json";

			List<string> frameJsonList = new();
			for (int i = 0; i < segment.Frames.Count; i++)
			{
				Snapshot currSnapshot = segment.Frames[i];
				string frameAsJson = currSnapshot.ToString();
				frameJsonList.Add(frameAsJson);
			}

			ReplaySegmentCompact compactSegment = new(segment.MapName, segment.SegmentNumber, frameJsonList);

			string jsonReplayData = JSON.Dump(compactSegment, EncodeOptions.PrettyPrint);
			File.WriteAllText(replaySavePath, jsonReplayData);
		}

		/// <summary>
		/// Loads a compacted json replay file and returns it as a ReplaySegment object.
		/// </summary>
		private ReplaySegment LoadFromCompactJson(string fileName)
		{
			string replaySaveFolder = Path.GetDirectoryName(Application.dataPath) + "\\Replays";
			string replaySavePath = replaySaveFolder + "\\" + fileName + "_compact.json";

			string replayAsText = File.ReadAllText(replaySavePath);
			var loadedSegmentJson = JSON.Load(replayAsText);

			List<Snapshot> decompressedFrames = new();

			// Extremely janky method to iterate through the json array
			bool hasMoreItems = true;
			int frameIndex = 0;
			while (hasMoreItems)
			{
				try {
					hasMoreItems = loadedSegmentJson["FrameStrings"][frameIndex] != null;
				}
				catch (System.Exception) {
					break;
				}

				//{\"Timestamp\":0.0084685, \"Pos\":[173.56622,390.06775,153.67947], \"Rot\":[-0.04777262,0.85057044,-0.078478366,-0.517773]}
				Snapshot frame = new Snapshot(loadedSegmentJson["FrameStrings"][frameIndex]);
				decompressedFrames.Add(frame);
				frameIndex++;
			}

			ReplaySegment loadedSegment = new ReplaySegment(loadedSegmentJson["MapName"], loadedSegmentJson["SegmentNumber"], decompressedFrames);
			return loadedSegment;
		}


		private string QuaternionToJson(Quaternion q)
		{
			return "{\"x\" : "+ q.x +", \"y\" : "+ q.y +", \"z\" : "+ q.z +", \"w\" : "+ q.w +"}";
		}
		private string Vector3ToJson(Vector3 vec)
		{
			return "{\"x\" : "+ vec.x +", \"y\" : "+ vec.y +", \"z\" : "+ vec.z +"}";
		}


		public void DrawInfoText()
		{
			float xOffset = 10;
			float xOffset2 = 200;
			//float xOffset3 = 450;

			string keyboardBinds = @"<b><color=cyan><size=20>
KEYBOARD
------------------
Keypad 7
Keypad 8
Keypad 9
Keypad 6
</size></color></b>";

			string bindDescriptions = @"<b><color=cyan><size=20>
| Action
--------------------------------
| Start / Stop Recording
| Start / Stop Replay
| Save to file
| Load from file
</size></color></b>";

			GUI.Label(new Rect(xOffset, 200, 1000, 200), "<b><color=lime><size=30>Replay Running</size></color></b>");

			GUI.Label(new Rect(xOffset, 230, 2000, 2000), keyboardBinds);
			GUI.Label(new Rect(xOffset2, 230, 2000, 2000), bindDescriptions);
		}

		public static void DrawRecordingText()
		{
			GUI.Label(new Rect(10, 185, 1000, 200), "<b><color=lime><size=30>Recording</size></color></b>");
		}

		public static void DrawVersionText()
		{
			GUI.Label(new Rect(20, 20, 1000, 200), "<b><color=white><size=15>Replay Tool v"+ MOD_VERSION +"</size></color></b>");
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


	public class ReplaySegment
	{
		public string formatVersion = "0";
		public string MapName;
		public int SegmentNumber;
		public List<Snapshot> Frames;

		public ReplaySegment(){}

		public ReplaySegment(string mapName, int segmentNumber, List<Snapshot> frames)
		{
			MapName = mapName;
			SegmentNumber = segmentNumber;
			Frames = frames;
		}
	}

	public class ReplaySegmentCompact
	{
		public string MapName;
		public int SegmentNumber;
		public List<string> FrameStrings;

		public ReplaySegmentCompact(string mapName, int segmentNumber, List<string> frameStrings)
		{
			MapName = mapName;
			SegmentNumber = segmentNumber;
			FrameStrings = frameStrings;
		}
	}

	/// <summary>
	/// Stores the state of an object at a single point in time.
	/// </summary>
	public struct Snapshot
	{
		public float Timestamp; //{ get; private set; }
		public Vector3 Pos; //{ get; private set; }
		public Quaternion Rot; //{ get; private set; }

		public Snapshot(float timestamp, Vector3 position, Quaternion rotation)
		{
			Timestamp = timestamp;
			Pos = position;
			Rot = rotation;
		}

		/// <summary>
		/// Creates a snapshot object from a compressed snapshot json string.
		/// </summary>
		public Snapshot(string compressedJson)
		{
			var loadedJson = JSON.Load(compressedJson);
			Timestamp = loadedJson["Timestamp"];
			Pos = new Vector3(loadedJson["Pos"][0], loadedJson["Pos"][1], loadedJson["Pos"][2]);
			Rot = new Quaternion(loadedJson["Rot"][0], loadedJson["Rot"][1], loadedJson["Rot"][2], loadedJson["Rot"][3]);
		}


		/// <summary>
		/// Returns the Snapshot as a compact JSON string.
		/// </summary>
		public override string ToString()
		{
			//{"Timestamp":0, "Pos":[0, 0, 0], "Rot":[0, 0, 0, 0]}
			return "{\"Timestamp\":"+Timestamp+", \"Pos\":["+Pos.x+","+Pos.y+","+Pos.z+"], \"Rot\":["+Rot.x+","+Rot.y+","+Rot.z+","+Rot.w+"]}";

			//Timestamp|Pos|Rot
			//"0|0,0,0|0,0,0,0"
			//return $"{Timestamp}|{Pos.x},{Pos.y},{Pos.z}|{Rot.x},{Rot.y},{Rot.z},{Rot.w},";
		}
	}

	/// <summary>
	/// The type of replay.
	/// Ghost spawns a new player.
	/// PlayerControl makes the actual player move.
	/// </summary>
	public enum ReplayMode
	{
		Ghost,
		PlayerControl
	}
}

/* REFERENCES
 * https://videlais.com/2021/02/25/using-jsonutility-in-unity-to-save-and-load-game-data/
 */