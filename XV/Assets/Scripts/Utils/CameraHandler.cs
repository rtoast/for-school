using System.Collections;
using System.Collections.Generic;
using RockVR.Video;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VideoCapture))]
public sealed class CameraHandler : MonoBehaviour
{
	public enum Mode { FREE, SUBJECTIVE, LOCKED }

	[SerializeField]
	private float standardSpeed;

	[SerializeField]
	private float fastSpeedMultiplier;

	[SerializeField]
	private float mouseSensitivity;

	[SerializeField]
	private Text mViewModeText;

	private bool mIsRepositioning;
	private float mSmoothTime;

	private Mode mViewMode;

	private Mode mCurrentMode;

	public Mode ViewMode
	{
		get { return mViewMode; }

		set
		{
			if (value == Mode.FREE) {
				mViewMode = Mode.FREE;
				if (mViewModeText != null)
					mViewModeText.text = "Camera: Free";
			} else if (value == Mode.SUBJECTIVE) {
				mViewMode = Mode.SUBJECTIVE;
				if (mViewModeText != null)
					mViewModeText.text = "Camera: Subjective";
				StartCoroutine(SetPositionAsync(new Vector3(transform.position.x, 1F, transform.position.z)));
			}
		}
	}

	public Mode CurrentMode
	{
		get { return mCurrentMode; }

		set
		{
			if (value != mCurrentMode) {
				switch (value) {
				case Mode.LOCKED:
					SetLockedMode();
					break;
				case Mode.FREE:
					SetFreeMode();
					break;
				case Mode.SUBJECTIVE:
					SetSubjectiveMode();
					break;
				}
			}
		}
	}

	public VideoCapture VideoCapture { get; private set; }


	public void Focus(Vector3 iTargetPosition)
	{
		Vector3 lTargetDir = iTargetPosition - transform.position;
		lTargetDir.Normalize();

		// The camera will position itself 10 units away from the focused object
		// This can be improved by calculating the size of the object
		Vector3 lNewPos = iTargetPosition - 10F * lTargetDir;
		StartCoroutine(SetPositionAsync(lNewPos));
		StartCoroutine(SetRotationAsync(iTargetPosition));
	}

	private void Start()
	{
		VideoCapture = GetComponent<VideoCapture>();

		ViewMode = Mode.FREE;
		CurrentMode = Mode.LOCKED;
		mIsRepositioning = false;
		mSmoothTime = 0.3F;
	}

	private void Update()
	{
		// Hold right click to go in 'view mode' (free or subjective), otherwise stay in locked mode
		if (Input.GetMouseButtonDown(1)) {
			CurrentMode = ViewMode;
		} else if (Input.GetMouseButtonUp(1)) {
			CurrentMode = Mode.LOCKED;
		}
		// Use tab to switch between 'view modes': free or subjective
		if (Input.GetKeyDown(KeyCode.Tab) && !XV_UI.Instance.isGUILocked) {
			ViewMode = (ViewMode == Mode.FREE) ? Mode.SUBJECTIVE : Mode.FREE;
		}

		if (!GameManager.Instance.KeyboardDeplacementActive)
			return;

		if (!mIsRepositioning) {
			ApplyMovement();
			if (CurrentMode != Mode.LOCKED) {
				ApplyRotation();
			}
		}
	}

	private void ApplyMovement()
	{
		bool lFastMode = Input.GetKey(KeyCode.LeftShift);
		float lBaseSpeed = lFastMode ? standardSpeed * fastSpeedMultiplier : standardSpeed;

		float lXSpeed = Input.GetAxis("Horizontal") * lBaseSpeed * Time.deltaTime;
		float lZSpeed = Input.GetAxis("Vertical") * lBaseSpeed * Time.deltaTime;

		Vector3 lXAxisMovement = transform.right * lXSpeed;
		Vector3 lZAxisMovement = transform.forward * lZSpeed;

		// Freeze Y axis movement if in subjective mode
		if (ViewMode == Mode.SUBJECTIVE) {
			lZAxisMovement.y = 0F;
		}
		// Move up / down if in free mode
		else if (ViewMode == Mode.FREE) {
			float lYSpeed = Input.GetAxis("Jump") * lBaseSpeed * Time.deltaTime;
			transform.position += Vector3.up * lYSpeed;
		}

		transform.position += lXAxisMovement;
		transform.position += lZAxisMovement;
	}

	private void ApplyRotation()
	{
		float lNewRotationX = transform.eulerAngles.y + Input.GetAxis("Mouse X") * mouseSensitivity;
		float lNewRotationY = transform.eulerAngles.x - Input.GetAxis("Mouse Y") * mouseSensitivity;
		if (lNewRotationY > 90F && lNewRotationY < 270F) {
			if (lNewRotationY < 180F) {
				lNewRotationY = 90F;
			} else {
				lNewRotationY = 270F;
			}
		}
		transform.eulerAngles = new Vector3(lNewRotationY, lNewRotationX, 0F);
	}

	private void SetLockedMode()
	{
		mCurrentMode = Mode.LOCKED;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}

	private void SetFreeMode()
	{
		mCurrentMode = Mode.FREE;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	private void SetSubjectiveMode()
	{
		mCurrentMode = Mode.SUBJECTIVE;
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	private IEnumerator SetPositionAsync(Vector3 iTarget)
	{
		Vector3 lVelocity = Vector3.zero;
		while (Vector3.Distance(transform.position, iTarget) > 0.1F) {
			mIsRepositioning = true;
			transform.position = Vector3.SmoothDamp(transform.position, iTarget, ref lVelocity, mSmoothTime);
			yield return new WaitForEndOfFrame();
		}
		mIsRepositioning = false;
		yield break;
	}

	private IEnumerator SetRotationAsync(Vector3 iTargetPosition)
	{
		float lAngularVelocity = 0F;
		Vector3 lTargetDir = iTargetPosition - transform.position;
		Quaternion lTargetRot = Quaternion.LookRotation(lTargetDir);
		float lDelta = Quaternion.Angle(transform.rotation, lTargetRot);
		while (lDelta > 0.3F) {
			mIsRepositioning = true;
			lDelta = Quaternion.Angle(transform.rotation, lTargetRot);
			if (lDelta > 0.0f) {
				float lAngle = Mathf.SmoothDampAngle(lDelta, 0.0f, ref lAngularVelocity, mSmoothTime);
				lAngle = 1.0f - lAngle / lDelta;
				transform.rotation = Quaternion.Slerp(transform.rotation, lTargetRot, lAngle);
			}
			yield return new WaitForEndOfFrame();
		}
		mIsRepositioning = false;
		yield break;
	}
}