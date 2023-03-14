using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ObjectEntity : AEntity
{
	public static string TAG = "ObjectEntity";

	private ObjectDataScene mODS;

	private bool mSelected;
	private bool mControlPushed;
	private bool mMouseDown;

	private bool mMouseOverObjectEntity;
	private bool mMouseDragObjectEntity;

	private Vector3 mMouseOriginClick;

	public bool IsBusy { get { return mBusy; } }

	public override bool Selected
	{
		get { return mSelected; }

		set
		{
			if (!value)
				mUIBubbleInfo.Hide();
			mSelected = value;
		}
	}

	public override string Name
	{
		get { return gameObject.name; }

		set
		{
			if (string.IsNullOrEmpty(value))
				return;

			gameObject.name = value;
			name = value + "_mesh";
			mODS.Name = value;
			SaveEntity();
		}
	}

	protected override void Start()
	{
		base.Start();

		// Set tag
		gameObject.tag = TAG;

		mUIBubbleInfo.GetComponent<RectTransform>().localPosition = new Vector3(0, mSize.y + 1, 0);

		StartCoroutine(PostPoppingAsync());
	}

	private void Update()
	{
		if (!mSelected || mLockEditorDeplacement)
			return;

		// Click mouse section
		if (Input.GetKeyDown(KeyCode.Mouse0)) {
			mMouseDown = true;
			mMouseOriginClick = Input.mousePosition;
		} else if (Input.GetKeyUp(KeyCode.Mouse0))
			mMouseDown = false;

		// Left control and Icons section
		if (Input.GetKeyDown(KeyCode.LeftControl)) {
			GameManager.Instance.SetCursorRotation();
			mControlPushed = true;
			mUIBubbleInfo.SetInteractable(false);
		} else if (Input.GetKeyUp(KeyCode.LeftControl)) {
			if (mMouseOverObjectEntity)
				GameManager.Instance.SetCursorHandOver();
			else
				GameManager.Instance.SetCursorStandard();
			mUIBubbleInfo.SetInteractable(true);
			mControlPushed = false;
		}

		// Rotation section
		if (mControlPushed && mMouseDown) {
			transform.rotation = Quaternion.Euler(
				transform.rotation.eulerAngles.x,
				transform.rotation.eulerAngles.y + (Input.mousePosition.x - mMouseOriginClick.x),
				transform.rotation.eulerAngles.z);

			mMouseOriginClick = Input.mousePosition;
		}

		// Moving section
		if (mMouseDragObjectEntity && Input.mousePosition != mMouseOriginClick) {
			mMouseOriginClick = Input.mousePosition;

			RaycastHit lHit;
			Ray lRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(lRay, out lHit, 1000, LayerMask.GetMask("dropable"))) {
				Debug.DrawRay(lRay.origin, lRay.direction * lHit.distance, Color.red, 1);

				transform.position = lHit.point;
			}
		}
	}

	public override void ResetWorldState()
	{
		base.ResetWorldState();

		ManifactureInteractable lManifactureInteractable;
		if ((lManifactureInteractable = GetComponent<ManifactureInteractable>()) != null)
			lManifactureInteractable.ResetWorldState();
	}

	// Place all the code you want to execute only after all the mesh enable animations
	private IEnumerator PostPoppingAsync()
	{
		// Waiting the end of the GameManager initialization of this class
		yield return new WaitForEndOfFrame();

		while (mBusy)
			yield return null;

		Transform[] lTransforms = GetComponentsInChildren<Transform>();

		foreach (Transform childObject in lTransforms) {
			MeshFilter meshFilter = childObject.gameObject.GetComponent<MeshFilter>();

			if (meshFilter != null) {
				MouseHandler lCMH = childObject.gameObject.AddComponent<MouseHandler>();
				lCMH.OnMouseDownAction = OnMouseDown;
				lCMH.OnMouseOverAction = OnMouseOver;
				lCMH.OnMouseExitAction = OnMouseExit;
				lCMH.OnMouseDragAction = OnMouseDrag;
				lCMH.OnMouseUpAction = OnMouseUp;
			}
		}

		mUIBubbleInfo.CreateButton(new UIBubbleInfoButton {
			Tag = UIBubbleInfo.DESTROY_TAG,
			Text = "Destroy",
			ClickAction = (iObjectEntity) => {
				Dispose();
				// RemoveEntity cannot be in dispose and Destroy because
				// Dispose() is called on a scene unload
				RemoveEntity();
			}
		});

		// Add a Nav Mesh obstacle on each object
		GameObject lChildMesh = null;
		if (gameObject.transform.GetChild(0) != null) {
			lChildMesh = gameObject.transform.GetChild(0).gameObject;
		}

		if ((mNavMeshObstacle = lChildMesh.AddComponent<NavMeshObstacle>()) != null) {
			mNavMeshObstacle.center = new Vector3(0, mSize.y / 2, 0);
			mNavMeshObstacle.size = new Vector3(mSize.x, mSize.y, mSize.z);
			mNavMeshObstacle.carving = true;
		}

		foreach (Action lAction in PostPoppingAction) {
			if (lAction != null)
				lAction();
		}
	}

	// Called by unity only !
	protected override void OnDestroy()
	{
		base.OnDestroy();
	}

	// Called by XV
	public override void Dispose()
	{
		if (!mBusy)
			StartCoroutine(DestroyObjectsTimedAsync());
	}

	public override void SaveEntity()
	{
		if (mODS != null && TimelineManager.Instance.Time == 0F) {
			mODS.Position = transform.position;
			mODS.Rotation = transform.rotation.eulerAngles;
			mODS.Scale = transform.localScale;
			mDataScene.Serialize();
		}
	}

	public override void RemoveEntity()
	{
		base.RemoveEntity();

		if (mODS != null) {
			if (mDataScene.IsDataObjectsContains(mODS)) {
				mDataScene.RemoveODS(mODS);
				mDataScene.Serialize();
			} else {
				Debug.LogWarning("ODS not contained in DO");
			}
		}
	}

	private IEnumerator DestroyObjectsTimedAsync()
	{
		mBusy = true;

		Transform[] lTransforms = gameObject.GetComponentsInChildren<Transform>();
		Array.Reverse(lTransforms);

		if (lTransforms.Length > 0) {
			float lWaiting = 0.05F / lTransforms.Length;

			foreach (Transform lTransform in lTransforms) {
				if (lTransform.gameObject.tag == TAG) {
					Destroy(lTransform.gameObject);
					yield return new WaitForSeconds(lWaiting);
				}
			}
		}
		mBusy = false;
	}

	public ObjectEntity StartAnimation(bool iAnimatedPopping)
	{
		if (iAnimatedPopping && !mBusy) {

			Transform[] lTransforms = gameObject.GetComponentsInChildren<Transform>();

			if (lTransforms.Length > 0) {

				foreach (Transform lTransform in lTransforms) {
					if (lTransform.gameObject.tag == TAG) {
						lTransform.gameObject.SetActive(false);
					}
				}
			}

			gameObject.SetActive(true);
			StartCoroutine(PoppingObject(lTransforms));
		}
		return this;
	}

	private IEnumerator PoppingObject(Transform[] iTransforms)
	{
		mBusy = true;

		if (iTransforms.Length > 0) {
			float lWaiting = 0.05F / iTransforms.Length;

			foreach (Transform lTransform in iTransforms) {
				if (lTransform.gameObject.tag == TAG) {
					lTransform.gameObject.SetActive(true);
					yield return new WaitForSeconds(lWaiting);
				}
			}
		}
		if (mUIBubbleInfo != null)
			mUIBubbleInfo.RefreshCanvas();
		mBusy = false;
	}

	public override void SetObjectDataScene(AObjectDataScene iODS)
	{
		base.SetObjectDataScene(iODS);

		mODS = (ObjectDataScene)iODS;
		if (mDataScene != null) {
			if (!mDataScene.IsDataObjectsContains(mODS)) {
				mDataScene.AddODS(mODS);
				mDataScene.Serialize();
			}
		}
	}

	public ObjectEntity SetSize(Vector3 iVector)
	{
		mSize = iVector;
		return this;
	}

	// ------------------- MOUSE EVENTS

	private void OnMouseOver()
	{
		if (!mSelected || mBusy || mControlPushed || mLockEditorDeplacement)
			return;

		if (!mMouseOverObjectEntity && mSelected && !mMouseDragObjectEntity)
			GameManager.Instance.SetCursorHandOver();
		mMouseOverObjectEntity = true;
	}

	private void OnMouseDrag()
	{
		if (!mSelected || mBusy || mControlPushed || mLockEditorDeplacement)
			return;

		if (!mMouseDragObjectEntity) {
			Utils.SetLayerRecursively(this.gameObject, LayerMask.NameToLayer("Ignore Raycast"));
			mMouseOriginClick = Input.mousePosition;
			GameManager.Instance.SetCursorCatchedHand();
			mMouseDragObjectEntity = true;
		}
	}

	private void OnMouseUp()
	{
		if (mMouseDragObjectEntity) {
			mMouseDragObjectEntity = false;
			if (mMouseOverObjectEntity)
				GameManager.Instance.SetCursorHandOver();
			else
				GameManager.Instance.SetCursorStandard();
			Utils.SetLayerRecursively(this.gameObject, LayerMask.NameToLayer("dropable"));
		}

		SaveEntity();
	}

	private void OnMouseDown()
	{
		// If the click is on a GUI : 
		if (EventSystem.current.IsPointerOverGameObject() || mControlPushed)
			return;

		if (!mSelected || mBusy || mLockEditorDeplacement) {
			GameManager.Instance.SelectedEntity = this;
			mUIBubbleInfo.Display();
		} else {
			GameManager.Instance.SetCursorCatchedHand();
		}
	}

	private void OnMouseExit()
	{
		if (mSelected && mMouseOverObjectEntity) {
			mMouseOverObjectEntity = false;
			if (!mMouseDragObjectEntity && !mControlPushed)
				GameManager.Instance.SetCursorStandard();
		}
	}

	// ------------------- MOUSE EVENTS
}