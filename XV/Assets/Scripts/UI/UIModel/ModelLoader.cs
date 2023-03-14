using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public sealed class ModelLoader
{
	public class Model
	{
		public ObjectDataSceneType Type { get; set; }
		public GameObject GameObject { get; set; }
		public Sprite Sprite { get; set; }
		public bool HumanModel { get; set; }
	};

	private static readonly ModelLoader instance = new ModelLoader();

	private Dictionary<string, Model> mModelPool;

	public int ModelPoolLenght { get; private set; }

	// Explicit static constructor to tell C# compiler
	// not to mark type as beforefieldinit
	static ModelLoader()
	{
	}

	private ModelLoader()
	{
		Utils.CreateDirectory(Application.dataPath + "/Resources/SavedData/Models/");
		if ((mModelPool = new Dictionary<string, Model>()) == null) {
			Debug.LogError("[MODEL_POOL] Error while creating the dictionary.");
			ModelPoolLenght = 0;
			return;
		}
		LoadInternModel();
		LoadHumanModel();
		LoadImportModel();
		ModelPoolLenght = mModelPool.Count;
	}

	public static ModelLoader Instance { get { return instance; } }

	// Test if it's possible to unload resources after store it in the dico
	private void LoadImportModel()
	{
		GameObject[] lModelFiles = null;
		// Load AssetBundle
		if ((lModelFiles = Utils.LoadAllAssetBundle<GameObject>(Application.dataPath + "/Resources/SavedData/Models/", (iErrorMessage) => { XV_UI.Instance.Notify(1F, iErrorMessage); })) == null)
			return;

		Sprite lImportModelSprite = Resources.Load<Sprite>("Sprites/UI/ModelsSprites/ImportModel");
		if (lImportModelSprite == null) {
			Debug.LogError("[MODEL_POOL] Error while loading sprite: Sprites/UI/ModelsSprites/ImportModel");
			return;
		}

		foreach (GameObject iModelFile in lModelFiles) {
			string lImportName = "Imp_" + iModelFile.name;

			if (mModelPool.ContainsKey(lImportName) == false) {
				iModelFile.name = lImportName;
				mModelPool.Add(lImportName, new Model {
					Type = ObjectDataSceneType.EXTERN,
					GameObject = iModelFile,
					Sprite = lImportModelSprite, });
			} else
				Debug.LogError("[MODEL_POOL] Error, a model with the same name already exist.");
		}
	}

	// Test if it's possible to unload resources after store it in the dico
	private void LoadInternModel()
	{
		Sprite lSprite = null;

		GameObject[] lModelFiles = Resources.LoadAll<GameObject>(GameManager.ITEM_BANK_PATH);
		if (lModelFiles == null) {
			Debug.LogError("[MODEL_POOL] Error while loading item:" + GameManager.ITEM_BANK_PATH);
			return;
		}

		foreach (GameObject iModelFile in lModelFiles) {
			if ((lSprite = Resources.Load<Sprite>(GameManager.UI_MODEL_SPRITE_PATH + iModelFile.name)) == null) {
				Debug.LogError("[MODEL_POOL] Error while loading sprite:" + GameManager.UI_MODEL_SPRITE_PATH + iModelFile.name);
				continue;
			}
			if (mModelPool.ContainsKey(iModelFile.name) == false) {
				mModelPool.Add(iModelFile.name, new Model {
					Type = ObjectDataSceneType.BUILT_IN,
					GameObject = iModelFile,
					Sprite = lSprite,
				});
			} else
				Debug.LogError("[MODEL_POOL] Error, model name already exist.");
		}
	}

	private void LoadHumanModel()
	{
		Sprite lSprite = null;

		GameObject lHumanModel = Resources.Load<GameObject>(GameManager.HUMAN_ITEM_PATH);
		if (lHumanModel == null) {
			Debug.LogError("[MODEL_POOL] Error while loading Human : " + GameManager.HUMAN_ITEM_PATH);
		}

		if ((lSprite = Resources.Load<Sprite>(GameManager.UI_MODEL_SPRITE_PATH + lHumanModel.name)) == null) {
			Debug.LogError("[MODEL_POOL] Error while loading Human Sprite : " + GameManager.UI_MODEL_SPRITE_PATH + lHumanModel.name);
			return;
		}
		if (mModelPool.ContainsKey(lHumanModel.name) == false) {
			mModelPool.Add(lHumanModel.name, new Model {
				Type = ObjectDataSceneType.BUILT_IN,
				GameObject = lHumanModel,
				Sprite = lSprite,
				HumanModel = true
			});
		}
	}

	// Update the model dictionary
	// Call when user import new model
	public void UpdatePool()
	{
		LoadImportModel();
	}

	public GameObject GetModelGameObject(string iName)
	{
		Model lModel;

		if (!(mModelPool.TryGetValue(iName, out lModel)))
			return null;
		return lModel.GameObject;
	}

	public Sprite GetModelSprite(string iName)
	{
		Model lModel;

		if (!(mModelPool.TryGetValue(iName, out lModel)))
			return null;
		return lModel.Sprite;
	}

	public List<Model> GetAllModel()
	{
		List<Model> lModels;

		if ((lModels = new List<Model>()) == null)
			return null;
		foreach (KeyValuePair<string, Model> lElement in mModelPool)
			lModels.Add(lElement.Value);
		return lModels;
	}
}
