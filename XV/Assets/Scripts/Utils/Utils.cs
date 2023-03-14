using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class Utils
{

	public static readonly Color32 ROYAL_BLUE = new Color32(65, 105, 255, 255);

	public static readonly Color32 ROYAL_GREY = new Color32(80, 80, 80, 255);

	public static readonly Color32 PALE_ORANGE = new Color32(255, 211, 131, 255);

	public static void CopyDirectory(string iSourceDirName, string iDestDirName, bool iCopySubDirs)
	{
		DirectoryInfo lDir = new DirectoryInfo(iSourceDirName);

		if (!lDir.Exists) {
			throw new DirectoryNotFoundException(
				"Source directory does not exist or could not be found: "
				+ iSourceDirName);
		}

		DirectoryInfo[] lDirs = lDir.GetDirectories();
		CreateDirectory(iDestDirName);

		FileInfo[] lFiles = lDir.GetFiles();
		foreach (FileInfo lFile in lFiles) {
			string lTempPath = Path.Combine(iDestDirName, lFile.Name);
			lFile.CopyTo(lTempPath, false);
		}

		if (iCopySubDirs) {
			foreach (DirectoryInfo lSubdir in lDirs) {
				string lTempPath = Path.Combine(iDestDirName, lSubdir.Name);
				CopyDirectory(lSubdir.FullName, lTempPath, iCopySubDirs);
			}
		}
	}

	/// <summary>
	/// Creates a directory.
	/// </summary>
	/// <param name="iFolderPath">Path to the directory.</param>
	public static bool CreateDirectory(string iFolderPath)
	{
		try {
			if (string.IsNullOrEmpty(iFolderPath))
				return false;

			if (!Directory.Exists(iFolderPath))
				Directory.CreateDirectory(iFolderPath);

			return true;
		} catch {
			return false;
		}
	}

	public static void PrintStackTrace()
	{
		StackTrace t = new StackTrace();
		UnityEngine.Debug.Log(t.ToString());
	}

	public static bool SetLayerRecursively(GameObject obj, int newLayer)
	{
		if (obj == null || newLayer < 0 || newLayer > 31)
			return false;
		obj.layer = newLayer;
		foreach (Transform child in obj.transform) {
			if (child == null)
				continue;
			SetLayerRecursively(child.gameObject, newLayer);
		}
		return true;
	}

	public static bool BrowseChildRecursively(GameObject obj, Action<GameObject> iDo)
	{
		if (obj == null)
			return false;
		iDo(obj);
		foreach (Transform child in obj.transform) {
			if (child == null)
				continue;
			BrowseChildRecursively(child.gameObject, iDo);
		}
		return true;
	}

	public static bool IsAlnum(char[] iString)
	{
		int lI = -1;
		int lLen = iString.Length;

		while (++lI < lLen) {
			if (!char.IsLetterOrDigit(iString[lI]) && !(iString[lI] == ' '))
				return false;
		}
		return true;
	}

	public static Bounds ComputeBoundingBox(GameObject iGameObject)
	{
		// Getting size and center
		MeshFilter[] lElementMeshs = iGameObject.GetComponentsInChildren<MeshFilter>();
		Bounds oBounds = new Bounds(Vector3.zero, Vector3.zero);
		foreach (MeshFilter lMesh in lElementMeshs) {
			// Bound mesh
			oBounds.Encapsulate(lMesh.sharedMesh.bounds);
		}
		return oBounds;
	}

	// Try to load all AssetBundle present in iPath
	// If iPath is not a directory, the iPath is used as an AssetBundle path
	// This function will warn the user using the Notifier when available
	public static T[] LoadAllAssetBundle<T>(string iPath, Action<string> iOnError = null) where T : UnityEngine.Object
	{
		List<T> oAssets;
		T[] lAssetBundleContent;

		if ((oAssets = new List<T>()) == null) {
			UnityEngine.Debug.LogError("[ASSET BUNDLE LOADER] Error during allocation.");
			if (iOnError != null)
				iOnError("Error during allocation.");
			return null;
		}

		// If the path is not a directory, just try to import as an AssetBundle
		FileAttributes lAttr = File.GetAttributes(iPath);
		if ((lAttr & FileAttributes.Hidden) == FileAttributes.Hidden) {
			if (iOnError != null)
				iOnError("Humm, what are you trying to do ?");
			return null;
		} else if ((lAttr & FileAttributes.Directory) != FileAttributes.Directory)
			return LoadAllAssetBundle<T>(iPath, iOnError);

		// List all file in iPath
		List<string> lDirs = new List<string>(Directory.GetFileSystemEntries(iPath));

		foreach (string lFile in lDirs) {

			// If the current file is not a directory and is not an hidden file
			lAttr = File.GetAttributes(lFile);
			if ((lAttr & FileAttributes.Hidden) == FileAttributes.Hidden)
				continue;
			else if ((lAttr & FileAttributes.Directory) != FileAttributes.Directory) {

				if ((lAssetBundleContent = LoadAssetBundle<T>(lFile)) == null)
					continue;
				else {
					foreach (T lAsset in lAssetBundleContent)
						oAssets.Add(lAsset);
				}
			}
		}
		return oAssets.ToArray();
	}

	// Try to load an AssetBundle file from iPath
	// This function will warn the user using the Notifier when available
	public static T[] LoadAssetBundle<T>(string iPath, Action<string> iOnError = null) where T : UnityEngine.Object
	{
		AssetBundle lAssetBundles = null;
		T[] oAssets = null;
		FileInfo lFileInfo = null;

		if ((lFileInfo = new FileInfo(iPath)) != null) {
			if (!string.IsNullOrEmpty(lFileInfo.Extension)) {
				UnityEngine.Debug.Log("[ASSET BUNDLE LOADER] Error choosen file is not an AssetBundle.");
				if (iOnError != null)
					iOnError("Error choosen file is not an AssetBundle.");
				return null;
			}
		}

		lAssetBundles = AssetBundle.LoadFromFile(iPath);

		// Load the AssetBundle file
		if (lAssetBundles == null) {
			UnityEngine.Debug.Log("[ASSET BUNDLE LOADER] Error while loading asset bundle:" + iPath);
			if (iOnError != null)
				iOnError("Error while loading asset bundle.");
			return null;
		}

		// Load all GameObject present in the AssetBundle
		if ((oAssets = lAssetBundles.LoadAllAssets<T>()) == null) {
			UnityEngine.Debug.Log("[ASSET BUNDLE LOADER] Error while loading " + typeof(T) + " in bundle:" + iPath);
			AssetBundle.UnloadAllAssetBundles(true);
			if (iOnError != null)
				iOnError("Error while loading" + typeof(T) + " in bundle.");
			return null;
		}
		AssetBundle.UnloadAllAssetBundles(false);
		return oAssets;
	}

	public static IEnumerator LookAtSlerpY(GameObject iGameObject, GameObject iTarget, float iDuration = 2F, float iSpeed = 1F)
	{
		float mRotationPerformed = 0F;

		while (mRotationPerformed > iDuration) {
			mRotationPerformed += Time.deltaTime * iSpeed;

			iGameObject.transform.rotation = Quaternion.Slerp(iGameObject.transform.rotation, iTarget.transform.rotation, mRotationPerformed / iDuration);
			iGameObject.transform.rotation = Quaternion.Euler(new Vector3(0f, iGameObject.transform.eulerAngles.y, 0f));
			yield return null;
		}
	}

	public static IEnumerator FadeToAsync(float iValue, float iTime, CanvasGroup iCanvasGroup, Action iOnEndFade = null)
	{
		float lAlpha = iCanvasGroup.alpha;

		for (float lTime = 0F; lTime < 1F; lTime += Time.deltaTime / iTime) {
			float newAlpha = Mathf.SmoothStep(lAlpha, iValue, lTime);
			iCanvasGroup.alpha = newAlpha;
			yield return null;
		}
		iCanvasGroup.alpha = iValue;
		if (iOnEndFade != null)
			iOnEndFade();
	}

	public static IEnumerator WaitNextFrameAsync<T>(Action<T> iAction, T iObj = null) where T : class
	{
		yield return new WaitForEndOfFrame();
		if (iAction != null)
			iAction(iObj);
	}

	public static IEnumerator WaitNextFrameAsync(Action iAction)
	{
		yield return new WaitForEndOfFrame();
		if (iAction != null)
			iAction();
	}

	public static IEnumerator WaitForAsync(float iTime, Action iAction)
	{
		yield return new WaitForSeconds(iTime);
		if (iAction != null)
			iAction();
	}

	public static IEnumerator WaitForAsync<T>(float iTime, Action<T> iAction, T iObj = null) where T : class
	{
		yield return new WaitForSeconds(iTime);
		if (iAction != null)
			iAction(iObj);
	}

    /// <summary>
    /// Logs message to the Unity console with the given color.
    /// The message will be print in normal color if iColor is null.
    /// </summary>
    /// <param name="iMessage"></param>
    /// <param name="color"></param>
    public static void DebugColor(string iMessage, string iColor = null)
    {
        if (iColor == null)
            UnityEngine.Debug.Log(iMessage);
        else
            UnityEngine.Debug.Log("<color=" + iColor + ">" + iMessage + "</color>");
    }
}