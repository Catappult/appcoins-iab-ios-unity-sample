#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

// LOCAL TESTING ONLY.
//
// Allows the app to reach the local trivial-drive-service over plain HTTP on a
// LAN IP (e.g. http://192.168.1.132:8000) from a physical device. App Transport
// Security blocks cleartext HTTP by default; NSAllowsLocalNetworking permits
// connections to reserved/private ranges (incl. 192.168.0.0/16) without HTTPS.
//
// Remove this (or the exception) before shipping — production should call the
// server over HTTPS.
public static class TrivialDriveBuildPostProcess
{
    [PostProcessBuild]
    public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
    {
        if (buildTarget != BuildTarget.iOS)
        {
            return;
        }

        string plistPath = Path.Combine(buildPath, "Info.plist");
        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        PlistElementDict ats = plist.root.values.ContainsKey("NSAppTransportSecurity")
            ? plist.root["NSAppTransportSecurity"].AsDict()
            : plist.root.CreateDict("NSAppTransportSecurity");
        ats.SetBoolean("NSAllowsLocalNetworking", true);

        plist.WriteToFile(plistPath);
    }
}
#endif
