using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

// This class impl. generic animation movement

// --- TODO ---

// some object are not correctly on the ground (ex: ConvoyeurPalettes)
// on drag & drop model, some model are shifted in position (ex: gerbeur electrique)

public sealed class MovableEntity : MonoBehaviour
{
	private enum EditionMode
	{
		NONE,

		MOVE,

		ROTATE,
	}

	public enum MoveStatus
	{
		FALSE,

		TRUE,

		END
	}

	public static readonly float MAX_SPEED_COEFF = 500F;

	public static readonly float DEFAULT_SPEED_COEFF = 100F;

	public static readonly float MIN_SPEED_COEFF = 30F;

	private const float CONSTANT_SPEED_VALUE = 3.5F;

	private const float CONSTANT_ACCELERATION_VALUE = 8F;

	private const float LIMIT = 0.5F;

	private const float ROT_SPEED = 500F;

	private AEntity mEntity;

	private Image mMoveButtonColor;

	private Image mRotateButtonColor;

	private EditionMode mEditionMode;

	private NavMeshAgent mAgent;

	private NavMeshObstacle mEntityObstacle;

	private GameObject mUITarget;

	private Renderer mUITargetRenderer;

	private GameObject mUITargetTemplate;

	private GameObject mGhostEntity;

	private float mAngle;

	private float mRotationPerformed;

	public float AngularSpeed
	{
		get
		{
			if (mAgent == null) {
				Debug.LogError("AngularSpeed have to be read/write after PostPopping");
				return -1F;
			}
			return mAgent.angularSpeed;
		}

		set
		{
			if (mAgent == null) {
				Debug.LogError("AngularSpeed have to be read/write after PostPopping");
				return;
			}
			mAgent.angularSpeed = value;
		}
	}

	public float Speed
	{
		get
		{
			if (mAgent == null) {
				Debug.LogError("Speed have to be read/write after PostPopping");
				return -1F;
			}
			return mAgent.speed;
		}

		set
		{
			if (mAgent == null) {
				Debug.LogError("Speed have to be read/write after PostPopping");
				return;
			}
			mAgent.speed = value;
		}
	}

	public List<Action> OnStartMovement { get; private set; }

	public List<Action> OnEndMovement { get; private set; }

	public MovableEntity SetEntity(AEntity iObjectEntity)
	{
		mEntity = iObjectEntity;

		// Add all this code to the PostPopping callback of ObjectEntity
		mEntity.PostPoppingAction.Add(() => {

			// Execute this at the next frame
			StartCoroutine(Utils.WaitNextFrameAsync((iObj) => {
				Button lButton;

				// Add Move button & Keep track of the button image to edit color
				lButton = iObj.CreateBubbleInfoButton(new UIBubbleInfoButton {
					Tag = "Move",
					Text = "Move",
					ClickAction = (iObject) => OnMoveClick()
				});
				mMoveButtonColor = lButton.GetComponent<Image>();

				// Add Rotate Button & Keep track of the button image to edit color
				lButton = iObj.CreateBubbleInfoButton(new UIBubbleInfoButton {
					Tag = "Rotate",
					Text = "Rotate",
					ClickAction = (iObject) => {
						Debug.LogWarning("Orienter: " + iObject.name + " has been clicked");
						OnRotateClick();
					}
				});
				mRotateButtonColor = lButton.GetComponent<Image>();

				// Add NavMeshAgent to move the object
				if ((mAgent = gameObject.AddComponent<NavMeshAgent>()) != null) {
					// Agent radius is the biggest size of the bounding box
					mAgent.radius = (iObj.Size.x > iObj.Size.z) ? (iObj.Size.x / 2) : (iObj.Size.z / 2);
					// Increase a little the radius to avoid limit of a mesh
					mAgent.radius += 0.5F;
					// Adjust the cylinder with the height position
					mAgent.baseOffset = -transform.position.y;
					// Add a limit to the target destination
					mAgent.stoppingDistance = LIMIT;
					// Disable it until is not use
					mAgent.enabled = false;
				} else {
					Debug.LogError("[MOVABLE ENTITY] Failed to add NavMeshAgent");
				}

				// Get the NavMeshObstacle to perform mutual exclusion with NavMeshAgent
				mEntityObstacle = GetComponentInChildren<NavMeshObstacle>();

				if (mEntityObstacle == null) {
					if ((mEntityObstacle = GetComponentInChildren<NavMeshObstacle>()) == null)
						Debug.LogError("NavMeshObstacle is null");
				}
			}, mEntity));

			CheckAndAddAnimationSaved();

			TimelineEvent.UIResizeClipEvent += OnDragClipEvent;
			TimelineEvent.UIDeleteClipEvent += OnDeleteClipEvent;
		});
		return this;
	}

