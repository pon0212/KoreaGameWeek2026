using System.IO;
using UnityEditor.Android;
using UnityEngine;

public sealed class AndroidLibraryNamespaceFixer : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 0;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        var gradle = Path.Combine(path, "TastePassportTts.androidlib", "build.gradle");
        if (!File.Exists(gradle)) return;

        var contents = File.ReadAllText(gradle);
        if (contents.Contains("namespace ")) return;

        contents = contents.Replace("android {", "android {\r\n    namespace 'com.tastepassport.tts'");
        File.WriteAllText(gradle, contents);
        Debug.Log("Added namespace to TastePassportTts Android library.");
    }
}
