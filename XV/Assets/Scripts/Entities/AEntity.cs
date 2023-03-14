using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public abstract class AEntity : MonoBehaviour
{
	public abstract bool Selected { get; set; }

	public abstract string Name { get; set; }

	public List<Action> PostPoppingAction { get; private set; }

	public virtual Vector3 Size { get { return mSize; } }

	public static List<AEntity> sAllEntites;

	public static AEntity[] AllEntities
	{
		get
		{
			if (sAllEntites != null)
				return sAllEntites.ToArray();
			else
				return new ObjectEntity[0];
		}
	}

	public static int InstantiatedEntity { get { return AllEntities.Length; } }

	public EntityParameters EntityParameters { get { return mEntityParameters; } }

	public AObjectDataScene AODS { get { return mAODS; } }

	public bool LockDestroy
	{
		get { return mUIBubbleInfo.DestroyLocked; }

		set { mUIBubbleInfo.DestroyLocked = value; }
	}

	public bool NavMeshObjstacleEnabled
	{
		get
		{
			if (mNavMeshObstacle != null)
				return mNavMeshObstacle.enabled;
			return false;
		}

		set
		{
			if (mNavMeshObstacle != null)
				mNavMeshObstacle.enabled = value;
		}
	}

	public bool LockWorldEditorDeplacement
	{
		get { return mLockEditorDeplacement; }
		set { mLockEditorDeplacement = value; }
	}

	protected UIBubbleInfo mUIBubbleInfo;

	protected EntityParameters mEntityParameters;

	protected NavMeshObstacle mNavMeshObstacle;

	protected bool mBusy;

	protected bool mLockEditorDeplacement;

	protected Vector3 mSize;

	protected DataScene mDataScene;

	private AObjectDataScene mAODS;

	private Queue<Color> mOriginalColorsMaterial;

	public abstract void Dispose();

	public abstract void SaveEntity();

	protected virtual void Awake()
	{
		PostPoppingAction = new List<Action>();
		mOriginalColorsMaterial = new Queue<Color>();
		mEntityParameters = GetComponent<EntityParameters>();

		if (mEntityParameters != null && GetComponent<AInteraction>() == null)
			gameObject.AddComponent<GenericInteractable>();

		//mAInteraction = GetComponent<AInteraction>();

		PostPoppingAction.Add(() => {
			if (mAODS.IsColored) {
				mAODS.IsColored = false; // This will be reset to true in the followed SetColored
										 // We set it to false because if IsColored is true, it will not save the default texture
				SetColored(mAODS.Color);
			}
		});
	}

	public AEntity InitDataScene(DataScene iDataScene)
	{
		mDataScene = iDataScene;
		return this;
	}

	public virtual void SetObjectDataScene(AObjectDataScene iODS)
	{
		mAODS = iODS;
	}

	protected virtual void Start()
	{
		// Adding this to all ObjectEntities
		if (sAllEntites == null)
			sAllEntites = new List<AEntity>();

		sAllEntites.Add(this);

		// SetEntity in any movable entity
		if (mEntityParameters.Type == EntityParameters.EntityType.CUPBOARD ||
				mEntityParameters.Type == EntityParameters.EntityType.TROLLEY ||
				mEntityParameters.Type == EntityParameters.EntityType.VEHICLE ||
		   		mEntityParameters.Type == EntityParameters.EntityType.HUMAN) {

			MovableEntity lMovableEntity = GetComponent<MovableEntity>();
			if (lMovableEntity == null) {
				Debug.LogError("[AENTITY] MovableEntity not found in : " + mEntityParameters.Type);
				return;
			}
			lMovableEntity.SetEntity(this);
		}
	}

	protected virtual void OnDestroy()
	{
		if (sAllEntites != null)
			sAllEntites.Remove(this);

		if (mDataScene == null || mAODS == null)
			return;
	}

	public virtual void RemoveEntity()
	{
		TimeLineSerialized lTimeLineSerialized = mDataScene.TimeLineSerialized;

		if (mEntityParameters.Type == EntityParameters.EntityType.HUMAN)
			lTimeLineSerialized.HumanInteractionList.Clear();

		ManifactureInteraction[] lManifacturableInterBuffer = lTimeLineSerialized.ManifactureInteractionList.ToArray();
		MovableAnimation[] lMovableAnimBuffer = lTimeLineSerialized.MovableAnimationList.ToArray();

		foreach (ManifactureInteraction lInter in lManifacturableInterBuffer) {
			if (lInter.EntityGUID == mAODS.GUID || lInter.TargetGUID == mAODS.GUID)
				lTimeLineSerialized.ManifactureInteractionList.Remove(lInter);
		}
		foreach (MovableAnimation lAnim in lMovableAnimBuffer) {
			if (lAnim.EntityGUID == mAODS.GUID)
				lTimeLineSerialized.MovableAnimationList.Remove(lAnim);
		}

		lTimeLineSerialized.Serialize();
	}

	public AEntity SetUIBubbleInfo(UIBubbleInfo iBubbleInfo)
	{
		mUIBubbleInfo = iBubbleInfo;

		mUIBubbleInfo.Parent = this;
		if (mAODS != null) {
			mUIBubbleInfo.SetUIName(mAODS.Name);
			mUIBubbleInfo.SetUISpeed(mAODS.Speed);
		} else
			Debug.LogError("[AENTITY] mODS is null when setting UIBubbleInfo");
		mUIBubbleInfo.RefreshCanvas();

		// Add code to OnEndEdit SpeedInput callback to serialize the value
		mUIBubbleInfo.OnEndEditSpeedCallback.Add((iNewSpeed) => {
			mAODS.Speed = iNewSpeed;
			mDataScene.Serialize();
		});
		return this;
	}

	/// <summary>
	/// The Speed input given by the user.
	/// If the user give an invalid input, or if an error occured a default value is used.
	/// Default Value: 1
	/// </summary>
	public float GetSpeedInput()
	{
		return mUIBubbleInfo.Speed;
	}

	public Button CreateBubbleInfoButton(UIBubbleInfoButton iInfoButton)
	{
		if (mUIBubbleInfo == null) {
			Debug.LogError("[AENTITY] mUIBubbleInfo is null when create button");
			return null;
		}
		return mUIBubbleInfo.CreateButton(iInfoButton);
	}

	public bool ContainsBubbleInfoButton(UIBubbleInfoButton iInfoButton)
	{
		return mUIBubbleInfo.ContainsButton(iInfoButton.Tag);
	}

	public bool ContainsBubbleInfoButton(string iTag)
	{
		return mUIBubbleInfo.ContainsButton(iTag);
	}

	public void DestroyBubbleInfoButton(UIBubbleInfoButton iInfoButton)
	{
		if (mUIBubbleInfo != null)
			mUIBubbleInfo.DestroyButton(iInfoButton.Tag);
	}

	public void DestroyBubbleInfoButton(string iTag)
	{
		if (mUIBubbleInfo != null)
			mUIBubbleInfo.DestroyButton(iTag);
	}

	public void SetUIBubbleInteractable(bool iInteractable)
	{
		mUIBubbleInfo.SetInteractable(iInteractable);
	}

	public void StashUIBubbleButtons(UIBubbleInfoButton iExcluded = null)
	{
		mUIBubbleInfo.StashButtons(iExcluded);
	}

	public void StashPopUIBubbleInfoButtons()
	{
		mUIBubbleInfo.StashPopButtons();
	}

	public static void ForEachEntities(Action<AEntity> iAction)
	{
		if (iAction == null)
			return;

		AEntity[] lEntities = AllEntities;
		foreach (AEntity lEntity in lEntities)
			iAction(lEntity);
	}

	public static void HideNoInteractable(EntityParameters.EntityType[] iTypes,
		AEntity iIgnored = null)
	{
		ForEachEntities((iEntity) => {
			if (iEntity == iIgnored)
				return;
			if (iEntity.mEntityParameters != null) {
				foreach (EntityParameters.EntityType lType in iTypes) {
					if (iEntity.mEntityParameters.Type == lType) {
						iEntity.gameObject.SetActive(true);
						return;
					}
				}
			}
			iEntity.gameObject.SetActive(false);
		});
	}

	public static void DisableHideNoInteractable()
	{
		ForEachEntities((iEntity) => {
			iEntity.gameObject.SetActive(true);
		});
	}

	public static AEntity FindGUID(string iGUID)
	{
		foreach (AEntity lEntity in sAllEntites) {
			if (lEntity.AODS.GUID == iGUID)
				return lEntity;
		}
		return null;
	}

	public void ResetColor()
	{
		if (!mAODS.IsColored)
			return;

		Utils.BrowseChildRecursively(gameObject, (iObject) => {

			Renderer lR = iObject.GetComponent<Renderer>();
			if (lR != null) {

				Material[] lMaterials = lR.materials;

				if (mAODS.IsColored) {
					foreach (Material lMaterial in lMaterials) {
						lMaterial.color = mOriginalColorsMaterial.Dequeue();
					}
				}

				lR.materials = lMaterials;
			}
		});

		mAODS.IsColored = false;
		mAODS.OriginalColorsMaterial = new List<Color>(mOriginalColorsMaterial);
		mDataScene.Serialize();
	}

	public void SetColored(Color iColor)
	{
		if (!mAODS.IsColored)
			mOriginalColorsMaterial.Clear();

		mAODS.Color = iColor;

		Utils.BrowseChildRecursively(gameObject, (iObject) => {

			Renderer lR = iObject.GetComponent<Renderer>();
			if (lR != null) {

				Material[] lMaterials = lR.materials;

				if (mAODS.IsColored) {
					foreach (Material lMaterial in lMaterials) {
						lMaterial.color = iColor;
					}
				} else if (!mAODS.IsColored) {
					foreach (Material lMaterial in lMaterials) {

						mOriginalColorsMaterial.Enqueue(lMaterial.color);
						lMaterial.color = iColor;
					}

				}

				lR.materials = lMaterials;
			}
		});

		mAODS.IsColored = true;
		mAODS.OriginalColorsMaterial = new List<Color>(mOriginalColorsMaterial);
		mDataScene.Serialize();
	}

	// This function Instantiate associated Model & make it child of OffsetRotation
	// Then all material are replace by GhostMaterial
	public GameObject CreateGhostObject()
	{
		GameObject lGameObject = null;
		GameObject oGhostObject = null;
		Material lGhostMaterial = null;

		if ((lGhostMaterial = Resources.Load<Material>(GameManager.UI_MATERIAL + "Ghost")) == null) {
			Debug.LogError("[AENTITY] Load material : 'Ghost' failed.");
			return null;
		}

		if (mAODS.Type == ObjectDataSceneType.BUILT_IN) {
			lGameObject = ModelLoader.Instance.GetModelGameObject(mAODS.PrefabName);
			if (lGameObject == null) {
				Debug.LogError("[AENTITY] Load prefab " + mAODS.PrefabName + " failed.");
				return null;
			}
		} else {
			lGameObject = ModelLoader.Instance.GetModelGameObject(mAODS.PrefabName);
			if (lGameObject == null) {
				Debug.LogError("[AENTITY] Load model " + mAODS.PrefabName + " failed.");
				return null;
			}
		}

		oGhostObject = Instantiate(lGameObject, transform.position, transform.rotation, transform.parent);

		Utils.BrowseChildRecursively(oGhostObject, (iObject) => {

			// Perform this action on each child of iObject, which is oGhostObject
			Renderer lR = iObject.GetComponent<Renderer>();
			if (lR != null) {
				Material[] lO = new Material[lR.materials.Length];
				for (int lIndex = 0; lIndex < lO.Length; lIndex++) {
					lO.SetValue(lGhostMaterial, lIndex);
				}
				lR.materials = lO;
			}

			MonoBehaviour[] lMBs = iObject.GetComponents<MonoBehaviour>();
			foreach (MonoBehaviour lMB in lMBs)
				lMB.enabled = false;
		});

		if (oGhostObject == null)
			Debug.LogError("[AENTITY] oGhostObject is null");

		return oGhostObject;
	}

	public virtual void ResetWorldState()
	{
		transform.parent = null;
		transform.localPosition = Vector3.zero;
		transform.position = mAODS.Position;
		transform.transform.eulerAngles = mAODS.Rotation;
	}

}