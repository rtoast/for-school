using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIFileBrowser : MonoBehaviour
{
	private const float NOTIFIER_DURATION = 1F;

	private const string SELECTION_FIELD = "Please select a model file to open";

	private readonly string STARTING_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

	private string mSavedDataPath;

	private bool mDisplayed;

	private CanvasGroup mCanvasGroup;

	private GameObject mFileUITemplate;

	private UIElementGridParam mFileUIParam;

	private string mPath;

	// Assets

	private Sprite mSpriteFile;

	private Sprite mSpriteFolder;

	[Header("Extern Reference")]
	[Header("Please attach: UIModelManager/UIModelManagerGrid/GridELements")]
	[SerializeField]
	private UIModelManager UILibModel;

	[Header("Intern Reference")]
	[Header("Please attach: FileBrowser/Middle/FileContainer/GridWithElements")]
	[SerializeField]
	private GameObject UIGridElement;

	//IMPORTANT : Do not remove the Button library prefix or it will break the prefab with our overload of Button

	[Header("Please attach: FileBrowser/Top/ButtonContainer/GoBackPath")]
	[SerializeField]
	private UnityEngine.UI.Button GoBack;

	[Header("Please attach: FileBrowser/Top/ButtonContainer/GoToHome")]
	[SerializeField]
	private UnityEngine.UI.Button GoHome;

	[Header("Please attach: FileBrowser/Bottom/ButtonContainer/OpenFile")]
	[SerializeField]
	private UnityEngine.UI.Button OpenFile;

	[Header("Please attach: FileBrowser/Bottom/ButtonContainer/Cancel")]
	[SerializeField]
	private UnityEngine.UI.Button Cancel;

	[Header("Please attach: FileBrowser/Top/PWD/TextField/Text")]
	[SerializeField]
	private Text PWD;

	[Header("Please attach: FileBrowser/Bottom/SelectedFile/TextField/Text")]
	[SerializeField]
	private Text SelectedFile;

    public static bool IsOpen { get; private set; }

	// Use this for initialization
	private void Start()
	{
		mSavedDataPath = Application.dataPath + "/Resources/SavedData/Models/";
		// Init some variables
		mPath = STARTING_PATH;
		if ((mCanvasGroup = GetComponent<CanvasGroup>()) != null)
			mCanvasGroup.alpha = 0F;

		gameObject.SetActive(false);
		mDisplayed = false;
        IsOpen = false;
		// Init selected file text
		SelectedFile.text = SELECTION_FIELD;
		SelectedFile.color = Utils.ROYAL_GREY;

		// Load Prefab
		mFileUITemplate = Resources.Load<GameObject>(GameManager.UI_TEMPLATE_PATH + "UIFileElementGrid");
		// Get the Script that give acces to settings
		mFileUIParam = mFileUITemplate.GetComponent<UIElementGridParam>();

		// Load Assets
		mSpriteFile = Resources.Load<Sprite>(GameManager.UI_ICON_PATH + "FileBrowser/File");
		mSpriteFolder = Resources.Load<Sprite>(GameManager.UI_ICON_PATH + "FileBrowser/Folder");

		// Button setting
		GoBack.onClick.AddListener(GoToParentDir);
		GoHome.onClick.AddListener(GoToHome);
		OpenFile.onClick.AddListener(OpenSelectedFile);
		Cancel.onClick.AddListener(HideBrowser);
	}

	// Update the list of element
	private void UpdateFiles()
	{
		PWD.text = mPath.Replace(STARTING_PATH, "");
		List<string> lDirs = new List<string>(Directory.GetFileSystemEntries(mPath));

		foreach (string lFile in lDirs) {
			FileAttributes lAttr = File.GetAttributes(lFile);
			if ((lAttr & FileAttributes.Hidden) == FileAttributes.Hidden)
				continue;
			else if ((lAttr & FileAttributes.Directory) == FileAttributes.Directory) {
				if (mFileUIParam != null) {
					mFileUIParam.Text.color = Color.white;
					mFileUIParam.Icon.sprite = mSpriteFolder;
				}
			} else {
				if (mFileUIParam != null) {
					mFileUIParam.Text.color = Utils.PALE_ORANGE;
					mFileUIParam.Icon.sprite = mSpriteFile;
				}
			}
			if (mFileUIParam != null)
				mFileUIParam.Text.text = lFile.Replace(mPath + "/", "");
			GameObject lElement = Instantiate(mFileUITemplate, UIGridElement.transform);
			lElement.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => { ElementSelection(lElement); });
		}
	}

	private void ClearFiles()
	{
		foreach (Transform lChild in UIGridElement.transform) {
			Destroy(lChild.gameObject);
		}
	}

	public void ElementSelection(GameObject iButtonClicked = null)
	{
		if (iButtonClicked) {
			Text lButtonText = iButtonClicked.GetComponentInChildren<Text>();
			if (lButtonText != null) {
				FileAttributes lAttr = File.GetAttributes(Path.Combine(mPath, lButtonText.text));
				if ((lAttr & FileAttributes.Directory) != FileAttributes.Directory)
					SelectedFile.text = lButtonText.text;
				else {
					mPath = Path.Combine(mPath, lButtonText.text);
					ClearFiles();
					UpdateFiles();
				}
			}
		}
	}

	public void GoToParentDir()
	{
		if (mPath == STARTING_PATH)
			return;
		mPath = Directory.GetParent(mPath).FullName;
		ClearFiles();
		UpdateFiles();
	}

	public void GoToHome()
	{
		mPath = STARTING_PATH;
		ClearFiles();
		UpdateFiles();
	}

	public void OpenSelectedFile()
	{
		string lPathSrc = Path.Combine(mPath, SelectedFile.text);
		string lPathDst = Path.Combine(mSavedDataPath, SelectedFile.text);

		// Test if the file exist
		if (File.Exists(lPathDst)) {
			Debug.LogError("[IMPORT MODEL] Error file exist !");
			XV_UI.Instance.Notify(NOTIFIER_DURATION, "The file has already been imported.");
			return;
		}

		// Load AssetBundle to test if the file is correct
		if (Utils.LoadAssetBundle<GameObject>(lPathSrc, (iErrorMessage) => { XV_UI.Instance.Notify(NOTIFIER_DURATION, iErrorMessage); }) == null)
			return;

		// AssetBundle loading success, and GameObject has been found
		// So save the AssetBundle into SavedData
		try {
			File.Copy(lPathSrc, lPathDst);
		} catch (Exception ex) {
			Debug.LogError("[IMPORT MODEL] Error:" + ex.Message);
			XV_UI.Instance.Notify(NOTIFIER_DURATION, "An error occurred while copying the file");
		}

		ModelLoader.Instance.UpdatePool();
		UILibModel.UpdateAvailableModel();

		AssetBundle.UnloadAllAssetBundles(true);
		HideBrowser();
		XV_UI.Instance.Notify(NOTIFIER_DURATION, "Your file has been imported.");
	}

	// This function toogle the display of the UI
	public void DisplayToogle()
	{
		// Display
		if (!mDisplayed) {
			gameObject.SetActive(true);
            IsOpen = true;
			UpdateFiles();
			mDisplayed = true;
			gameObject.SetActive(true);
            GameManager.Instance.KeyboardDeplacementActive = false;
			if (mCanvasGroup != null)
                StartCoroutine(Utils.FadeToAsync(1F, 0.4F, mCanvasGroup, () => {
                    GameManager.Instance.KeyboardDeplacementActive = false;
                }));
		}
		// Hide
		else
			HideBrowser();
	}

    public void HideBrowser()
	{
		if (mDisplayed) {
            IsOpen = false;
			mDisplayed = false;
			if (mCanvasGroup != null) {
				StartCoroutine(Utils.FadeToAsync(0F, 0.4F, mCanvasGroup, () => {
					ClearFiles();
					gameObject.SetActive(false);
                    GameManager.Instance.KeyboardDeplacementActive = true;
				}));
			}
		}
	}
}