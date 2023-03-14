using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class UITrack : MonoBehaviour
{
	private List<UIClip> mClips;

	private RectTransform mRectTransform;
	private UIClip UIAnimationClipPrefab;
	private UIClip UIInteractionClipPrefab;
	private UIClip UITranslationClipPrefab;
	private UIClip UIRotationClipPrefab;

	[SerializeField]
	private Text nameText;
	public static UITrack sSampleTrack;

	public int ID { get; set; }

	public string Name
	{
		get { return nameText.text; }
		set
		{
			if (!string.IsNullOrEmpty(value)) {
				nameText.text = value;
			}
		}
	}

	public float Size
	{
		get
		{
            if (this == null)
                return 1000F;
			RectTransform rectTransform = transform as RectTransform;
			return rectTransform.rect.size.x;
		}
	}

	private void OnEnable()
	{
		StartCoroutine(CheckIntegrityAsync());
	}

	private void OnDisable()
	{
		StopAllCoroutines();
	}

	private void Awake()
	{
		mRectTransform = transform.Find("Track") as RectTransform;
		mClips = new List<UIClip>();
		UIAnimationClipPrefab = Resources.Load<UIClip>(GameManager.UI_TEMPLATE_PATH + "Timeline/UIAnimationClip");
		UIInteractionClipPrefab = Resources.Load<UIClip>(GameManager.UI_TEMPLATE_PATH + "Timeline/UIInteractionClip");
		UITranslationClipPrefab = Resources.Load<UIClip>(GameManager.UI_TEMPLATE_PATH + "Timeline/UITranslationClip");
		UIRotationClipPrefab = Resources.Load<UIClip>(GameManager.UI_TEMPLATE_PATH + "Timeline/UIRotationClip");
		if (sSampleTrack == null) {
			sSampleTrack = this;
		}
	}

	private IEnumerator CheckIntegrityAsync()
	{
		while (true) {
			GameObject lObject = TimelineManager.Instance.GetObjectFromID(ID);
			if (lObject == null) {
				TimelineManager.Instance.DeleteTrack(ID);
			}
			yield return new WaitForSeconds(0.2F);
		}
	}

	public void AddAnimationClip(int iClipID, double iStart)
	{
		UIClip lClip = Instantiate(UIAnimationClipPrefab, mRectTransform);
		lClip.Type = TimelineData.EventType.ANIMATION;
		lClip.ID = iClipID;
		mClips.Add(lClip);
		BuildClip(lClip, iStart);
	}

	public void AddInteractionClip(int iClipID, double iStart)
	{
		UIClip lClip = Instantiate(UIInteractionClipPrefab, mRectTransform);
		lClip.Type = TimelineData.EventType.INTERACTION;
		lClip.ID = iClipID;
		mClips.Add(lClip);
		BuildClip(lClip, iStart);
	}

	public void AddTranslationClip(int iClipID, double iStart)
	{
		UIClip lClip = Instantiate(UITranslationClipPrefab, mRectTransform);
		lClip.Type = TimelineData.EventType.TRANSLATION;
		lClip.ID = iClipID;
		mClips.Add(lClip);
		BuildClip(lClip, iStart);
	}

	public void AddRotationClip(int iClipID, double iStart)
	{
		UIClip lClip = Instantiate(UIRotationClipPrefab, mRectTransform);
		lClip.Type = TimelineData.EventType.ROTATION;
		lClip.ID = iClipID;
		mClips.Add(lClip);
		BuildClip(lClip, iStart);
	}

	public void DeleteClip(UIClip iClip)
	{
		TimelineEventData lEventData = new TimelineEventData(ID);
		lEventData.ClipIndex = GetIndex(iClip);
		lEventData.ClipID = iClip.ID;
		lEventData.Type = iClip.Type;
		if (mClips.Remove(iClip)) {
			TimelineEvent.OnUIDeleteClip(lEventData);
			Destroy(iClip.gameObject);
		}
	}

	public void ResizeClip(TimelineEventData iData)
	{
		UIClip lClip = mClips.Find(i => i.ID == iData.ClipID);
		if (lClip != null) {
			BuildClip(lClip, iData.ClipStart);
		}
	}

	private float BuildClip(UIClip iClip, double iClipStart)
	{
		float lClipX = TimelineUtility.ClipStartToPosition(iClipStart, GetLimits());
		lClipX += UIClip.sSizeMin / 2F;
		iClip.Build(iClip.Size, lClipX);
		return lClipX;
	}

	public void ReorderClips()
	{
		List<UIClip> lTmp = mClips.OrderByDescending(lClip => {
			RectTransform lTr = lClip.transform as RectTransform;
			return lTr.localPosition.x;
		}).ToList();
		mClips = lTmp;
	}

	public int GetIndex(UIClip iClip)
	{
		return mClips.IndexOf(iClip);
	}

	public Vector2 GetLimits()
	{
		float lHalfSize = mRectTransform.rect.size.x / 2F;
		return new Vector2(-lHalfSize, lHalfSize);
	}
}
