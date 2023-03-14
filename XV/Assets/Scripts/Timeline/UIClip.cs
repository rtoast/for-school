using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIClip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform mTrackRectTransform;
	private RectTransform mRectTransform;
	private float mOffset;
	private Vector2 mPointerPosition;
	public static float sSizeMin = 25F;


	[SerializeField]
	private Text nameText;
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
		get { return mRectTransform.rect.size.x; }
	}

	public UITrack Track { get; private set; }
	public TimelineData.EventType Type { get; set; }
	public int ID { get; set; }

    private void Awake() {
        mTrackRectTransform = transform.parent as RectTransform;
		mRectTransform = transform as RectTransform;
		Track = transform.GetComponentInParent<UITrack>();
    }

    public void OnPointerDown(PointerEventData iData) {
		Vector2 lLocalPointerPosition;
		if (Type != TimelineData.EventType.ANIMATION) {
        	mRectTransform.SetAsLastSibling();
		}
		RectTransformUtility.ScreenPointToLocalPointInRectangle(mTrackRectTransform, iData.position, iData.pressEventCamera, out lLocalPointerPosition);
		mOffset = mRectTransform.localPosition.x - lLocalPointerPosition.x;
    }

    public void OnDrag(PointerEventData iData) {
        Vector2 lPointerPosition = iData.position;
        Vector2 lLocalPointerPosition;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mTrackRectTransform, lPointerPosition, iData.pressEventCamera, out lLocalPointerPosition
        )) {
			float lXMin = GetLeftLimit() + Size / 2F - mOffset;
			float lXMax = GetRightLimit() - Size / 2F - mOffset;
			lLocalPointerPosition.x = Mathf.Clamp(lLocalPointerPosition.x, lXMin, lXMax);
			lLocalPointerPosition.y = 0F;
            mRectTransform.localPosition = lLocalPointerPosition + new Vector2(mOffset, 0F);
			ResizeEvent(lLocalPointerPosition.x == lXMax && mPointerPosition.x < lPointerPosition.x);
			OnPointerEnter(iData);
        }
		mPointerPosition = lPointerPosition;
    }

	public void OnPointerClick(PointerEventData iData)
	{
		if (iData.button == PointerEventData.InputButton.Middle) {
			if (Track != null) {
				Track.DeleteClip(this);
			}
		}
		else if (iData.dragging == false && iData.button == PointerEventData.InputButton.Left) {
			TimelineEventData lEventData = new TimelineEventData(Track.ID);
			if (Track != null) {
				lEventData.ClipID = ID;
				lEventData.Type = Type;
				TimelineEvent.OnGetDescription(lEventData);
			}
		}
	}

	public void Build(float iSize, float iPosition)
	{
		mRectTransform.localPosition = new Vector3(iPosition, 0F, 0F);
		mRectTransform.sizeDelta = new Vector2(iSize, mRectTransform.sizeDelta.y);
	}

	public void ResizeEvent(bool iShouldGrow = false)
	{
		Track.ReorderClips();
		
		TimelineEventData lEventData = new TimelineEventData(Track.ID);
		lEventData.Type = Type;
		lEventData.ClipIndex = Track.GetIndex(this);
		lEventData.ClipID = ID;
		lEventData.ClipLength = TimelineUtility.ClipSizeToDuration(mRectTransform.rect.size.x, Track.Size);
		lEventData.ClipStart = TimelineUtility.ClipPositionToStart(mRectTransform.localPosition.x, Track.GetLimits()) - lEventData.ClipLength / 2F;

		double lGrow = (iShouldGrow) ? TimelineManager.Instance.Duration / 30D : 0D;
		lEventData.ClipStart += (float)lGrow;
		
		TimelineEvent.OnUIResizeClip(lEventData);
	}
 

	public float GetLeftLimit()
	{
		float lLeftLimit = 0F;
		if (Track != null) {
			lLeftLimit = Track.GetLimits().x;
		}
		return lLeftLimit;
	}

	public float GetRightLimit()
	{
		float lRightLimit = 0F;
		if (Track != null) {
			lRightLimit = Track.GetLimits().y;
		}
		return lRightLimit;
	}

	public void OnPointerEnter(PointerEventData iData)
	{
		double lTime = TimelineUtility.ClipPositionToStart(mRectTransform.localPosition.x - Mathf.Floor(Size / 2F), Track.GetLimits());
		string lTimeString = TimelineUtility.FormatTime(lTime);
		string lTypeString = TimelineUtility.FormatType(Type);
		UITimelineInfo.Instance.Show(transform as RectTransform, lTypeString, lTimeString);
	}

	public void OnPointerExit(PointerEventData iData)
	{
		UITimelineInfo.Instance.Hide();
	}

	public void OnPointerUp(PointerEventData iData)
	{
		UITimelineInfo.Instance.Hide();
	}
}
