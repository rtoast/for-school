using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UITimelineInfo : MonoBehaviour
{
	public static UITimelineInfo Instance { get; private set; }

	[SerializeField]
	private Text mainText;

	[SerializeField]
	private Text subText;
	private CanvasGroup mCanvasGroup;
	private RectTransform mRectTransform;

	private void Start()
	{
		if (Instance == null) {
			Instance = this;
		}
		mCanvasGroup = GetComponent<CanvasGroup>();
		mRectTransform = transform as RectTransform;
	}

	public void Show(RectTransform iTransform, string iMainText, string iSubText)
	{
		transform.position = (Vector2)iTransform.position + new Vector2(0F, mRectTransform.sizeDelta.y / 2F + iTransform.sizeDelta.y / 2F);
		mainText.text = iMainText;
		subText.text = iSubText;
		mCanvasGroup.alpha = 1F;
		mCanvasGroup.interactable = true;
		mCanvasGroup.blocksRaycasts = true;
	}

	public void Hide()
	{
		mCanvasGroup.alpha = 0F;
		mCanvasGroup.interactable = false;
		mCanvasGroup.blocksRaycasts = false;
	}

}