	private void OnDragClipEvent(TimelineEventData iEvent)
	{
		if (iEvent.Type != TimelineData.EventType.INTERACTION) {
			TimeLineSerialized lTimeLineSerialized = GameManager.Instance.TimeLineSerialized;

			MovableAnimation lAnim = lTimeLineSerialized.FindMovableAnimation(iEvent.ClipID);
			if (lAnim != null) {
				lAnim.Time = iEvent.ClipStart;
				lTimeLineSerialized.Serialize();
			}
		}
	}

	private void OnDeleteClipEvent(TimelineEventData iEvent)
	{
		if (iEvent.Type != TimelineData.EventType.INTERACTION) {
			TimeLineSerialized lTimeLineSerialized = GameManager.Instance.TimeLineSerialized;

			MovableAnimation lAnim = lTimeLineSerialized.FindMovableAnimation(iEvent.ClipID);
			if (lAnim != null) {
				lTimeLineSerialized.MovableAnimationList.Remove(lAnim);
				lTimeLineSerialized.Serialize();
			}
		}
	}

	private void Start()
	{
		OnStartMovement = new List<Action>();
		OnEndMovement = new List<Action>();

		mEditionMode = EditionMode.NONE;
		mUITargetTemplate = Resources.Load<GameObject>(GameManager.UI_TEMPLATE_PATH + "UITarget");
	}

	private void Update()
	{
		if (mEditionMode == EditionMode.NONE)
			return;

		else if (mEditionMode == EditionMode.MOVE) {

			// Cancel Movement on press escape
			if (Input.GetKeyDown(KeyCode.Escape)) {
				ResetMode();
				return;
			}

			// -- Movement Preview --
			// Raycast on MousePosition to put UITarget on destination
			RaycastHit lHit;
			Ray lRay = Camera.main.ScreenPointToRay(Input.mousePosition);
			if (Physics.Raycast(lRay, out lHit, 100000, LayerMask.GetMask("dropable"))) {
				// Make UI follow mouse
				mUITarget.transform.position = new Vector3(lHit.point.x, lHit.point.y + 0.1F, lHit.point.z);

				// Change UI color according to the object Hit
				if (mUITargetRenderer != null && lHit.collider.tag == "scene")
					mUITargetRenderer.material.color = Color.green;
				else if (mUITargetRenderer != null)
					mUITargetRenderer.material.color = Color.red;
			} else if (mUITargetRenderer != null)
				mUITargetRenderer.material.color = Color.red;

			// On click leave this mode and continue animation movement adding process
			if (Input.GetMouseButtonDown(0))
				AddAndExecuteMove();

		} else if (mEditionMode == EditionMode.ROTATE) {

			/*  Explication temporaire: comment ajouter une animation de rotation. (Un peu tire par les cheveux je vous l'accorde mais les click souris etait deja pris pour translation / rotation dans ObjectEntity)
            **  Click on rotate button, the button change to red 
            **  Hold left alt key, and just move your mouse (WITHOUT CLICK) to rotate object, release left alt key when your done, (the ghost object rotation will be the final rotation of the object)
            **  press 'r' key to validate the rotation, your rotation has been saved into the timeline - you can click on 'Play'
            **  If you press escape, or if you mouse click during this process, the rotation process is stopped and nothing is add to timeline
            */

			// Cancel Rotation on press escape
			if (Input.GetKeyDown(KeyCode.Escape)) {
				ResetMode();
				return;
			}

			// Orientation preview - When LeftAlt is pressed a rotation is performed on the ghost.
			if (Input.GetKey(KeyCode.LeftAlt)) {

				// Get increment angle from mouse axis
				float lAngle = Input.GetAxis("Mouse X") * ROT_SPEED * Mathf.Deg2Rad;
				// Compute final angle & clamp it between 0 & 360
				mAngle += lAngle;
				if (mAngle > 360F)
					mAngle = mAngle % 360F;
				// Rotate the ghost around the center of the object, which is the offset rotation parent
				mGhostEntity.transform.RotateAround(gameObject.transform.position, Vector3.up, lAngle);
			}

			// When 'r' is pressed leave this mode and continue animation orientation adding process
			if (Input.GetKeyDown(KeyCode.R)) {
				Quaternion lStart = gameObject.transform.rotation;
				// Send rotation destination to the function
				AddAndExecuteRotation(gameObject.transform.rotation * Quaternion.Euler(lStart.x, lStart.y + mAngle, lStart.z));
			}

			if (Input.GetMouseButtonDown(0))
				ResetMode();
		}
	}

