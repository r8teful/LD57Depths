using System;
using System.IO;
using UnityEngine;


public static class SavePathResolver {
    public static string GetSharedSavePath() {
        string company = Application.companyName;
        string title = Application.productName;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            company, title);

#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Personal),
                "Library", "Application Support", company, title);

#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            string xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                         ?? Path.Combine(Environment.GetFolderPath(
                                Environment.SpecialFolder.Personal), ".local", "share");
            return Path.Combine(xdg, company, title);
#endif
    }
}