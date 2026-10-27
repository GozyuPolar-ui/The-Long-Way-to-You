using System;

namespace Prince
{
    internal static class MenuStrings
    {
        internal const string GameTitle = "The Long Way to You";
        internal const string Subtitle = "A quiet journey";
        internal const string Continue = "Continue";
        internal const string NewGame = "New Game";
        internal const string Settings = "Settings";
        internal const string Quit = "Quit";
        internal const string Studio = "ExceedOG";
        internal const string Beta = "BETA TEST";
        internal const string VersionPrefix = "v";
        internal const string MasterVolume = "Master Volume";
        internal const string Music = "Music";
        internal const string Effects = "Effects";
        internal const string GraphicsQuality = "Graphics Quality";
        internal const string Fullscreen = "Fullscreen";
        internal const string CameraSensitivity = "Camera Sensitivity";
        internal const string Back = "Back";
        internal const string StartNewConfirmation =
            "Start a new game? Your current progress will be replaced.";
        internal const string QuitConfirmation = "Quit the game?";
        internal const string Confirm = "Yes";
        internal const string Cancel = "Cancel";
        internal const string DefaultQuality = "Default";

        internal static string Copyright => $"© {DateTime.Now.Year} {Studio}. All rights reserved.";

        internal static string Version
        {
            get
            {
                string version = UnityEngine.Application.version;
                return $"{VersionPrefix}{(string.IsNullOrWhiteSpace(version) ? "0.0.0" : version)}";
            }
        }
    }
}
