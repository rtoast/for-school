using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBubbleInfo : MonoBehaviour
{
	public static string TAG = "BubbleInfo";

    public static string DESTROY_TAG = "_DESTROY";

	private bool mDisplayed;
	private CanvasGroup mCanvasGroup;
	private ContentSizeFitter mContentSizeFitter;

	//private List<KeyValuePair<string, Button>> mButtons;
	private List<Button> mButtons;

	private Queue<UIBubbleInfoButton> mStashedButtons;

	[SerializeField]
	private GameObject GridContainer;

	[SerializeField]
	private GameObject SampleButton;

	[SerializeField]
	private GameObject ColorSelector;

	[SerializeField]
	private InputField ModelName;

	[SerializeField]
	private InputField SpeedInput;

	public List<Action<float>> OnEndEditSpeedCallback { get; private set; }

	private float mSpeed;

    private bool mDestroyLocked;

	/// <summary>
	/// The Speed coeffcient input given by the user.
	/// If the user give an invalid input, or if an error occured a default value is used.
	/// Default Value: 100
	/// </summary>
	public float Speed { get { return mSpeed; } }

    public bool DestroyLocked
    {
        get { return mDestroyLocked; }

        set
        {
            if (mDestroyLocked == value)
                return;
            if (value)
                LockDestroy();
            else
                UnlockDestroy();
            mDestroyLocked = value;
        }
    }

	public AEntity Parent { get; set; }

	// Use this for initialization
	private void Start()
	{
		mButtons = new List<Button>();
		mStashedButtons = new Queue<UIBubbleInfoButton>();
		mCanvasGroup = GetComponent<CanvasGroup>();
		mContentSizeFitter = GetComponentInChildren<ContentSizeFitter>();
		mCanvasGroup.alpha = 0F;
		mCanvasGroup.blocksRaycasts = false;
		if (SpeedInput == null) {
			Debug.LogError("SpeedInput reference is missing in UIBubbleInfo.");
			mSpeed = MovableEntity.DEFAULT_SPEED_COEFF;
			FireOnEndEditSpeedCallback();
		} else {
			// Add treatment of speed input on end edit
			SpeedInput.onEndEdit.AddListener(OnEndEditSpeed);
			// Execute callback at start (Treatment of the default value store in the InputField)
			OnEndEditSpeed(SpeedInput.text);
		}
	}

	private void Awake()
	{
		RefreshCanvas();
		OnEndEditSpeedCallback = new List<Action<float>>();
	}

	// Update is called once per frame
	private void Update()
	{
		if (mDisplayed) {
			Quaternion lLookAt = Quaternion.LookRotation(
				transform.position - Camera.main.transform.position);

			transform.rotation = Quaternion.Slerp(
				transform.rotation, lLookAt, Time.deltaTime * 5);

			if (ModelName.isFocused || (SpeedInput != null && SpeedInput.isFocused))
				GameManager.Instance.KeyboardDeplacementActive = false;
            else if (!UISceneBrowser.IsOpen && !UIFileBrowser.IsOpen)
				GameManager.Instance.KeyboardDeplacementActive = true;
		}
	}

	/// <summary>
	/// Try to parse the Speed InputField, if the input is not a number,
	/// or if it's not in range [0.1;10], the user is Notify and the default speed value is used instead.
	/// </summary>
	/// <param name="iSpeedInput"></param>
	private void OnEndEditSpeed(string iSpeedInput)
	{
		// If the user doesn't give an input, use default value.
		if (string.IsNullOrEmpty(iSpeedInput))
			mSpeed = MovableEntity.DEFAULT_SPEED_COEFF;
		else {

			try {
				mSpeed = float.Parse(iSpeedInput);
				if (mSpeed < MovableEntity.MIN_SPEED_COEFF || mSpeed > MovableEntity.MAX_SPEED_COEFF)
					mSpeed = -1F;
			} catch (Exception lException) {
				Debug.LogError("UIBubbleInfo - Error while retrieving float in SpeedInput:" + lException.Message);
				mSpeed = -1;
			}
			if (mSpeed < 0F) {
				XV_UI.Instance.Notify(2.5F, "Enter a speed coefficient between" + MovableEntity.MIN_SPEED_COEFF + "% and " + MovableEntity.MAX_SPEED_COEFF + "%.");
				SpeedInput.text = null;
				mSpeed = MovableEntity.DEFAULT_SPEED_COEFF;
			}
		}
		FireOnEndEditSpeedCallback();
	}

	private void FireOnEndEditSpeedCallback()
	{
		// Execute all code in OnEndEditSpeedCallback
		foreach (Action<float> lOnEndCallback in OnEndEditSpeedCallback) {
			lOnEndCallback(mSpeed);
		}
	}

	/// <summary>
	/// Hide the Speed Input field in the BubbleInfo.
	/// </summary>
	public void HideSpeedInput()
	{
		SpeedInput.gameObject.transform.parent.gameObject.SetActive(false);
		mSpeed = MovableEntity.DEFAULT_SPEED_COEFF;
	}

	public Button CreateButton(UIBubbleInfoButton iInfoButton)
	{
		if (iInfoButton == null)
			return null;

		GameObject lNewButton = Instantiate(SampleButton, GridContainer.transform);
		lNewButton.transform.SetSiblingIndex(0); // puts the buttons before the selectorColor line

		Button lButtonComponant = lNewButton.GetComponent<Button>();

		lButtonComponant.AttachedInfoButton = iInfoButton;

		lButtonComponant.onClick.AddListener(() => {
			if (iInfoButton.ClickAction != null)
				iInfoButton.ClickAction(Parent);
		});

		if (string.IsNullOrEmpty(iInfoButton.Tag))
			lButtonComponant.AttachedValue = "untagged";
		else
			lButtonComponant.AttachedValue = iInfoButton.Tag;

		mButtons.Add(lButtonComponant);

		lNewButton.GetComponentInChildren<Text>().text = iInfoButton.Text;
		lNewButton.name = iInfoButton.Text;
		lNewButton.SetActive(true);
		Canvas.ForceUpdateCanvases();

        if (lButtonComponant.tag == DESTROY_TAG && mDestroyLocked)
            lButtonComponant.interactable = false;
		return lButtonComponant;
	}

    private void LockDestroy()
    {
        foreach (Button lButton in mButtons) {
            if (lButton.AttachedInfoButton.Tag == DESTROY_TAG)
                lButton.interactable = false;
        }
    }

    private void UnlockDestroy()
    {
        foreach (Button lButton in mButtons) {
            if (lButton.AttachedInfoButton.Tag == DESTROY_TAG)
                lButton.interactable = true;
        }
    }

    public bool ContainsButton(string iTag)
	{
		if (string.IsNullOrEmpty(iTag))
			return false;
		foreach (Button lButton in mButtons) {
			if (iTag == (string)lButton.AttachedValue) {
				return true;
			}
		}
		return false;
	}

	public void DestroyButton(string iTag)
	{
		if (string.IsNullOrEmpty(iTag))
			return;
		foreach (Button lButton in mButtons) {
			if (iTag == (string)lButton.AttachedValue) {
				mButtons.Remove(lButton);
				Destroy(lButton.gameObject);
				Canvas.ForceUpdateCanvases();
				return;
			}
		}
	}

	public void SetUIName(string iName)
	{
		ModelName.text = iName;
	}

	public void SetUISpeed(float iSpeed)
	{
		if (SpeedInput == null) {
			mSpeed = MovableEntity.DEFAULT_SPEED_COEFF;
			return;
		}
		if (iSpeed < MovableEntity.MIN_SPEED_COEFF || iSpeed > MovableEntity.MAX_SPEED_COEFF) {
			//Debug.LogWarning("[SET UI SPEED] Given speed was wrong, the default value was used instead.");
			mSpeed = MovableEntity.DEFAULT_SPEED_COEFF;
		} else
			mSpeed = iSpeed;
		SpeedInput.text = mSpeed.ToString();
	}

	public void SetInteractable(bool iInteractable, UIBubbleInfoButton iExcluded = null)
	{
		foreach (Button lButton in mButtons) {
			if (iExcluded != null && iExcluded.Tag == (string)lButton.AttachedValue)
				continue;

            if (lButton.tag == DESTROY_TAG && mDestroyLocked)
                continue;

            lButton.interactable = iInteractable;
		}
		ModelName.interactable = iInteractable;
	}

	public void StashButtons(UIBubbleInfoButton iExcluded = null)
	{
		List<UIBubbleInfoButton> lToDestroy = new List<UIBubbleInfoButton>();

		foreach (Button lButton in mButtons) {
			if (iExcluded != null && iExcluded.Tag == (string)lButton.AttachedValue)
				continue;

			mStashedButtons.Enqueue(lButton.AttachedInfoButton);
			lToDestroy.Add(lButton.AttachedInfoButton);
		}

		foreach (UIBubbleInfoButton lButton in lToDestroy)
			DestroyButton(lButton.Tag);
	}

	public void StashPopButtons()
	{
        while (mStashedButtons.Count != 0)
            CreateButton(mStashedButtons.Dequeue());

        // relock if timeline is playing
        if (TimelineManager.sGlobalState == TimelineManager.State.PLAY ||
            TimelineManager.sGlobalState == TimelineManager.State.PAUSE)
            LockDestroy();
	}

	public void OnEndEdit()
	{
		if (string.IsNullOrEmpty(ModelName.text))
			ModelName.text = Parent.Name.TrimEnd("_mesh".ToCharArray());
		else if (Parent != null)
			Parent.Name = ModelName.text;
	}

	public void Display()
	{
		mDisplayed = true;
		mCanvasGroup.blocksRaycasts = true;
		StartCoroutine(FadeToAsync(1F, 0.4F));
        //Dont call interactable here because it reset the preset of interactable buttons
	}

	public void Hide()
	{
		OnEndEdit();
		GameManager.Instance.KeyboardDeplacementActive = true;
		mDisplayed = false;
		mCanvasGroup.blocksRaycasts = false;
		StartCoroutine(FadeToAsync(0F, 0.4F));
        //Dont call interactable here because it reset the preset of interactable buttons
    }

    public void RefreshCanvas()
	{
		if (mContentSizeFitter == null)
			return;

		StartCoroutine(Utils.WaitNextFrameAsync(() => {

			StartCoroutine(Utils.WaitNextFrameAsync(() => {
				mContentSizeFitter.enabled = true;
			}));

			mContentSizeFitter.enabled = false;
		}));
	}

	public void OnResetColorClick()
	{
		Parent.ResetColor();
		Debug.Log("reset");
	}

	public void OnRedColorClick()
	{
		Color lColor;
		ColorUtility.TryParseHtmlString("#E73F3FFF", out lColor);

		Parent.SetColored(lColor);
		Debug.Log("red");
	}

	public void OnGreenColorClick()
	{
		Color lColor;
		ColorUtility.TryParseHtmlString("#13A945FF", out lColor);

		Parent.SetColored(lColor);
		Debug.Log("green");
	}

	public void OnBlueColorClick()
	{
		Color lColor;
		ColorUtility.TryParseHtmlString("#347ADBFF", out lColor);

		Parent.SetColored(lColor);
		Debug.Log("blue");
	}

	IEnumerator FadeToAsync(float iValue, float iTime)
	{
		float lAlpha = mCanvasGroup.alpha;

		for (float lTime = 0F; lTime < 1F; lTime += Time.deltaTime / iTime) {
			float newAlpha = Mathf.SmoothStep(lAlpha, iValue, lTime);
			mCanvasGroup.alpha = newAlpha;
			yield return null;
		}
	}
}
