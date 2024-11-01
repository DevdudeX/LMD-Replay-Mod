# DevdudeX's Replay Tool for Lonely Mountains: Downhill
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/L4L5S9BK3)

A MelonLoader mod that aims to let you replay your movement to record at different angles.
Very early in development so expect potential bugs.


## Setup Instructions
#### Preparing
Your game folder can be found by right-clicking on the game in steam and going 'Manage -> Browse local files'  
Alternatively, for other platforms/storefronts, you will simply need to navigate to your game installation folder manually.  

Install Melon Loader to your LMD game install folder.  
Look under 'Automated Installation':  
https://melonwiki.xyz/#/  
(v0.6.1 is the current version at time of writing)  

Run the game once then exit. (See **Known Issues & Fixes** if your game freezes on quit)  
If successful the Melon Loader splash screen should appear on launch. 

Download `ReplayTool.dll` from the [Releases](https://github.com/DevdudeX/LMD-Replay-Mod/releases/latest) and add it to the `Mods` folder in your LMD game folder.  
Create a folder called `Replays` in your main game folder (make sure it's visible next to the `Mods` folder).  


#### Usage
After loading into a level wait to be able to move the bike then hit the start recording button.  

#### Tweaking values
A config file is generated in `[LMD folder]/UserData/ReplayToolSettings.cfg`.  
This file can be opened with any text editor and contains all the mods settings.  


#### Keybinds
Alternatively you can use the [Mod Menu](https://github.com/DevdudeX/LM-ModMenu/releases/latest) for a visual interface.
| Action                               | Keyboard & Mouse      | Gamepad      |
| ---                                  | ---                   | ---          |
| Start / Stop Recording               | R + Keypad 7          |              |
| Start / Stop Replay                  | R + Keypad 8          |              |
| Save to JSON                         | R + Keypad 9          |              |
| Load from JSON                       | R + Keypad 6          |              |


#### What Gets Saved
:heavy_check_mark: Complete
:x: Incomplete / not available
:construction::wrench: Work in Progress

| Feature                  | Status               
| ---                      | ---                  
| Position                 |:heavy_check_mark:    
| Rotation                 |:heavy_check_mark:    
| Animations               |:x::construction::wrench:
| Particles                |:x:                   
| Crash Ragdolls           |:x:                   


#### Known Issues & Fixes
- Controls are currently not rebindable
- Game freezes on quitting: Add the `--quitfix` [MelonLoader launch option](https://github.com/LavaGang/MelonLoader#launch-options).  
On steam: right-click on LMD --> Properties --> Launch Options --> Paste the command (with `--` infront!).

