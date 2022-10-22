![HugsLib logo](http://i.imgur.com/1d35OiC.png)

![Version](https://img.shields.io/badge/Rimworld-1.0-brightgreen.svg)
![Version](https://img.shields.io/badge/Rimworld-1.1-brightgreen.svg)
![Version](https://img.shields.io/badge/Rimworld-1.2-brightgreen.svg)
![Version](https://img.shields.io/badge/Rimworld-1.3-brightgreen.svg)
[![NuGet](https://img.shields.io/nuget/v/UnlimitedHugs.Rimworld.HugsLib.svg)](https://www.nuget.org/packages/UnlimitedHugs.Rimworld.HugsLib/)

A lightweight shared mod library for Rimworld. Provides a foundation for mods and delivers shared functionality.

**Notice:** HugsLib must be installed as a separate mod by the players. The library assembly itself is not to be shipped with your mod.

**Notice:** HugsLib requires the Harmony mod to be installed to work correctly. [Download here](https://github.com/pardeike/HarmonyRimWorld/releases/latest).

## Dependency tags

If your mod depends on HugsLib, it is recommended to include these tags in your About.xml file:

```xml
<modDependencies>
	<li>
		<packageId>UnlimitedHugs.HugsLib</packageId>
		<displayName>HugsLib</displayName>
		<downloadUrl>https://github.com/UnlimitedHugs/RimworldHugsLib/releases/latest</downloadUrl>
		<steamWorkshopUrl>steam://url/CommunityFilePage/818773962</steamWorkshopUrl>
	</li>
</modDependencies>
<loadAfter>
	<li>UnlimitedHugs.HugsLib</li>
</loadAfter>
```



## Current features
- Mod foundation: Base class to build mods on. Extending classes have access to custom logging, settings, and receive the following events from the library controller: Initialize, DefsLoaded, Tick, Update, FixedUpdate, OnGUI, WorldLoaded, MapGenerated, MapComponentsInitializing, MapLoaded, MapDiscarded, SceneLoaded, SettingsChanged.
- Persistent in-game settings: Implementing mods can create custom settings of various types that can be changed by the player in the new Mod Settings menu. Settings are stored in a file in the user folder.
- Mod update news: Mods can provide a message for each version they release, highlighting new features. These messages will be shown once to the player the next time he starts the game. This is a good way to ensure that new mod features do not go unnoticed by the majority of players. This is especially true on Steam, where the player may not have even read the description before subscribing. Messages include support for images and basic formatting.
- Log publisher: Adds a keyboard shortcut (**Ctrl+F12**) to publish the logs from within the game. Returns a URL that you can share with others or send to a mod author. The published logs also include the list of running mods and their versions, as well as the full list of active Harmony patches. This is a great way for a mod author to get the logs from a player who is experiencing an issue with his mod.
- Quickstarter: Load a save file or generate a new map with a given scenario and size right after the game is started. Also allows to generate a new map with one click. Settings dialog included.
- UtilityWorldObjects: A convenient way to store your data in a save file. Since A16 MapComponents are no longer a reliable way to store your data, and UWO's are designed to be a drop-in replacement.
- Custom tick scheduling: Includes tools for executing callbacks with a specified tick delay, and registering recurring ticks with non-standard intervals. Recurring ticks are distributed uniformly across the time spectrum, to minimize the performance impact of the ticking entity.
- Auto-restarter: Automatically restarts the game when the language is changed.
- Log window additions: Adds a menu to find common files: open the log file and browse the user data and mods folders.

## Compatibility
There are no known compatibility issues at this time. Please use the [Harmony library](https://github.com/pardeike/Harmony) for your detouring needs and everything should work well together.

## Usage
This is a public library similar to the Community Core Library, designed to be easily updateable between Rimworld versions. Feel free to use it for your own projects.