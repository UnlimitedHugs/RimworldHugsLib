![HugsLib logo](http://i.imgur.com/1d35OiC.png)

A lightweight shared mod library for Rimworld. Provides a foundation for mods and delivers shared functionality.

**Notice:** HugsLib has changed and must now be installed as a separate mod by the players. The library itself is no longer to be included with your mods. You can, however, include the checker assembly to ensure that the player will be notified if they are missing the necessary version of the library ([RimworldHugsLibChecker](https://github.com/UnlimitedHugs/RimworldHugsLibChecker)).

## Current features
- Mod foundation: Base class to build mods on. Extending classes have access to custom logging, settings, and receive the following events from the library controller: Initialize, Tick, Update, FixedUpdate, OnGUI, WorldLoaded, MapComponentsInitializing, MapLoaded, SceneLoaded, SettingsChanged, DefsLoaded.
- Persistent in-game settings: Implementing mods can create custom settings of various types that can be changed by the player in the new Mod Settings menu. Settings are stored in a file in the user folder.
- Mod update news: Mods can provide a message for each version they release, highlighting new features. These messages will be shown once to the player the next time he starts the game. This is a good way to ensure that new mod features do not go unnoticed by the majority of players. This is especially true on Steam, where the player may not have even read the description before subscribing. Messages include support for images and basic formatting.
- Log publisher: Adds a keyboard shortcut (**Ctrl+F12**) to publish the logs from within the game. Returns a URL that you can share with others or send to a mod author. The published logs also include the list of running mods and their versions. This is a great way for a mod author to get the logs from a player who is experiencing an issue with his mod.
- Checker assembly: A small dll designed to be included with your mod, that ensures the player is running at least the version of the library you specify. A dialog is displayed if a problem is detected, helping the player to resolve the issue. This is how the library stays up to date. See [RimworldHugsLibChecker](https://github.com/UnlimitedHugs/RimworldHugsLibChecker) for more info.
- UtilityWorldObjects: A convenient way to store your data in a save file. Since A16 MapComponents are no longer a reliable way to store your data, and UWO's are designed to be a drop-in replacement.
- Custom tick scheduling: Includes tools for executing callbacks with a specified tick delay, and registering recurring ticks with non-standard intervals. Recurring ticks are distributed uniformly across the time spectrum, to minimize the performance impact of the ticking entity.
- Detouring: provides special attributes for more convenient detouring. Detours are safety-checked to prevent improper use. Repeated detours of the same method are not allowed and will generate an error. Mods can implement special methods to handle detouring errors, which allows for graceful failure and easier pinpointing of player issues.
- GUI injection: provides a special attribute that allows a method to be executed whenever a given window type is drawn. This allows to inject drawing code for any window in the game.
- Auto-restarter: adds a prompt to the Mods dialog to restart the game when changes to the mod configuration have been made. Mod load order changes are also detected.
- Log window additions: adds buttons to copy the selected log message and activate the log publisher. Also adds a menu to find common files: open the log file and browse the user data and mods folders.

## Compatibility
The only detour by the library itself is the `Window.WindowOnGUI` method, used to power the GUI injection system.

## Usage
This is a public library similar to CCL, designed to be easily updateable between Rimworld versions. Feel free to use it for your own projects.