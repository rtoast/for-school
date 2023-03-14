using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class UIClipResizeHandle : MonoBehaviour, IPointerDownHandler, IDragHandler
{
	private RectTransform mRectTransform;

	public enum HandleSide { HANDLE_LEFT, HANDLE_RIGHT }

	[SerializeField]
	private HandleSide side;

	private Vector2 mCurrentPointerPos;
	private Vector2 mPreviousPointerPos;
	private UIClip mClip;

	private void Start()
	{
		mRectTransform = transform.parent.GetComponent<RectTransform>();
		mClip = transform.parent.GetComponent<UIClip>();
	}

	public void OnPointerDown(PointerEventData iData)
	{
		mRectTransform.SetAsLastSibling();
		RectTransformUtility.ScreenPointToLocalPointInRectangle(mRectTransform, iData.position, iData.pressEventCamera, out mPreviousPointerPos);
	}

	public void OnDrag(PointerEventData iData)
	{
		float lSizeDelta = mRectTransform.sizeDelta.x;
		float lResizeValue = 0F;
		float lTmpSizeDelta = 0F;

		RectTransformUtility.ScreenPointToLocalPointInRectangle(mRectTransform, iData.position, iData.pressEventCamera, out mCurrentPointerPos);
		lResizeValue = GetRawResizeValue();

		lSizeDelta += lResizeValue;
		lTmpSizeDelta = GetClampedSizeDelta(lSizeDelta);

		lResizeValue -= (lSizeDelta - lTmpSizeDelta);
		lSizeDelta = lTmpSizeDelta;

		if (lSizeDelta != mRectTransform.sizeDelta.x) {
			Resize(lSizeDelta, lResizeValue);
		}
		mPreviousPointerPos = mCurrentPointerPos;
		mClip.ResizeEvent();
	}

	private float GetRawResizeValue()
	{
		if (side == HandleSide.HANDLE_LEFT) {
			return mPreviousPointerPos.x - mCurrentPointerPos.x;
		}
		else {
			return mCurrentPointerPos.x - mPreviousPointerPos.x;
		}
	}

	private float GetClampedSizeDelta(float iSizeDelta)
	{
		float lSizeMax;
		if (side == HandleSide.HANDLE_LEFT) {
			lSizeMax = (mRectTransform.localPosition.x - mClip.GetLeftLimit()) * 2F;
		}
		else {
			lSizeMax = (mClip.GetRightLimit() - mRectTransform.localPosition.x) * 2F;
		}
		return Mathf.Clamp(iSizeDelta, UIClip.sSizeMin, lSizeMax);
	}

	private void Resize(float iSizeDelta, float iResizeValue)
	{
		mRectTransform.sizeDelta = new Vector2(iSizeDelta, mRectTransform.sizeDelta.y);
		if (side == HandleSide.HANDLE_RIGHT) {
			mRectTransform.anchoredPosition += new Vector2(iResizeValue / 2.0F, 0F);
		}
		else {
			mRectTransform.anchoredPosition -= new Vector2(iResizeValue / 2.0F, 0F);
		}
	}
}
