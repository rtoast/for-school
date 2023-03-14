using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum UISceneTitleResult
{
	CANCEL_RESULT,

	OK_RESULT,

	ALREADY_DISPLAYING,

	BAD_PARAMETERS
}

public class UINewSceneTitle : MonoBehaviour
{
	//IMPORTANT : Do not remove the Button library prefix or it will break the prefab with our overload of Button
	[SerializeField]
	private UnityEngine.UI.Button createButton;

	[SerializeField]
	private UnityEngine.UI.Button cancelButton;

	[SerializeField]
	private InputField inputField;

	[SerializeField]
	private Text textError;

	private CanvasGroup mCanvasGroup;

	private bool mDisplayed;

	private Action<UISceneTitleResult, string> mResultAction;

	private List<string> mNamesAlreadyUsed;

	// Use this for initialization
	void Start()
	{
		mCanvasGroup = GetComponent<CanvasGroup>();

		createButton.onClick.AddListener(OnClickCreate);
		cancelButton.onClick.AddListener(OnClickCancel);
		inputField.onValueChanged.AddListener(OnValueChanged);
		mNamesAlreadyUsed = new List<string>();
		HideError();
		mCanvasGroup.alpha = 0F;
		Reset();
	}

	public void StartForResult(Action<UISceneTitleResult, string> iAction, string[] iFileAlreadyPresent)
	{
		if (iAction == null || iFileAlreadyPresent == null) {
			iAction(UISceneTitleResult.BAD_PARAMETERS, string.Empty);
			Reset();
			return;
		}

		if (!mDisplayed) {
			mDisplayed = true;
			mResultAction = iAction;
			Display();

			foreach (string iFileWithExtension in iFileAlreadyPresent)
				mNamesAlreadyUsed.Add(iFileWithExtension.Replace(".xml", "").ToLower());
		} else {
			iAction(UISceneTitleResult.ALREADY_DISPLAYING, string.Empty);
			Reset();
			return;
		}
	}

	private void Display()
	{
		mCanvasGroup.alpha = 0F;
		mCanvasGroup.blocksRaycasts = true;
		createButton.interactable = false;
		StartCoroutine(Utils.FadeToAsync(1F, 0.2F, mCanvasGroup));
	}

	private void Hide()
	{
		mCanvasGroup.alpha = 1F;
		mCanvasGroup.blocksRaycasts = false;
		StartCoroutine(Utils.FadeToAsync(0F, 0.2F, mCanvasGroup));
		Reset();
	}

	private void ShowError(string iErrorMessage)
	{
		textError.text = iErrorMessage;
		textError.gameObject.SetActive(true);
	}

	private void HideError()
	{
		textError.gameObject.SetActive(false);
	}

	private void OnValueChanged(string iValue)
	{
		if (string.IsNullOrEmpty(iValue.Trim()))
			createButton.interactable = false;
		else if (mNamesAlreadyUsed != null && mNamesAlreadyUsed.Contains(iValue.ToLower())) {
			ShowError("Name already used");
			createButton.interactable = false;
		} else if (!Utils.IsAlnum(iValue.ToCharArray())) {
			ShowError("A-Z and 0-9 only");
			createButton.interactable = false;
		} else {
			HideError();
			createButton.interactable = true;
		}
	}

	private void OnClickCreate()
	{
		mDisplayed = false;
		mResultAction(UISceneTitleResult.OK_RESULT, inputField.text);
		Hide();
	}

	private void OnClickCancel()
	{
		mDisplayed = false;
		mResultAction(UISceneTitleResult.CANCEL_RESULT, string.Empty);
		Hide();
	}

	private void Reset()
	{
		mCanvasGroup.blocksRaycasts = false;
		mNamesAlreadyUsed = new List<string>();
		mResultAction = null;
		inputField.text = "";
	}
}