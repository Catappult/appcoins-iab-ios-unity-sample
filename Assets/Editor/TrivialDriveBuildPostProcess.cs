#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

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

        // Required by Apple for apps that sell digital goods via StoreKit.
        plist.root.SetBoolean("MKSellsDigitalGoods", true);

        // LOCAL TESTING ONLY — allows plain HTTP to LAN IPs for the local trivial-drive-service.
        // Remove before shipping; production must use HTTPS.
        PlistElementDict ats = plist.root.values.ContainsKey("NSAppTransportSecurity")
            ? plist.root["NSAppTransportSecurity"].AsDict()
            : plist.root.CreateDict("NSAppTransportSecurity");
        ats.SetBoolean("NSAllowsLocalNetworking", true);

        plist.WriteToFile(plistPath);
    }
}
#endif
