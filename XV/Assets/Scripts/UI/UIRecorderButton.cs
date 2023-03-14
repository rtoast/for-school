using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(UnityEngine.UI.Button))]
[RequireComponent(typeof(Image))]
public class UIRecorderButton : MonoBehaviour
{
	private Image mImage;
	private UnityEngine.UI.Button mButton;
	private Text mText;

	private Recorder Recorder { get { return GameManager.Instance.Recorder; } }

	private void Awake()
	{
		mImage = GetComponent<Image>();
		mButton = GetComponent<UnityEngine.UI.Button>();
		mText = GetComponentInChildren<Text>();
	}

	private void StartRecord()
	{
		Recorder.StartRecord();
		mText.text = "Stop Recording";
		mText.color = Color.white;
		mImage.color = new Color32(188, 0, 0, 100);
	}

	private void StopRecord()
	{
		Recorder.ReleaseRecord();
		mButton.interactable = false;
		StartCoroutine(UpdateComputingRecorderAsync());

	}

	private IEnumerator UpdateComputingRecorderAsync()
	{
		int lDots = 0;
		while (Recorder.RecorderStatus == RecorderStatus.COMPUTING) {
			mText.text = "Computing";
			for (int lIndex = 0; lIndex < lDots; lIndex++) {
				mText.text += ".";
			}
			lDots++;
			if (lDots > 3) {
				lDots = 0;
			}
			yield return new WaitForSeconds(0.2F);
		}
		mText.text = "Start Recording";
		mText.color = Color.black;
		mImage.color = Color.white;
		mButton.interactable = true;
	}

	public void ToggleRecord()
	{
		if (Recorder.RecorderStatus == RecorderStatus.RECORDING) {
			StopRecord();
		}
		else if (Recorder.RecorderStatus == RecorderStatus.AVAILABLE) {
			StartRecord();
		}
	}
}
