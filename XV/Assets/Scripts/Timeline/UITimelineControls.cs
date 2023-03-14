using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITimelineControls : MonoBehaviour
{
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private Transform timeScale;

	[SerializeField]
	private Text maxDurationText;

	private GameObject mTimeScaleLinePrefab;

	private void Start()
	{
		slider.value = slider.minValue;
		mTimeScaleLinePrefab = Resources.Load<GameObject>(GameManager.UI_TEMPLATE_PATH + "Timeline/UITimeScaleLine");
	}

	private void Update()
	{
		slider.value = (float)TimelineManager.Instance.Time;
		float lDuration = (float)TimelineManager.Instance.Duration;
		if (!Mathf.Approximately(lDuration, slider.maxValue)) {
			SetTimeScale(lDuration.ToString());
		}
	}
	
	public void SetTimeScale(string iStringValue)
	{
		float lValue = float.Parse(iStringValue);
		if (iStringValue != null) {
			maxDurationText.text = TimelineUtility.FormatDuration(lValue);
		}
		foreach (Transform lChild in timeScale) {
			Destroy(lChild.gameObject);
		}
		for (int lCount = 0; lCount < lValue; lCount++) {
			Instantiate(mTimeScaleLinePrefab, timeScale);
		}
		slider.maxValue = lValue;
		TimelineManager.Instance.Rebuild();
	}
}
