using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITimeline : MonoBehaviour {

	private UITrack mUITrackPrefab;
	private List<UITrack> mTracks;
	private Animator mAnimator;

	[SerializeField]
	private Transform contentPanel;

	[SerializeField]
	private Transform modelManager;

	private void OnEnable()
	{
		TimelineEvent.AddTrackEvent += AddTrack;
		TimelineEvent.DeleteTrackEvent += DeleteTrack;
		TimelineEvent.AddClipEvent += AddClip;
		TimelineEvent.ResizeClipEvent += ResizeClip;
	}

	private void OnDisable()
	{
		TimelineEvent.AddTrackEvent -= AddTrack;
		TimelineEvent.DeleteTrackEvent -= DeleteTrack;
		TimelineEvent.AddClipEvent -= AddClip;
		TimelineEvent.ResizeClipEvent -= ResizeClip;
	}

	private void Start()
	{
		mUITrackPrefab = Resources.Load<UITrack>(GameManager.UI_TEMPLATE_PATH + "Timeline/UITrack");
		mTracks = new List<UITrack>();
		mAnimator = GetComponent<Animator>();
	}

	private void AddTrack(TimelineEventData iData)
	{
		if (!mAnimator.GetBool("IsVisible")) {
			ToggleVisibility();
		}
		UITrack lNewTrack = Instantiate(mUITrackPrefab, contentPanel);
		lNewTrack.ID = iData.TrackID;
		GameObject lObject = TimelineManager.Instance.GetObjectFromID(iData.TrackID);
		lNewTrack.Name = (lObject != null) ? lObject.name : "Unbound object";
		mTracks.Add(lNewTrack);
	}

	private void DeleteTrack(TimelineEventData iData)
	{
		if (!mAnimator.GetBool("IsVisible")) {
			ToggleVisibility();
		}
		UITrack lTrack = mTracks.Find(iTrack => iTrack.ID == iData.TrackID);
		if (lTrack != null) {
			mTracks.Remove(lTrack);
			Destroy(lTrack.gameObject);
		}
	}
	
	private void AddClip(TimelineEventData iData)
	{
		UITrack lTrack = mTracks.Find(iTrack => iTrack.ID == iData.TrackID);
		if (lTrack != null) {
			switch (iData.Type) {
				case TimelineData.EventType.ANIMATION:
					lTrack.AddAnimationClip(iData.ClipID, iData.ClipStart);
					break;
				case TimelineData.EventType.TRANSLATION:
					lTrack.AddTranslationClip(iData.ClipID, iData.ClipStart);
					break;
				case TimelineData.EventType.ROTATION:
					lTrack.AddRotationClip(iData.ClipID, iData.ClipStart);
					break;
				case TimelineData.EventType.INTERACTION:
					lTrack.AddInteractionClip(iData.ClipID, iData.ClipStart);
					break;
			}
		}
	}

	private void ResizeClip(TimelineEventData iData)
	{
		UITrack lTrack = mTracks.Find(iTrack => iTrack.ID == iData.TrackID);
		if (lTrack != null) {
			lTrack.ResizeClip(iData);
		}
	}

	public void ToggleVisibility()
	{
		Animator lModelManagerAnimator = modelManager.GetComponent<Animator>();
		mAnimator.SetBool("IsVisible", !mAnimator.GetBool("IsVisible"));
		lModelManagerAnimator.SetBool("IsCropped", !lModelManagerAnimator.GetBool("IsCropped"));
	}
}
