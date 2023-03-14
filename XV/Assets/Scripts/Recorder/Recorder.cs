//using UnityEditor.Media;
using UnityEngine;
using System.IO;
using RockVR.Video;
using RockVR.Common;

#if UNITY_EDITOR_OSX
//using UnityEngine.Collections;
#endif

using System.Collections;

public enum RecorderStatus
{
	AVAILABLE,

	RECORDING,

	COMPUTING
}

public class Recorder : MonoBehaviour
{
	//https://docs.unity3d.com/ScriptReference/Camera.Render.html
	//https://docs.unity3d.com/ScriptReference/Camera.CopyFrom.html
	//https://docs.unity3d.com/ScriptReference/Media.MediaEncoder.html
	//https://answers.unity.com/questions/22954/how-to-save-a-picture-take-screenshot-from-a-camer.html

	public const string RES_PATH = "/Resources/RecordedVideo";

	public RecorderStatus RecorderStatus { get; private set; }

	private VideoCapture mVideoCapture;

	private void Start()
	{
     #if UNITY_EDITOR_OSX
        string a = "a+x " + PathConfig.injectorPath;
		string b = "a+x " + PathConfig.ffmpegPath;
		CmdProcess.Run("chmod", a);
		CmdProcess.Run("chmod", b);
     #endif

        mVideoCapture = Camera.main.GetComponent<VideoCapture>();
		if (mVideoCapture == null)
			Debug.Log("[RECORDER] Main camera doesn't have VideoCapture component");
        else
    		mVideoCapture.captureGUI = false;
	}

	public void StartRecord(string iPath = null)
	{
		if (RecorderStatus != RecorderStatus.AVAILABLE) {
			Debug.LogError("[RECORDER] Cannot start multiple record at same time");
			return;
		}

		Debug.Log("[RECORDER] Start record");
		RecorderStatus = RecorderStatus.RECORDING;
		VideoCaptureCtrl.instance.StartCapture();
	}

	public void ReleaseRecord()
	{
		Debug.Log("[RECORDER] Stop record");
		VideoCaptureCtrl.instance.StopCapture();
		RecorderStatus = RecorderStatus.COMPUTING;
	}

	private void Update()
	{
//		if (RecorderStatus != RecorderStatus.AVAILABLE)
//			Debug.Log(VideoCaptureCtrl.instance.status);
		if (RecorderStatus == RecorderStatus.COMPUTING) {
			if (VideoCaptureCtrl.instance.status == VideoCaptureCtrl.StatusType.FINISH) {
				RecorderStatus = RecorderStatus.AVAILABLE;
				Debug.Log("[RECORDER] Finish computing");
			}
			else
				Debug.Log("[RECORDER] Computing");
		}
	}
}
