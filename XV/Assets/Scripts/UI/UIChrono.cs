using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class UIChrono : MonoBehaviour
{

	private bool mPlayed;

	private Text mMyText;

	private Stopwatch mStopWatch;

	private void Start()
	{
		mStopWatch = new Stopwatch();

		mMyText = GetComponent<Text>();
		mMyText.text = "Chrono: " + "0.0s";
		mStopWatch.Reset();

		//Callback necessary in case stop doesnt make more than a frame
		TimelineEvent.StopEvent += OnTimelineEventStop;
	}

	private void OnDestroy()
	{
		TimelineEvent.StopEvent -= OnTimelineEventStop;
	}

	// Update is called once per frame
	void Update()
	{
		if (TimelineManager.sGlobalState == TimelineManager.State.PLAY) {
			mStopWatch.Start();
		} else if (TimelineManager.sGlobalState == TimelineManager.State.PAUSE) {
			mStopWatch.Stop();
		}

		mMyText.text = "Chrono: " + (string.Format("{0:0.00}", mStopWatch.ElapsedMilliseconds / 1000F)) + "s";
	}

	void OnTimelineEventStop(TimelineEventData iData)
	{
		mStopWatch.Stop();
		mStopWatch.Reset();
	}

}
