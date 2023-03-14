using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimelineUtility : MonoBehaviour
{
	public static double ClipSizeToDuration(float iClipSize, float iTrackSize)
	{
		double lTotalDuration = TimelineManager.Instance.Duration;
		double lDuration = lTotalDuration * iClipSize / iTrackSize;
		return lDuration;
	}

	public static float ClipDurationToSize(double iClipLength, float iTrackSize)
	{
		double lTotalDuration = TimelineManager.Instance.Duration;
		float lSize = ((float)iClipLength * iTrackSize) / (float)lTotalDuration;
		return lSize;
	}

	public static float ClipStartToPosition(double iClipStart, Vector2 iTrackLimits)
	{
		double lTotalDuration = TimelineManager.Instance.Duration;
		float lPosition = (float)iClipStart * (iTrackLimits.y - iTrackLimits.x) / ((float)lTotalDuration) + iTrackLimits.x;
		return lPosition;
	}

	public static double ClipPositionToStart(float iClipPosition, Vector2 iTrackLimits)
	{
		double lTotalDuration = TimelineManager.Instance.Duration;
		double lStart = (iClipPosition - iTrackLimits.x) / (iTrackLimits.y - iTrackLimits.x) * lTotalDuration;
		return lStart;
	}

	public static string FormatDuration(double iSeconds, int iMaxPrecision = 2)
	{
		string lStr = iSeconds.ToString();
		return lStr.Substring(0, Mathf.Min(lStr.Length, 2 + iMaxPrecision)) + "s";
	}

	public static string FormatTime(double iSeconds)
	{
		string lStr = iSeconds.ToString();
		string lSeconds = lStr;
		string lRemain = "00";
		if (lStr.IndexOf('.') != -1) {
			lSeconds = lStr.Substring(0, lStr.IndexOf('.'));
			lRemain = lStr.Substring(lStr.IndexOf('.') + 1, 2);
		}
		return lSeconds + ":" + lRemain;
	}

	public static string FormatType(TimelineData.EventType iType)
	{
		switch (iType) {
			case TimelineData.EventType.ANIMATION:
				return "Animation";
			case TimelineData.EventType.TRANSLATION:
				return "Movement";
			case TimelineData.EventType.ROTATION:
				return "Rotation";
			case TimelineData.EventType.INTERACTION:
				return "Interaction";
			default:
				return "";
		}
	}
}
