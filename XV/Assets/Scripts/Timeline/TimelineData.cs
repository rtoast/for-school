using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

using AnimAction = System.Predicate<object>;

public sealed class TimelineData
{
	public enum EventType { ANIMATION, TRANSLATION, ROTATION, INTERACTION };
	private TimelineAsset mTimeline;
	private PlayableDirector mDirector;
	private Dictionary<int, ActionTrack> mBindings;

	public TimelineData(TimelineAsset iTimeline, PlayableDirector iDirector)
	{
		mTimeline = iTimeline;
		mDirector = iDirector;
		mBindings = new Dictionary<int, ActionTrack>();
	}

	public int CreateEventClip(int iTrackID, AnimAction iAction, EventType iType, object iParams, double iTime)
	{
		ActionTrack lTrack = GetTrack(iTrackID);
		TimelineClip lTimelineClip = lTrack.CreateClip<ActionAsset>();
		ActionAsset lActionAsset = lTimelineClip.asset as ActionAsset;
		int lClipID = lActionAsset.GetInstanceID();

		lActionAsset.Actions.Add(iAction);
		lActionAsset.Parameters.Add(iParams);
		lActionAsset.Track = lTrack;
		lTimelineClip.duration = TimelineUtility.ClipSizeToDuration(UIClip.sSizeMin, UITrack.sSampleTrack.Size);
		if (iTime > 0D) {
			lTimelineClip.start = iTime;
		}

		TimelineEventData lEventData = new TimelineEventData(iTrackID);
		lEventData.ClipStart = lTimelineClip.start;
		lEventData.Type = iType;
		lEventData.ClipID = lClipID;
		TimelineEvent.OnAddClip(lEventData);

		RebuildTrack(iTrackID);

		return lClipID;
	}

	public int CreateInteractionEventClip(int iTrackID, List<InteractionStep> iSteps, double iTime)
	{
		ActionTrack lTrack = GetTrack(iTrackID);

		TimelineClip lTimelineClip = lTrack.CreateClip<ActionAsset>();
		ActionAsset lActionAsset = lTimelineClip.asset as ActionAsset;
		int lClipID = lActionAsset.GetInstanceID();

		foreach (InteractionStep lStep in iSteps) {
			lActionAsset.Actions.Add(lStep.action);
			lActionAsset.Parameters.Add(lStep.tag);
			lActionAsset.Track = lTrack;
		}
		lTimelineClip.duration = TimelineUtility.ClipSizeToDuration(UIClip.sSizeMin, UITrack.sSampleTrack.Size);
		if (iTime > 0D) {
			lTimelineClip.start = iTime;
		}

		TimelineEventData lEventData = new TimelineEventData(iTrackID);
		lEventData.ClipStart = lTimelineClip.start;
		lEventData.Type = EventType.INTERACTION;
		lEventData.ClipID = lClipID;
		TimelineEvent.OnAddClip(lEventData);

		RebuildTrack(iTrackID);

		return lClipID;
	}

	public ActionTrack CreateTrack(GameObject iObject)
	{
		int lID = iObject.GetInstanceID();

		ActionTrack lTrack = (ActionTrack)mTimeline.CreateTrack(typeof(ActionTrack), null, iObject.name);
		mBindings.Add(lID, lTrack);
		mDirector.SetGenericBinding(lTrack, iObject);
		TimelineEvent.OnAddTrack(new TimelineEventData(lID));
		return lTrack;
	}

	public void DestroyTrack(int iID)
	{
		ActionTrack lTrack = GetTrack(iID);
		mTimeline.DeleteTrack(lTrack);
		mBindings.Remove(iID);
		TimelineEventData lEventData = new TimelineEventData(iID);
		TimelineEvent.OnDeleteTrack(lEventData);
	}

	public void RebuildTrack(int iID)
	{
		ActionTrack lTrack = GetTrack(iID);
		List<TimelineClip> lClips = lTrack.GetClips().ToList();

 		foreach (TimelineClip lClip in lClips) {
			TimelineEventData lEventData = new TimelineEventData(iID);
			lEventData.ClipID = lClip.asset.GetInstanceID();
			lEventData.ClipStart = lClip.start;
			TimelineEvent.OnResizeClip(lEventData);
		}
	}

	public void RebuildAllTracks()
	{
		foreach (KeyValuePair<int, ActionTrack> lTrack in mBindings) {
			RebuildTrack(lTrack.Key);
		}
	}

	public ActionTrack GetTrack(int iID)
	{
		if (TrackExists(iID)) {
			return mBindings[iID];
		}
		return null;
	}

	public bool TrackExists(int iID)
	{
		return mBindings.ContainsKey(iID);
	}

	public void CheckEmptyTrack(int iID)
	{
		ActionTrack lTrack = GetTrack(iID);
		if (lTrack != null && lTrack.GetClips().Count() == 0) {
			DestroyTrack(iID);
		}
	}

	public GameObject GetBinding(int iID)
	{
		ActionTrack lTrack = GetTrack(iID);
		GameObject lObject = (GameObject)mDirector.GetGenericBinding(lTrack);
		return lObject;
	}

	public string GetClipDescription(TimelineEventData iData)
	{
		ActionTrack lTrack = GetTrack(iData.TrackID);
		TimelineClip lClip = lTrack.GetClips().ToList().Find(i => i.asset.GetInstanceID() == iData.ClipID);
		if (lClip != null) {
			ActionAsset lAsset = lClip.asset as ActionAsset;
			return lAsset.Description;
		}
		return "";
	}

	public void SetClipDescription(TimelineEventData iData, string iDescription)
	{
		ActionTrack lTrack = GetTrack(iData.TrackID);
		TimelineClip lClip = lTrack.GetClips().ToList().Find(i => i.asset.GetInstanceID() == iData.ClipID);
		if (lClip != null) {
			ActionAsset lAsset = lClip.asset as ActionAsset;
			lAsset.Description = iDescription;
		}
	}
}
