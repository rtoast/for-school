using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class XV_UI : MonoBehaviour
{

	public bool isGUILocked { get { return mIsGUILocked; } }

	public bool isGUITimelineTrackLocked { get { return mIsTimelineTrackLocked; } }

	[SerializeField]
	private UINotifier notifier;

	[SerializeField]
	private CanvasGroup UIModelManagerLockerScreen;

	[SerializeField]
	private CanvasGroup UISceneLockerScreen;

	[SerializeField]
	private CanvasGroup UIToolBarLockerScreen;

	[SerializeField]
	private CanvasGroup UITimelinePanelLockerScreen;

	[SerializeField]
	private CanvasGroup UITimelinePanelLockerScreenTrack;

	[SerializeField]
	private UIConfirmPopup uIConfirmPopup;

	[SerializeField]
	private Text sceneNameText;

	private UINotifier Notifier { get { return notifier; } }

	private static XV_UI sInstance;

	private bool mIsGUILocked;
	private bool mIsTimelineTrackLocked;

	static public XV_UI Instance
	{
		get
		{
			if (sInstance == null) {

				GameObject lGameObject = null;
				if ((lGameObject = GameObject.Find("XV_UI"))) {
					if ((sInstance = lGameObject.GetComponent<XV_UI>()))
						return sInstance;
				}
				sInstance = Resources.Load<GameObject>(GameManager.UI_TEMPLATE_PATH + "XV_UI").GetComponent<XV_UI>();
			}
			return sInstance;
		}
	}

	public Text SceneNameText { get { return sceneNameText; } }

	public UIConfirmPopup UIConfirmPopup { get { return uIConfirmPopup; } }

	private void Start()
	{
		if (sInstance == null)
			sInstance = this;

		enabled = false;
	}

	public void Notify(float iDuration, string iMessage)
	{
		if (Notifier != null)
			Notifier.Notify(iDuration, iMessage);
	}

	public void LockGUI()
	{
		if (!mIsGUILocked) {
			mIsGUILocked = true;

			if (!mIsTimelineTrackLocked) {
				LockCanvas(UITimelinePanelLockerScreenTrack);
				LockCanvas(UISceneLockerScreen);
			}
			LockCanvas(UITimelinePanelLockerScreen);
			LockCanvas(UIModelManagerLockerScreen);
			LockCanvas(UIToolBarLockerScreen);

		}
	}

	public void UnlockGUI()
	{
		if (mIsGUILocked) {
			mIsGUILocked = false;

			if (!mIsTimelineTrackLocked) {
				UnlockCanvas(UITimelinePanelLockerScreenTrack);
				UnlockCanvas(UISceneLockerScreen);
			}
			UnlockCanvas(UITimelinePanelLockerScreen);
			UnlockCanvas(UIModelManagerLockerScreen);
			UnlockCanvas(UIToolBarLockerScreen);
		}
	}

	public void LockTimelineTracks()
	{
		if (!mIsTimelineTrackLocked) {
			mIsTimelineTrackLocked = true;
			LockGUI();
		}
	}

	public void UnlockTimelineTracks()
	{
		if (mIsTimelineTrackLocked) {
			mIsTimelineTrackLocked = false;

			UnlockGUI();

			//UnlockCanvas(UIModelManagerLockerScreen);
			//UnlockCanvas(UITimelinePanelLockerScreenTrack);
			//UnlockCanvas(UIToolBarLockerScreen);
		}
	}

	private void LockCanvas(CanvasGroup iCanvasGroup)
	{
		iCanvasGroup.alpha = 0F;
		iCanvasGroup.gameObject.SetActive(true);
		StartCoroutine(Utils.FadeToAsync(1F, 0.5F, iCanvasGroup));
	}

	private void UnlockCanvas(CanvasGroup iCanvasGroup)
	{
		StartCoroutine(Utils.FadeToAsync(0F, 0.5F, iCanvasGroup,
							 () => { iCanvasGroup.gameObject.SetActive(false); }));
	}
}