	private void OnDestroy()
	{
		TimelineEvent.AddClipEvent -= OnDragClipEvent;
		TimelineEvent.DeleteClipEvent -= OnDeleteClipEvent;
	}

	public void SetMoveButton(Button iButton)
	{
		if (iButton != null)
			mMoveButtonColor = iButton.GetComponent<Image>();
	}

	private void AddAndExecuteMove()
	{
		// Reset Button & Mode
		ResetMode();

		Vector3 lHitPoint = Vector3.zero;

		// Check the hit.point clicked is the ground
		if ((GetHitPointFromMouseClic(ref lHitPoint, "scene"))) {

            AnimationParameters lAP = new AnimationParameters() {
                Speed = ComputeSpeed(),
                Acceleration = ComputeAcceleration(),
                Action = () => {
                    if (mEntity.EntityParameters.Type == EntityParameters.EntityType.HUMAN)
                    {
                        if (gameObject.GetComponent<HumanInteractable>().IsBusy)
                            return true;
                    }
                    return false;
                }
            };

            // Add the code that do the animation in the Action timeline
            int lId = TimelineManager.Instance.AddAnimation(gameObject, iInfo => {
				return MoveCallback(lHitPoint, iInfo);
			}, lAP, TimelineManager.Instance.Time);

			GameManager.Instance.TimeLineSerialized.MovableAnimationList.Add(new MovableAnimation() {
				EntityGUID = mEntity.AODS.GUID,
				IsMoveAnim = true,
				TargetPosition = lHitPoint,
				Time = TimelineManager.Instance.Time,
				Speed = ComputeSpeed(),
				Acceleration = ComputeAcceleration(),
				TimeLineId = lId
			});
			GameManager.Instance.CurrentDataScene.Serialize();
		}
	}

	public bool MoveCallback(Vector3 iDestination, object iParams, Action iOnEndMovement = null)
	{
		// Check NavMesh component are present
		if (mEntityObstacle == null || mAgent == null) {
			Debug.LogError("NavMeshAgent or NavMeshObstacle are missing.");
			return true;
		}

		if (gameObject == null || iParams == null)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.STOP) {
			mAgent.enabled = false;
			return true;
		}

		if (TimelineManager.sGlobalState == TimelineManager.State.PAUSE) {
			mAgent.enabled = false;
			return false;
		}

		// Disable Obstacle
		if (mEntityObstacle.enabled) {
			mEntityObstacle.enabled = false;
			return false;
		}

		AnimationParameters lAnimParams = (AnimationParameters)iParams;

        if (lAnimParams.Action != null) {
            if (lAnimParams.Action() == true)
            {
                XV_UI.Instance.Notify(1.5F, "Cannot move if human is busy !");
                return true;
            }
        }

		if (!mAgent.enabled) {
			// Active Agent
			mAgent.enabled = true;
			// Update path and dest
			mAgent.ResetPath();
			mAgent.SetDestination(iDestination);
			// Update speed
			mAgent.speed = lAnimParams.Speed;
			mAgent.acceleration = lAnimParams.Acceleration;
		}

