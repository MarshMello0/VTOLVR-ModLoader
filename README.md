# VTOL VR Mod Loader
VTOL VR Mod Loader is a basic mod loader to help people get custom scripts/assets into the game VTOLVR on steam. 

## How to install VTOL VR Mod Loader

 1. Download the zip from the [releases tab](https://github.com/MarshMello0/VTOLVR-ModLoader/releases)
 2. Extract the contents to the root of your game folder
 3. Run VTOLVR-ModLoader.exe to launch the mod loader

## How to use the mod loader
![The Mod Loader](https://raw.githubusercontent.com/MarshMello0/VTOLVR-ModLoader/master/VTOLVR-ModLoader/Images/Mod%20Loader.PNG)
This mod loader is very basic, on the left you have mods which have been found inside your mods folder. On the right, there are the mods which will get loaded when you press the "Inject Mods" button. To move a mod from the unloaded to loaded and the other way round, you just use the arrows to the side.

Once you have moved the mods you want to load, first, make sure the game is running. There is a little shortcut under the unloaded mods which launches the game via Steam. Once the game is in the first scene on the aircraft carrier, then you can inject the mods.
![The Aircraft Carrier where you can inject the mods](https://raw.githubusercontent.com/MarshMello0/VTOLVR-ModLoader/master/VTOLVR-ModLoader/Images/VTOL%20VR%20Main%20Menu.PNG)

### WARNING, You can inject mods twice, there is no protection to stop you from doing this. 
Just move them back to the unloaded side or close the application and reopen it if you want to load more mid-session.

## Mods List
|Mod Name | Description|Version | Download Link | Author |
|--|--|--|--|--|
|No Gravity|Adds a basic button to disable/enable gravity|1.0 | [Download](https://github.com/MarshMello0/VTOLVR-ModLoader/blob/master/Example%20Mods/NoGravity/NoGravity.dll?raw=true)| . Marsh.Mello . |
|Console Mod|Displays the Unity Console in a sperate window|1.0 | [Download](https://github.com/MarshMello0/MarshMellos_VTOLVR_Mods/blob/master/ConsoleMod/ConsoleMod.dll?raw=true)| . Marsh.Mello . |

## How to create mods

Mods are created in C# in Visual Studio using the .net framework. Creating these mods are just like creating your own script in Unity, so if you have experience in writing C# scripts in Unity you can check out my more detailed post in the [Wiki](https://github.com/MarshMello0/VTOLVR-ModLoader/wiki).

