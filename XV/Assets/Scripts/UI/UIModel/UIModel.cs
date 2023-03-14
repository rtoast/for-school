using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class UIModel : MonoBehaviour,
	IPointerEnterHandler,
	IPointerExitHandler,
	IBeginDragHandler,
	IDragHandler,
	IEndDragHandler
{
	private Image mElementColor;

	private Text mElementText;

	public ModelLoader.Model Model;

	private GameObject mSelectedElement;

	private bool mMouseOver;

	private bool mLockSelection;

	private void Start()
	{
		mSelectedElement = null;

		mElementColor = transform.Find("Frame").GetComponent<Image>();
		mElementText = GetComponentInChildren<Text>();

		if (mElementColor == null)
			Debug.LogError("[ERROR] Ui Model element doesn't contain Image!");

		if (mElementText == null)
			Debug.LogError("[ERROR] Ui Model element doesn't contain Text!");

		if (Model.HumanModel)
			Model.GameObject.GetComponent<HumanEntity>().enabled = false;
	}

	private void Update()
	{
		if (Model.HumanModel && HumanEntity.Instance != null)
			mLockSelection = true;
		else if (Model.HumanModel && HumanEntity.Instance == null)
			mLockSelection = false;
		else if (mMouseOver) {
			if (Input.GetKey(KeyCode.LeftControl))
				mLockSelection = true;
			else
				mLockSelection = false;
		}
	}

	public void OnPointerEnter(PointerEventData iEventData)
	{
		if (mElementColor)
			mElementColor.color = Utils.PALE_ORANGE;
		if (mElementText)
			mElementText.color = Utils.PALE_ORANGE;
		mMouseOver = true;
	}

	public void OnPointerExit(PointerEventData iEventData)
	{
		if (mElementColor)
			mElementColor.color = Color.white;
		if (mElementText)
			mElementText.color = Color.white;
		mMouseOver = false;
	}

	// If a selectedElement exist, cast a ray from the camera to the mouse,
	// Just cast on dropable element
	// On hit, update the selectedElement position
	public void OnDrag(PointerEventData iEventData)
	{
		if (mSelectedElement != null) {

			if (!mSelectedElement.activeSelf)
				mSelectedElement.SetActive(true);
			RaycastHit lHit;
			Ray lRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(lRay, out lHit, 1000, LayerMask.GetMask("dropable"))) {
				Debug.DrawRay(lRay.origin, lRay.direction * lHit.distance, Color.red, 1);

				mSelectedElement.transform.position = lHit.point;
			}
		}
	}

	// On End restore Layer to dropable, build the object using selectedElement, delete SelectedElement.
	public void OnEndDrag(PointerEventData eventData)
	{
		if (!Model.HumanModel) {
			ObjectDataScene lODS = new ObjectDataScene {
				Name = mElementText.text,
				PrefabName = mElementText.text,
				Type = Model.Type,
				Position = mSelectedElement.transform.position,
				Rotation = Model.GameObject.transform.rotation.eulerAngles,
				Scale = mSelectedElement.transform.localScale,
			};

			GameManager.Instance.BuildObject(lODS);
			Destroy(mSelectedElement);
		} else if (HumanEntity.Instance == null) {

			HumanDataScene lHDS = new HumanDataScene {
				Name = mElementText.text,
				PrefabName = mElementText.text,
				Position = mSelectedElement.transform.position,
				Rotation = Model.GameObject.transform.rotation.eulerAngles,
				Scale = mSelectedElement.transform.localScale
			};

			GameManager.Instance.BuildHuman(lHDS);
			Destroy(mSelectedElement);
			// Lock because this human model must not be instantiated any more.
			mLockSelection = true;
		}
	}

	// Instantiate the associated Model, disable it and ignore raycast for this object.
	public void OnBeginDrag(PointerEventData eventData)
	{
		if (mSelectedElement != null && mLockSelection)
			return;

		if (Model.HumanModel && HumanEntity.Instance != null)
			return;

		// Instantiate temporary object during drag & drop, it will follow mouse
		if ((mSelectedElement = Instantiate(Model.GameObject)) == null)
			return;

		mSelectedElement.transform.eulerAngles = Vector3.zero;

		mSelectedElement.SetActive(false);
		Utils.SetLayerRecursively(mSelectedElement, LayerMask.NameToLayer("Ignore Raycast"));

		List<Vector3> lPoints = new List<Vector3>();
		if (lPoints == null) {
			Debug.LogError("[UI_MODEL] Allocation Error.");
			return;
		}
	}
}
