﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using xdelta3.net;

namespace PizzaOven
{
    public static class ModLoader
    {
        // Restore all backups created from previous build
        public static bool Restart()
        {
            var SoundsFolder = $"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop";
            var LanguageFolder = $"{Global.config.ModsFolder}{Global.s}lang";
            // Restore all backups of .bank files
            RestoreDirectory(SoundsFolder);
            // Delete font folder if it exists
            if (Directory.Exists($"{LanguageFolder}{Global.s}fonts"))
                Directory.Delete($"{LanguageFolder}{Global.s}fonts", true);
            // Delete all language files that aren't English
            foreach (var file in Directory.GetFiles(LanguageFolder))
                if (!Path.GetFileName(file).Equals("english.txt", StringComparison.InvariantCultureIgnoreCase))
                    File.Delete(file);
            // Delete modded .win
            if (File.Exists($"{Global.config.ModsFolder}{Global.s}PizzaOven.win"))
                File.Delete($"{Global.config.ModsFolder}{Global.s}PizzaOven.win");
            return true;
        }
        // Copy over mod files in order of ModList
        public static void Build(string mod)
        {
            var FilesToPatch = Directory.GetFiles($"{Global.config.ModsFolder}{Global.s}sound{Global.s}Desktop").ToList();
            FilesToPatch.Insert(0, $"{Global.config.ModsFolder}{Global.s}data.win");
            foreach (var modFile in Directory.GetFiles(mod, "*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(modFile);
                // xdelta patches
                if (extension.Equals(".xdelta", StringComparison.InvariantCultureIgnoreCase))
                {
                    var success = false;
                    foreach (var file in FilesToPatch)
                    {
                        if (!File.Exists(file))
                        {
                            Global.logger.WriteLine($"{file} does not exist", LoggerType.Error);
                            continue;
                        }
                        try
                        {
                            // Attempt to patch file
                            Global.logger.WriteLine($"Attempting to patch {file} with {modFile}...", LoggerType.Info);
                            Patch(file, modFile, $"{Path.GetDirectoryName(file)}{Global.s}temp");
                            if (Path.GetFileName(file).Equals("data.win", StringComparison.InvariantCultureIgnoreCase))
                                File.Move($"{Path.GetDirectoryName(file)}{Global.s}temp", $"{Path.GetDirectoryName(file)}{Global.s}PizzaOven.win", true);
                            else
                            {
                                // Only make backup if it doesn't already exist
                                if (!File.Exists($"{file}.po"))
                                    File.Copy(file, $"{file}.po", true);
                                File.Move($"{Path.GetDirectoryName(file)}{Global.s}temp", file, true);
                            }
                        }
                        catch (Exception e)
                        {
                            Global.logger.WriteLine($"Unable to patch {file} with {modFile}", LoggerType.Warning);
                            continue;
                        }
                        // Stop trying to patch if it was successful
                        success = true;
                        break;
                    }
                    if (!success)
                    {
                        Global.logger.WriteLine($"{modFile} wasn't able to patch any file. Ensure that either the mod xdelta patch or your game version is up to date", LoggerType.Error);
                    }
                }
                // Language .txt files
                else if (extension.Equals(".txt", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Verify .txt file is for language
                    if (File.ReadAllText(modFile).Contains("lang = ", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Copy over file to lang folder
                        File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}lang{Global.s}{Path.GetFileName(modFile)}", true);
                        Global.logger.WriteLine($"Copied over {modFile} to language folder", LoggerType.Info);
                    }
                }
                // Font .png files
                else if (extension.Equals(".png", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Check if png is in fonts folder
                    if (modFile.Contains("fonts", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Create fonts folder
                        Directory.CreateDirectory($"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts");
                        // Copy over file to fonts folder
                        File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}lang{Global.s}fonts{Global.s}{Path.GetFileName(modFile)}", true);
                        Global.logger.WriteLine($"Copied over {modFile} to fonts folder", LoggerType.Info);
                    }
                }
                // Copy over .win file in case modder provides entire file instead of .xdelta patch
                else if (extension.Equals(".win", StringComparison.InvariantCultureIgnoreCase))
                {
                    File.Copy(modFile, $"{Global.config.ModsFolder}{Global.s}PizzaOven.win", true);
                    Global.logger.WriteLine($"Copied over {modFile} to use instead of data.win", LoggerType.Info);
                }
                // Copy over .bank file in case modder provides entire file instead of .xdelta patch
                else if (extension.Equals(".bank", StringComparison.InvariantCultureIgnoreCase))
                {
                    var FileToReplace = $"{Global.config.ModsFolder}{Global.s}sounds{Global.s}Desktop{Global.s}{Path.GetFileName(modFile)}";
                    if (File.Exists(FileToReplace))
                    {
                        // Only make backup if it doesn't already exist
                        if (!File.Exists($"{FileToReplace}.po"))
                            File.Copy(FileToReplace, $"{FileToReplace}.po", true);
                        File.Copy(modFile, FileToReplace, true);
                        Global.logger.WriteLine($"Copied over {modFile} to use sounds folder", LoggerType.Info);
                    }
                    else
                        Global.logger.WriteLine($"{FileToReplace} does not exist", LoggerType.Error);
                }
            }
        }

        private static void Patch(string file, string patch, string output)
        {
            var fileBytes = File.ReadAllBytes(file);
            var patchBytes = File.ReadAllBytes(patch);
            var decoded = Xdelta3Lib.Decode(fileBytes, patchBytes);
            File.WriteAllBytes(output, decoded.ToArray());
            Global.logger.WriteLine($"Applied {patch} to {file}.", LoggerType.Info);
        }
        private static void RestoreDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                    if (File.Exists($"{file}.po"))
                        File.Move($"{file}.po", file, true);
            }
        }
    }
}