using System.IO;
using UnityEditor;
using UnityEngine;

public class CreateAssetBundles
{
	[MenuItem("XV/Build AssetBundles")]
	static void BuildAllAssetBundles()
	{
		Utils.CreateDirectory(Path.Combine(Application.dataPath, "AssetBundles"));
		BuildPipeline.BuildAssetBundles("Assets/AssetBundles", BuildAssetBundleOptions.None, BuildTarget.StandaloneOSX);
	}
}