		// Check if we have reached the destination
		if (!mAgent.pathPending) {
			if (mAgent.remainingDistance <= mAgent.stoppingDistance) {
				if (!mAgent.hasPath || mAgent.velocity.sqrMagnitude <= float.Epsilon) {
					// Switch into obstacle mode
					mAgent.enabled = false;
					mEntityObstacle.enabled = true;
					// End of this Action

					foreach (Action lAction in OnEndMovement) {
						if (lAction != null)
							lAction();
					}

					return true;
				}
			}
		}
		foreach (Action lAction in OnStartMovement) {
			if (lAction != null)
				lAction();
		}
		return false;
	}

    private void AddAndExecuteRotation(Quaternion iTarget)
    {
        // Reset Button & Mode
        ResetMode();

        // Add the code that do the animation in the following Action
        int lId = TimelineManager.Instance.AddRotation(gameObject, (iInfo) => {
            return RotateCallback(iTarget, iInfo);
        }, new AnimationParameters(){
            Action = () =>
            {
                if (mEntity.EntityParameters.Type == EntityParameters.EntityType.HUMAN)
                {
                    if (gameObject.GetComponent<HumanInteractable>().IsBusy)
                    {
                        return true;
                    }
                }
                return false;
            }
        }, TimelineManager.Instance.Time);

		GameManager.Instance.TimeLineSerialized.MovableAnimationList.Add(new MovableAnimation() {
			EntityGUID = mEntity.AODS.GUID,
			IsRotateAnim = true,
			TargetRotation = iTarget.eulerAngles,
			Time = TimelineManager.Instance.Time,
			TimeLineId = lId
		});
		GameManager.Instance.CurrentDataScene.Serialize();
	}

	private bool RotateCallback(Quaternion iTarget, object iParams)
	{
		if (gameObject == null || iParams == null)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.STOP) {
			mAgent.enabled = false;
			return true;
		}

		if (TimelineManager.sGlobalState == TimelineManager.State.PAUSE) {
			mAgent.enabled = false;
			return false;
		}

		// The duration is 2s by default for now
		float lActionDuration = 2F;

		AnimationParameters lAnimParams = (AnimationParameters)iParams;

        if (lAnimParams.Action != null)
        {
            if (lAnimParams.Action() == true)
            {
                XV_UI.Instance.Notify(1.5F, "Cannot rotate if human is busy !");
                return true;
            }
        }

        // Update rotation performed according to speed
        mRotationPerformed += Time.deltaTime * lAnimParams.Speed;

		// Rotate to the correct amount
		gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, iTarget, mRotationPerformed / lActionDuration);

		// When the counter reach the duration, the rotation is finished
		if (mRotationPerformed > lActionDuration) {
			// End of this Action
			mRotationPerformed = 0F;
			return true;
		}
		return false;
	}

	//  Deplacement animation for all movable object
	//  UIBubleInfo `Move` is bind with this func.
	public bool OnMoveClick()
	{
		if (mEditionMode == EditionMode.NONE) {
			// Enter in move edition mode
			mEditionMode = EditionMode.MOVE;
			if (mMoveButtonColor != null)
				mMoveButtonColor.color = Color.red;
			mUITarget = Instantiate(mUITargetTemplate);

			// Get the renderer of the UI object
			if (mUITarget != null)
				mUITargetRenderer = mUITarget.GetComponent<Renderer>();

			// Warn the user to click somewhere to get a destination
			XV_UI.Instance.Notify(1F, "Click on a destination !");
		} else
			ResetMode();
		return true;
	}

	//  Rotation animation for all movable
	//  UIBubleInfo `Rotate` is bind with this func.
	private bool OnRotateClick()
	{
		if (mEditionMode == EditionMode.NONE) {
			// Enter in rotate edition mode
			mEditionMode = EditionMode.ROTATE;
            if (mRotateButtonColor != null)
                mRotateButtonColor.color = Color.red;

            // Create a Ghost clone to preview rotation
            mGhostEntity = mEntity.CreateGhostObject();

			// Cancel rotation if error
			if (mGhostEntity == null) {
				XV_UI.Instance.Notify(1.5F, "Something went wrong !");
				Debug.LogError("Error on Instantiate GameObject");
				ResetMode();
				return true;
			}

			// Warn the user to click somewhere to get an orientation
			XV_UI.Instance.Notify(2.5F, "Choose an orientation (LeftAlt + MouseMotion) Then press 'r' to validate.");
		} else
			ResetMode();
		return true;
	}

	private void CheckAndAddAnimationSaved()
	{
		List<MovableAnimation> lMovableAnimationList = GameManager.Instance.TimeLineSerialized.MovableAnimationList;
		string lMyGUID = mEntity.AODS.GUID;

		foreach (MovableAnimation lAnim in lMovableAnimationList) {
			if (lAnim.EntityGUID == lMyGUID) {

				if (lAnim.IsMoveAnim) {

					lAnim.TimeLineId = TimelineManager.Instance.AddAnimation(gameObject, iInfo => {
						return MoveCallback(lAnim.TargetPosition, iInfo);
					}, new AnimationParameters() {
						Speed = lAnim.Speed,
						Acceleration = lAnim.Acceleration,
                        Action = () =>
                        {
                            if (mEntity.EntityParameters.Type == EntityParameters.EntityType.HUMAN)
                            {
                                if (gameObject.GetComponent<HumanInteractable>().IsBusy)
                                    return true;
                            }
                            return false;
                        }
                    }, lAnim.Time);

				} else if (lAnim.IsRotateAnim) {
					// Rotation action

					Quaternion lRotation = Quaternion.Euler(lAnim.TargetRotation.x, lAnim.TargetRotation.y, lAnim.TargetRotation.z);

					lAnim.TimeLineId = TimelineManager.Instance.AddRotation(gameObject, (iInfo) => {
						return RotateCallback(lRotation, iInfo);
					}, new AnimationParameters() {
                        Action = () =>
                        {
                            if (mEntity.EntityParameters.Type == EntityParameters.EntityType.HUMAN)
                            {
                                if (gameObject.GetComponent<HumanInteractable>().IsBusy)
                                    return true;
                            }
                            return false;
                        }
                    }, lAnim.Time);
				}
			}
		}
		GameManager.Instance.TimeLineSerialized.Serialize();
	}

	/// <summary>
	/// Return the speed of this MovableEntity according to the Speed Input.
	/// The speed goes from 30% of the CONSTANT_SPEED_VALUE to 500% of the CONSTANT_SPEED_VALUE
	/// </summary>
	/// <returns>The compute speed according to speed input in GUI</returns>
	public float ComputeSpeed()
	{
		// Get ratio of the Speed input, range: [0.3 ; 5]
		float lSpeedRatio = mEntity.GetSpeedInput() / DEFAULT_SPEED_COEFF;
		// Compute speed
		return CONSTANT_SPEED_VALUE * lSpeedRatio;
	}

	/// <summary>
	/// Return the acceleration of this MovableEntity according to the Speed Input.
	/// The acceleration goes from 30% of the CONSTANT_ACCELERATION_VALUE to 500% of the CONSTANT_ACCELERATION_VALUE
	/// </summary>
	/// <returns>The compute acceleration according to speed input in GUI</returns>
	public float ComputeAcceleration()
	{
		// Get ratio of the Speed input, range: [0.3 ; 5]
		float lSpeedRatio = mEntity.GetSpeedInput() / DEFAULT_SPEED_COEFF;
		// Compute acceleration
		return CONSTANT_ACCELERATION_VALUE * lSpeedRatio;
	}

	// Reset all variable to retrieve neutral edition mode
	private void ResetMode()
	{
		// Reset Button & Mode
		if (mMoveButtonColor != null)
			mMoveButtonColor.color = Color.white;
        if (mRotateButtonColor != null)
    		mRotateButtonColor.color = Color.white;
		mEditionMode = EditionMode.NONE;
		mAngle = 0F;
		mRotationPerformed = 0F;
		if (mUITarget != null)
			Destroy(mUITarget);
		if (mGhostEntity != null)
			Destroy(mGhostEntity);
	}

	// Trace ray from camera according to mouse position, and give the hit if it's 'walkable area'
	private bool GetHitPointFromMouseClic(ref Vector3 iHitPoint, string iTag)
	{
		RaycastHit lHit;
		Ray lRay = Camera.main.ScreenPointToRay(Input.mousePosition);
		if (Physics.Raycast(lRay, out lHit, 1000, LayerMask.GetMask("dropable"))) {
			Debug.DrawRay(lRay.origin, lRay.direction * lHit.distance, Color.red, 1);
			if (lHit.collider.tag != iTag)
				return false;
			iHitPoint = lHit.point;
			return true;
		}
		return false;
	}
}