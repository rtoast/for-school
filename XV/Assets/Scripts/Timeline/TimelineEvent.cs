using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimelineEventData
{
	public int TrackID { get; private set; }
	public TimelineData.EventType Type { get; set; }
	public int ClipIndex { get; set; }
	public int ClipID { get; set; }
	public double ClipStart { get; set; }
	public double ClipLength { get; set; }
	public string ClipName { get; set; }

	public TimelineEventData(int iTrackID)
	{
		TrackID = iTrackID;
	}
}

public static class TimelineEvent
{

	public delegate void TimelineAction(TimelineEventData iData);

	// Manager -> UI events
	public static event TimelineAction AddTrackEvent;
	public static event TimelineAction DeleteTrackEvent;
	public static event TimelineAction AddClipEvent;
	public static event TimelineAction DeleteClipEvent;
	public static event TimelineAction ResizeClipEvent;

	// UI -> Manager events
	public static event TimelineAction UIDeleteClipEvent;
	public static event TimelineAction UIResizeClipEvent;

	// Other events
	public static event TimelineAction PauseEvent;
	public static event TimelineAction StopEvent;
	public static event TimelineAction PlayEvent;
	public static event TimelineAction GetDescriptionEvent;
	public static event TimelineAction SetDescriptionEvent;

	public static void OnAddTrack(TimelineEventData iData)
	{
		if (AddTrackEvent != null) {
			AddTrackEvent(iData);
		}
	}

	public static void OnDeleteTrack(TimelineEventData iData)
	{
		if (DeleteTrackEvent != null) {
			DeleteTrackEvent(iData);
		}
	}

	public static void OnAddClip(TimelineEventData iData)
	{
		if (AddClipEvent != null) {
			AddClipEvent(iData);
		}
	}

	public static void OnDeleteClip(TimelineEventData iData)
	{
		if (DeleteClipEvent != null) {
			DeleteClipEvent(iData);
		}
	}

	public static void OnResizeClip(TimelineEventData iData)
	{
		if (ResizeClipEvent != null) {
			ResizeClipEvent(iData);
		}
	}

	public static void OnUIResizeClip(TimelineEventData iData)
	{
		if (UIResizeClipEvent != null) {
			UIResizeClipEvent(iData);
		}
	}

	public static void OnUIDeleteClip(TimelineEventData iData)
	{
		if (UIDeleteClipEvent != null) {
			UIDeleteClipEvent(iData);
		}
	}

	public static void OnPause(TimelineEventData iData)
	{
		if (PauseEvent != null) {
			PauseEvent(iData);
		}
	}

	public static void OnStop(TimelineEventData iData)
	{
		if (StopEvent != null) {
			StopEvent(iData);
		}
	}

	public static void OnPlay(TimelineEventData iData)
	{
		if (PlayEvent != null) {
			PlayEvent(iData);
		}
	}

	public static void OnGetDescription(TimelineEventData iData)
	{
		if (GetDescriptionEvent != null) {
			GetDescriptionEvent(iData);
		}
	}

	public static void OnSetDescription(TimelineEventData iData)
	{
		if (SetDescriptionEvent != null) {
			SetDescriptionEvent(iData);
		}
	}
}
