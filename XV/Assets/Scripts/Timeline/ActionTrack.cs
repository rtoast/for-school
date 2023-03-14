using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

using AnimAction = System.Predicate<object>;

[TrackClipType(typeof(ActionAsset))]
[TrackBindingType(typeof(GameObject))]
public class ActionTrack : TrackAsset
{
	private Queue<List<AnimAction>> mActionsSets;
	private Queue<List<object>> mParamsSets;
	private Queue<string> mDescriptions;

	private static float ACTIONS_LOOP_TIME = 0.2F;

	private new void OnEnable()
	{
		TimelineEvent.StopEvent += Stop;
	}

	private void OnDisable()
	{
		TimelineEvent.StopEvent -= Stop;
	}

	private void Awake()
	{
		mActionsSets = new Queue<List<AnimAction>>();
		mParamsSets = new Queue<List<object>>();
		mDescriptions = new Queue<string>();
		if (TimelineManager.Instance != null) {
			TimelineManager.Instance.StartCoroutine(ActionQueueCallAsync());
		}
	}

	public void QueueActions(List<AnimAction> iAction, List<object> iParams, string iDescription)
	{
		mActionsSets.Enqueue(iAction);
		mParamsSets.Enqueue(iParams);
		mDescriptions.Enqueue(iDescription);
	}

	private IEnumerator ActionQueueCallAsync()
	{
		while (true) {
			if (mActionsSets.Count > 0) {
				List<AnimAction> lActions = mActionsSets.Dequeue();
				List<object> lParams = mParamsSets.Count > 0 ? mParamsSets.Dequeue() : null;
				string lDesc = mDescriptions.Dequeue();

				XV_UI.Instance.Notify(2F, lDesc);
				for (int lIndex = 0; lIndex < lActions.Count; lIndex++) {

					AnimAction lAction = lActions[lIndex];
					object lParam = GetParam(lParams, lIndex);

					if (lAction != null) {
						yield return new WaitUntil(() => lAction(lParam));
					}
					else {
						Debug.LogError("An error occured while trying to execute a Timeline Action");
					}
				}
			}
			else {
				yield return new WaitForSeconds(ACTIONS_LOOP_TIME);
			}
		}
	}

	private object GetParam(List<object> iParams, int iIndex)
	{
		if (iParams != null) {
			if (iIndex < iParams.Capacity) {
				return iParams[iIndex];
			}
		}
		return null;
	}
	
	private void Stop(TimelineEventData iData)
	{
		mActionsSets.Clear();
		mParamsSets.Clear();
		mDescriptions.Clear();
	}
}
