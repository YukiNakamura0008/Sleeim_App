using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.iOS.Xcode;
using UnityEditor.Callbacks;
using System.Collections;


public class XcodeSettingsPostProcesser {

	[PostProcessBuild]
	public static void SetXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
	{
		if (buildTarget != BuildTarget.iOS) return;

		var plistPath = pathToBuiltProject + "/Info.plist";
		var plist = new PlistDocument();
		plist.ReadFromString(File.ReadAllText(plistPath));

		var rootDict = plist.root;
		// ここに記載したKey-ValueがXcodeのinfo.plistに反映されます
		rootDict.SetString("UIStatusBarStyle", "UIStatusBarStyleLightContent");

		rootDict.SetString("NSBluetoothAlwaysUsageDescription", "別の端末にデータを送信する為にBluetoothを使用します");
		File.WriteAllText(plistPath, plist.WriteToString());
	}
}