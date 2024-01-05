# DevdudeX's Replay Tool for Lonely Mountains: Downhill
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/L4L5S9BK3)

A MelonLoader mod that aims to let you replay your movement to record at different angles.
Very early in development so expect bugs.


## Setup Instructions
#### Preparing
Your game folder can be found by right-clicking on the game in steam and going 'Manage -> Browse local files'  

Install Melon Loader to your LMD game install folder.  
Look under 'Automated Installation':  
https://melonwiki.xyz/#/  
(v0.6.1 is the current version at time of writing)  

Run the game once then exit. (See **Known Issues & Fixes** if your game freezes on quit)  
If successful the Melon Loader splash screen should appear on launch. 

Download `ReplayTool.dll` from the releases and add it to the `Mods` folder in your LMD game folder.  

#### Loading The Mod In-Game
After loading into a level wait to be able to move the bike then hit the start recording button.  


#### Tweaking values
A config file is generated in `[LMD folder]/UserData/ReplayModSettings.cfg`.  
This file can be opened with any text editor and contains all the mods settings.  


#### Keybinds
| Keyboard & Mouse      | Gamepad      | Action                               |
| ---                   | ---          | ---                                  |
| R + Keypad 7          |              | Start / Stop Recording               |
| R + Keypad 8          |              | Start / Stop Replay                  |
| R + Keypad 9          |              | Save to JSON                         |
| R + Keypad 6          |              | Load from JSON                       |


#### Known Issues & Fixes
- Game freezes on quitting: Use [`Alt + Tab`] to select the commandline window and then close it.
- Controls are currently not rebindable
