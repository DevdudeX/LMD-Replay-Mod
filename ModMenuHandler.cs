using LonelyMountains_ModMenu;

namespace ReplayMod
{
	internal class ModMenuHandler
	{
		ReplayTool replayToolScript;
		string InfoName = "Replay Tool";

		public void HandleOnInitializeMelon(ReplayTool replayTool)
		{
			replayToolScript = replayTool;

			// Mod Menu
			MenuManager.Instance.RegisterActionBtn(InfoName, "Start Recording", 0, MenuStartRecording);
			MenuManager.Instance.RegisterActionBtn(InfoName, "Stop Recording", 1, MenuStopRecording);
			MenuManager.Instance.RegisterActionBtn(InfoName, "Start Replay", 2, MenuStartReplay);
			MenuManager.Instance.RegisterActionBtn(InfoName, "Stop Replay", 3, MenuStopReplay);

			MenuManager.Instance.RegisterActionBtn(InfoName, "Save Replay", 4, MenuSaveReplay);
			MenuManager.Instance.RegisterActionBtn(InfoName, "Load Replay", 5, MenuLoadReplay);

			MenuManager.Instance.RegisterInfoItem(InfoName, "State: ", 6, MenuGetReplayState);
		}

		// MOD MENU CONTROLS ==============
		void MenuStartRecording(int callbackID)
		{
			replayToolScript.MStartRecording();
		}
		void MenuStopRecording(int callbackID)
		{
			replayToolScript.MStopRecording();
		}

		void MenuStartReplay(int callbackID)
		{
			replayToolScript.MStartReplay();
		}
		void MenuStopReplay(int callbackID)
		{
			replayToolScript.MStopReplay();
		}

		void MenuSaveReplay(int callbackID)
		{
			replayToolScript.MSaveReplay();
		}
		void MenuLoadReplay(int callbackID)
		{
			replayToolScript.MLoadReplay();
		}

		string MenuGetReplayState()
		{
			return replayToolScript.GetReplayState();
		}
	}
}
