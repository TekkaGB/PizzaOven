<img src="https://media.discordapp.net/attachments/796396777090711635/1088971726676828300/PizzaOvenPreview.png?width=1074&height=604" width="500">
Pizza Oven is a tool that allows gamers to download, install, and manage mods for Pizza Tower. The aim for it is to make installing mods a much better quality of life experience. Unfortunately, this tool does not solve the issue of combining multiple mods as there is no easy way to combine xdelta patches (the main method of modding Pizza Tower).

## Getting Started
### Prerequisites
When you first open the exe, you'll get a message saying to install .NET 7 Desktop Runtime if you don't already have it installed. Please do so if that's the case.

### Setup
After the exe actually launches, Pizza Oven will automatically try to locate the game directory. If it fails to find it, it will prompt you to manually select your PizzaTower.exe. Setup will then be done just like that! If you for some reason need to setup again, just click the Setup button again.

## Features
### Installing Mods
Before you can manage and load some mods, you have to install some.

There are 3 methods of doing this:
1. Using the built in Mod Browser tab to download mods found on GameBanana
2. Using 1-click install buttons from browsing mods directly from the GameBanana website
3. Downloading mods from other sources and dropping the folders/archive files onto the mod grid for easy install.

### Managing Mods
There's not much to managing mods as you can only select one mod at a time to use. You can drag and drop the mods in whatever order you want for ease of access. You can also use the search bar to easily find the mod you're looking for amongst many. Once you decide which mod to use, press Launch to play. If you want to go back to playing a Vanilla version of the game, simply press Clear Selection then Launch.

### Auto Updates
Pizza Oven also supports auto updates for mods downloaded from GameBanana. Click the Check for Updates button for Pizza Oven to check if any are available for the currently selected game. It will also check if there is an update for Pizza Oven itself. These updates are also checked when launched.

## How It Works
Pizza Oven will go through all of the files for the selected mod and do different things based on the file extension. 

### .xdelta
If it finds an xdelta patch, it will first try to patch the data.win file. If it fails, it will then attempt to patch every single .bank file from the sound/Desktop folder until it succeeds.

### .txt
It will make sure its a language file by reading the contents first. Then it will copy over the .txt file to the lang folder

### .png
If the .png file is in a fonts folder, it will copy it over to the lang/fonts folder

### .win
If the entire .win file is provided (which is bad practice), it will copy over the .win file to be used with the game

### .bank
It will look if the .bank file exists in the sound/Desktop folder. If it does it will make a backup of the original file then copy the modded file over.

## FAQ
### Why isn't the modded .xdelta patch working?
Either your game needs to be updated to the latest version or the mod creator needs to update their .xdelta patch to be used with the latest verseion.

### Why can't I use multiple mods at once?
Unless someone comes up with an alternative way of modding the game besides using xdelta patches, Pizza Oven will continue to only allow users to use one mod at a time. If a method becomes available, please let me know ASAP and I will work on incorporating it.

### Is this safe? My antivirus is getting set off.
Yes this application is safe. Antivirus tends to trigger false alarms, especially due to it needing to be connected to the internet in order to be compatible with 1-click installations and updating. You can check out the source code for yourself if you're suspicious of anything as well.

### Why won’t Pizza Oven open?
I made it so only one instance is running at a time so if it’s already running, the app won’t open. Check to see if you can end the process in task manager or even restart your PC if you don’t know how to do that. 

### Why doesn't Pizza Oven have permissions to copy over files?
Try running as administrator or checking to see if any antivirus is preventing the application from operating on files.
