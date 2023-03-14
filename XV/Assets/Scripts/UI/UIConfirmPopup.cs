using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum UIConfirmPopupResult
{
	LEFT_RESULT,

	RIGHT_RESULT,

	ALREADY_DISPLAYING
}

public class UIConfirmPopup : MonoBehaviour
{
	[SerializeField]
	private Text confirmText;

	[SerializeField]
	private UnityEngine.UI.Button leftButton;

	[SerializeField]
	private UnityEngine.UI.Button rightButton;

	private Text leftButtonText;

	private Text rightButtonText;

	private CanvasGroup mCanvasGroup;

	private bool mDisplayed;

	private Action<UIConfirmPopupResult> mResultAction;

	// Use this for initialization
	void Start()
	{
		mCanvasGroup = GetComponent<CanvasGroup>();

		leftButton.onClick.AddListener(OnClickLeftButton);
		leftButtonText = leftButton.GetComponentInChildren<Text>();

		rightButton.onClick.AddListener(OnClickRightButton);
		rightButtonText = rightButton.GetComponentInChildren<Text>();

		mCanvasGroup.alpha = 0F;
		mCanvasGroup.blocksRaycasts = false;
	}

	public void StartForResult(Action<UIConfirmPopupResult> iResultAction,
					string iMessage = null, string iLeft = null, string iRight = null)
	{
		if (mDisplayed) {
			if (iResultAction != null)
				iResultAction(UIConfirmPopupResult.ALREADY_DISPLAYING);
			return;
		}

		if (iMessage == null)
			iMessage = "Confirm";
		if (iLeft == null)
			iLeft = "No";
		if (iRight == null)
			iRight = "Yes";

		confirmText.text = iMessage;
		leftButtonText.text = iLeft;
		rightButtonText.text = iRight;

		mResultAction = iResultAction;
		mDisplayed = true;
		Display();
	}

	private void OnClickLeftButton()
	{
		if (mResultAction != null)
			mResultAction(UIConfirmPopupResult.LEFT_RESULT);
		mDisplayed = false;
		Hide();
	}

	private void OnClickRightButton()
	{
		if (mResultAction != null)
			mResultAction(UIConfirmPopupResult.RIGHT_RESULT);
		mDisplayed = false;
		Hide();
	}

	private void Display()
	{
		mCanvasGroup.alpha = 0F;
		mCanvasGroup.blocksRaycasts = true;
		StartCoroutine(Utils.FadeToAsync(1F, 0.2F, mCanvasGroup));
	}

	private void Hide()
	{
		mCanvasGroup.alpha = 1F;
		mCanvasGroup.blocksRaycasts = false;
		StartCoroutine(Utils.FadeToAsync(0F, 0.2F, mCanvasGroup));
	}

}
