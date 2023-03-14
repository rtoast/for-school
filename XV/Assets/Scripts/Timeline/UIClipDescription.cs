using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIClipDescription : MonoBehaviour
{
	private CanvasGroup mCanvasGroup;

	[SerializeField]
	private InputField input;

	[SerializeField]
	private UnityEngine.UI.Button okButton;

	private TimelineEventData mCurrentData;

	private void Start()
	{
		mCanvasGroup = GetComponent<CanvasGroup>();
		okButton.onClick.AddListener(SetDescription);
	}

	private void OnEnable()
	{
		TimelineEvent.GetDescriptionEvent += GetDescription;
	}

	private void OnDisable()
	{
		TimelineEvent.GetDescriptionEvent -= GetDescription;
	}

	private void GetDescription(TimelineEventData iData)
	{
		if (mCurrentData == null) {
			string lDesc = TimelineManager.Instance.GetClipDescription(iData);
			input.text = lDesc;
			mCurrentData = iData;
			SetVisibility(true);
		}
		else {
			Cancel();
		}
	}

	private void SetDescription()
	{
		TimelineManager.Instance.SetClipDescription(mCurrentData, input.text);
		Cancel();
	}

	public void Cancel()
	{
		mCurrentData = null;
		SetVisibility(false);
	}

	private void SetVisibility(bool iState)
	{
		mCanvasGroup.alpha = iState ? 1F : 0F;
		mCanvasGroup.interactable = iState;
		mCanvasGroup.blocksRaycasts = iState;
		GameManager.Instance.KeyboardDeplacementActive = !iState;
	}
}
