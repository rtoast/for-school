using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(MovableEntity))]
[RequireComponent(typeof(Animator))]
public class HumanInteractable : AInteraction
{
	private MovableEntity mMovableEntity;

	private Animator mAnimator;

	private Vector3 mItemTakenPosition;
	private Vector3 mItemPutPosition;

	private AEntity mObjectHeld;

	private ManifactureInteractable mObjectMounted;
	private ManifactureInteractable mObjectPushed;

	private ItemInteraction mTakeObjectInteraction;
	private ItemInteraction mMountObjectInteraction;
	private ItemInteraction mPushObjectInteraction;

	private UIBubbleInfoButton mTakeOffBubbleButton;
	private UIBubbleInfoButton mUnmountBubbleButton;
	private UIBubbleInfoButton mReleaseBubbleButton;

	private Action mReleaseTargetObject;

	private bool mTargetIsAvailable;

    public bool IsBusy
    {
        get { return mObjectPushed != null || mObjectMounted != null; }
    }

	protected override void Start()
	{
		base.Start();

		mMovableEntity = GetComponent<MovableEntity>();
		mAnimator = GetComponent<Animator>();

		mItemTakenPosition = new Vector3(0F, 0.813F, 0.308F);
		mItemPutPosition = new Vector3(0F, 0.039F, 0.7F);
		mReleaseTargetObject = null;
		mTargetIsAvailable = true;
	}

	protected override void PostPoppingEntity()
	{
		mMovableEntity.OnStartMovement.Add(OnStartMovement);
		mMovableEntity.OnEndMovement.Add(OnEndMovement);

		mTakeObjectInteraction = CreateInteraction(new ItemInteraction() {
			Name = "Take",
			Help = "Take an object",
			InteractWith = new EntityParameters.EntityType[] { EntityParameters.EntityType.SMALL_ITEM, EntityParameters.EntityType.MEDIUM_ITEM },
			AInteraction = this,
			Button = new UIBubbleInfoButton() {
				Text = "Take",
				Tag = name + "_TAKE_OBJECT",
				ClickAction = OnClickTakeObject
			}
		});

		mMountObjectInteraction = CreateInteraction(new ItemInteraction() {
			Name = "Mount",
			Help = "Mount an object",
			InteractWith = new EntityParameters.EntityType[] { EntityParameters.EntityType.VEHICLE },
			AInteraction = this,
			Button = new UIBubbleInfoButton() {
				Text = "Mount",
				Tag = name + "_MOUNT_OBJECT",
				ClickAction = OnClickMountObject
			},
		});

		mPushObjectInteraction = CreateInteraction(new ItemInteraction() {
			Name = "Handle",
			Help = "Handle an object",
			InteractWith = new EntityParameters.EntityType[] { EntityParameters.EntityType.TROLLEY },
			AInteraction = this,
			Button = new UIBubbleInfoButton() {
				Text = "Handle",
				Tag = name + "_HANDLE_OBJECT",
				ClickAction = OnClickPushObject
			}
		});

		mTakeOffBubbleButton = new UIBubbleInfoButton() {
			Text = "Take off",
			Tag = "TakeOffButton",
			ClickAction = OnClickTakeOffObject
		};

		mUnmountBubbleButton = new UIBubbleInfoButton() {
			Text = "Unmount",
			Tag = "UnmountButton",
			ClickAction = OnClickUnmount
		};

		mReleaseBubbleButton = new UIBubbleInfoButton() {
			Text = "Release",
			Tag = "ReleaseButton",
			ClickAction = OnClickRelease
		};

		CheckAndAddInteractionsSaved();

		TimelineEvent.UIResizeClipEvent += OnDragClipEvent;
		TimelineEvent.UIDeleteClipEvent += OnDeleteClipEvent;
		TimelineEvent.PauseEvent += OnPauseEvent;
		TimelineEvent.PlayEvent += OnPlayEvent;
		TimelineEvent.StopEvent += OnStopEvent;
	}

	private void OnStopEvent(TimelineEventData iEvent)
	{
		if (mReleaseTargetObject != null)
			mReleaseTargetObject();
	}

	private void OnPauseEvent(TimelineEventData iEvent)
	{
		if (mAnimator != null)
			mAnimator.SetFloat("GlobalSpeed", 0F);
	}

	private void OnPlayEvent(TimelineEventData iEvent)
	{
		if (mAnimator != null)
			mAnimator.SetFloat("GlobalSpeed", 1F);
	}

	private void OnDragClipEvent(TimelineEventData iEvent)
	{
		if (iEvent.Type == TimelineData.EventType.INTERACTION) {
			TimeLineSerialized lTimeLineSerialized = GameManager.Instance.TimeLineSerialized;

			HumanInteraction lInter = lTimeLineSerialized.FindHumanInteraction(iEvent.ClipID);
			if (lInter != null) {
				lInter.Time = iEvent.ClipStart;
				lTimeLineSerialized.Serialize();
			}
		}
	}

	private void OnDeleteClipEvent(TimelineEventData iEvent)
	{
		if (iEvent.Type == TimelineData.EventType.INTERACTION) {
			TimeLineSerialized lTimeLineSerialized = GameManager.Instance.TimeLineSerialized;

			HumanInteraction lInter = lTimeLineSerialized.FindHumanInteraction(iEvent.ClipID);
			if (lInter != null) {
				lTimeLineSerialized.HumanInteractionList.Remove(lInter);
				lTimeLineSerialized.Serialize();
			}
		}
	}

	private void ReleaseTargetInteractionOnDestroy(AInteraction iTarget)
	{
		if (iTarget != null)
			iTarget.ReleaseForInteraction();
		mReleaseTargetObject = null;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (mObjectHeld != null) {
			mObjectHeld.transform.parent = null;
			mObjectHeld.RemoveEntity();
			mObjectHeld.Dispose();
		}
		TimelineEvent.AddClipEvent -= OnDragClipEvent;
		TimelineEvent.DeleteClipEvent -= OnDeleteClipEvent;
		TimelineEvent.PauseEvent -= OnPauseEvent;
		TimelineEvent.PlayEvent -= OnPlayEvent;
		TimelineEvent.StopEvent -= OnStopEvent;

		if (mReleaseTargetObject != null)
			mReleaseTargetObject();
	}

	#region MountObject

	private void OnClickMountObject(AEntity iEntity)
	{
		StartCoroutine(InteractionWaitForTarget("Mount", (iTargetEntityParameters) => {

			AnimationParameters lAnimationParameters = null;
			if (iTargetEntityParameters == null)
				return;
			lAnimationParameters = new AnimationParameters() {
				TargetType = AnimationParameters.AnimationTargetType.ENTITY,
				AnimationTarget = iTargetEntityParameters.gameObject,
				Speed = mMovableEntity.ComputeSpeed(),
				Acceleration = mMovableEntity.ComputeAcceleration(),
			};

			if (lAnimationParameters == null) {
				XV_UI.Instance.Notify(2F, "An error occured when retrieving targeted object.");
				return;
			}

			List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

			lInteractionSteps.Add(new InteractionStep {
				tag = lAnimationParameters,
				action = MoveToTargetCallback
			});

			lInteractionSteps.Add(new InteractionStep {
				tag = lAnimationParameters,
				action = MountObjectCallback
			});

			int lId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, TimelineManager.Instance.Time);

			GameManager.Instance.TimeLineSerialized.HumanInteractionList.Add(new HumanInteraction() {
				InteractionType = HumanInteractionType.MOUNT,
				TargetGUID = iTargetEntityParameters.gameObject.GetComponent<AEntity>().AODS.GUID,
				Time = TimelineManager.Instance.Time,
				Speed = lAnimationParameters.Speed,
				Acceleration = lAnimationParameters.Acceleration,
				TimeLineId = lId
			});
			GameManager.Instance.CurrentDataScene.Serialize();

		}));
	}

	private bool MountObjectCallback(object iParams)
	{
		if (gameObject == null || iParams == null)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.STOP)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.PAUSE)
			return false;

		AnimationParameters lParams = (AnimationParameters)iParams;
		GameObject lTarget = (GameObject)lParams.AnimationTarget;

		if (lTarget == null || mObjectMounted != null || mObjectHeld != null || mObjectPushed != null) {
			XV_UI.Instance.Notify(2F, "Human can't mount the object.");
			return true;
		}

		ManifactureInteractable lMI = lTarget.GetComponent<ManifactureInteractable>();

		if (lMI == null) {
			XV_UI.Instance.Notify(2F, "An error occured when using target!");
			return true;
		}

		mObjectMounted = lMI;
		lMI.HoldHuman(this, HumanInteractionType.MOUNT);
		OnMount();
		return true;
	}

	private void OnClickUnmount(AEntity iEntity)
	{
		AnimationParameters lAnimationParameters = new AnimationParameters() {
			TargetType = AnimationParameters.AnimationTargetType.ENTITY,
		};

		List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

		lInteractionSteps.Add(new InteractionStep {
			tag = lAnimationParameters,
			action = UnmountObjectCallback
		});

		int lId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, TimelineManager.Instance.Time);

		GameManager.Instance.TimeLineSerialized.HumanInteractionList.Add(new HumanInteraction() {
			InteractionType = HumanInteractionType.UNMOUNT,
			Time = TimelineManager.Instance.Time,
			TimeLineId = lId
		});
		GameManager.Instance.CurrentDataScene.Serialize();
	}

	private bool UnmountObjectCallback(object iParams)
	{
		if (gameObject == null || iParams == null)
			return true;
		
        if (mObjectMounted == null) {
            XV_UI.Instance.Notify(2F, "Human can't dismount the object.");
            return true;
        }

		mObjectMounted.DropHuman(this);
		OnUnmount();
		return true;
	}

	private void OnMount()
	{
		ResetAnimator();

		mEntity.NavMeshObjstacleEnabled = false;
		mEntity.LockWorldEditorDeplacement = true;

		mMountObjectInteraction.Enabled = false;
		mPushObjectInteraction.Enabled = false;
		mTakeObjectInteraction.Enabled = false;

		if (!mEntity.ContainsBubbleInfoButton(mUnmountBubbleButton))
			mEntity.CreateBubbleInfoButton(mUnmountBubbleButton);

		mEntity.StashUIBubbleButtons(mUnmountBubbleButton);
	}

	private void OnUnmount()
	{
		ResetAnimator();

		mEntity.NavMeshObjstacleEnabled = true;
		mEntity.LockWorldEditorDeplacement = false;

		mMountObjectInteraction.Enabled = true;
		mPushObjectInteraction.Enabled = true;
		mTakeObjectInteraction.Enabled = true;

		if (mEntity.ContainsBubbleInfoButton(mUnmountBubbleButton))
			mEntity.DestroyBubbleInfoButton(mUnmountBubbleButton);
		mEntity.StashPopUIBubbleInfoButtons();

        if (mObjectMounted != null)
			mObjectMounted = null;
	}

	#endregion MountObject

	#region TakeObject

	private void OnClickTakeObject(AEntity iEntity)
	{
		StartCoroutine(InteractionWaitForTarget("Take", (iTargetEntityParameters) => {
			AnimationParameters lAnimationParameters = null;
			if (iTargetEntityParameters == null)
				return;
			lAnimationParameters = new AnimationParameters() {
				TargetType = AnimationParameters.AnimationTargetType.ENTITY,
				AnimationTarget = iTargetEntityParameters.gameObject,
				Speed = mMovableEntity.ComputeSpeed(),
				Acceleration = mMovableEntity.ComputeAcceleration(),
			};

			if (lAnimationParameters == null) {
				XV_UI.Instance.Notify(2F, "An error occured when retrieving targeted object.");
				return;
			}

			List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

			lInteractionSteps.Add(new InteractionStep {
				tag = lAnimationParameters,
				action = MoveToTargetCallback
			});

			lInteractionSteps.Add(new InteractionStep {
				tag = lAnimationParameters,
				action = TakeObjectAnimationTakeCallback
			});

			lInteractionSteps.Add(new InteractionStep {
				tag = lAnimationParameters,
				action = TakeObjectWaitAnimationEndCallback
			});

			int lId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, TimelineManager.Instance.Time);

			GameManager.Instance.TimeLineSerialized.HumanInteractionList.Add(new HumanInteraction() {
				InteractionType = HumanInteractionType.TAKE,
				TargetGUID = iTargetEntityParameters.gameObject.GetComponent<AEntity>().AODS.GUID,
				Time = TimelineManager.Instance.Time,
				Speed = lAnimationParameters.Speed,
				Acceleration = lAnimationParameters.Acceleration,
				TimeLineId = lId
			});
			GameManager.Instance.CurrentDataScene.Serialize();
		}));
	}

	private bool TakeObjectAnimationTakeCallback(object iParams)
	{
		if (gameObject == null || iParams == null || !mTargetIsAvailable)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.STOP)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.PAUSE)
			return false;

		AnimationParameters lParams = (AnimationParameters)iParams;
		GameObject lTarget = (GameObject)lParams.AnimationTarget;

		if (lTarget == null || mObjectMounted != null || mObjectHeld != null || mObjectPushed != null) {
			XV_UI.Instance.Notify(2F, "Human can't take the object.");
			return true;
		}

		mAnimator.SetTrigger("PickUp");

		if (mAnimator.GetCurrentAnimatorStateInfo(0).IsName("PickUp")) {
			mAnimator.SetBool("IdleWithBox", true);
		}

		if (mAnimator.GetCurrentAnimatorStateInfo(0).IsName("IdleWithBox")) {
			mAnimator.ResetTrigger("PickUp");

			mObjectHeld = lTarget.GetComponent<AEntity>();
			mObjectHeld.Selected = false;
			mObjectHeld.NavMeshObjstacleEnabled = false;

			lTarget.transform.parent = gameObject.transform;
			lTarget.transform.localPosition = mItemTakenPosition;
			OnHold();
			return true;
		}

		return false;
	}

	private bool TakeObjectWaitAnimationEndCallback(object iParams)
	{
		if (gameObject == null || !mTargetIsAvailable)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.STOP) {
			ResetAnimator();
			return true;
		}

		if (TimelineManager.sGlobalState == TimelineManager.State.PAUSE)
			return false;

		if (mAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime > 1)
			return true;
		return false;
	}

	private void OnClickTakeOffObject(AEntity iEntity)
	{
		AnimationParameters lAnimationParameters = new AnimationParameters() {
			TargetType = AnimationParameters.AnimationTargetType.ENTITY,
		};

		List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

		lInteractionSteps.Add(new InteractionStep {
			tag = lAnimationParameters,
			action = TakeOffObjectCallback
		});

		int lId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, TimelineManager.Instance.Time);

		GameManager.Instance.TimeLineSerialized.HumanInteractionList.Add(new HumanInteraction() {
			InteractionType = HumanInteractionType.TAKEOFF,
			Time = TimelineManager.Instance.Time,
			TimeLineId = lId
		});
		GameManager.Instance.CurrentDataScene.Serialize();
	}

	private bool TakeOffObjectCallback(object iParams)
	{
		if (gameObject == null || iParams == null)
			return true;

		if (mObjectHeld == null) {
			XV_UI.Instance.Notify(2F, "Human can't drop the object.");
			return true;
		}

		AInteraction lTargetBaseInteraction;
		if ((lTargetBaseInteraction = mObjectHeld.GetComponent<AInteraction>()) == null) {
			XV_UI.Instance.Notify(2F, "Can't check the object is available.");
			return true;
		}
		lTargetBaseInteraction.ReleaseForInteraction();
		mReleaseTargetObject = null;

		ResetAnimator();
		mObjectHeld.transform.localPosition = mItemPutPosition;
		mObjectHeld.transform.parent = null;
		OnUnhold();
		return true;
	}

	private void OnHold()
	{
		mMountObjectInteraction.Enabled = false;
		mPushObjectInteraction.Enabled = false;
		mTakeObjectInteraction.Enabled = false;

		if (!mEntity.ContainsBubbleInfoButton(mTakeOffBubbleButton))
			mEntity.CreateBubbleInfoButton(mTakeOffBubbleButton);
	}

	private void OnUnhold()
	{
		mEntity.DestroyBubbleInfoButton(mTakeOffBubbleButton);
		mMountObjectInteraction.Enabled = true;
		mPushObjectInteraction.Enabled = true;
		mTakeObjectInteraction.Enabled = true;
		if (mObjectHeld != null) {
			mObjectHeld.NavMeshObjstacleEnabled = true;
			mObjectHeld = null;
		} else
			Debug.LogWarning("[HUMAN INTERACTABLE] Object Held shouldn't be null in OnUnhold");
	}

	#endregion TakeObject

	#region PushObject

	private void OnClickPushObject(AEntity iEntity)
	{
		StartCoroutine(InteractionWaitForTarget("Handle", (iTargetEntityParameters) => {

			AnimationParameters lAnimationParameters = null;
			if (iTargetEntityParameters == null)
				return;
			lAnimationParameters = new AnimationParameters() {
				TargetType = AnimationParameters.AnimationTargetType.ENTITY,
				AnimationTarget = iTargetEntityParameters.gameObject,
				Speed = mMovableEntity.ComputeSpeed(),
				Acceleration = mMovableEntity.ComputeAcceleration(),
			};

			if (lAnimationParameters == null) {
				XV_UI.Instance.Notify(2F, "An error occured when retrieving targeted object.");
				return;
			}

			List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

			lInteractionSteps.Add(new InteractionStep {
				tag = lAnimationParameters,
				action = MoveToTargetCallback
			});

			lInteractionSteps.Add(new InteractionStep {
				tag = lAnimationParameters,
				action = PushObjectCallback
			});

			int lId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps);

			GameManager.Instance.TimeLineSerialized.HumanInteractionList.Add(new HumanInteraction() {
				InteractionType = HumanInteractionType.PUSH,
				TargetGUID = iTargetEntityParameters.gameObject.GetComponent<AEntity>().AODS.GUID,
				Time = TimelineManager.Instance.Time,
				Speed = lAnimationParameters.Speed,
				Acceleration = lAnimationParameters.Acceleration,
				TimeLineId = lId
			});
			GameManager.Instance.CurrentDataScene.Serialize();

		}));
	}

	private bool PushObjectCallback(object iParams)
	{
		if (gameObject == null || iParams == null)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.STOP)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.PAUSE)
			return false;

		AnimationParameters lParams = (AnimationParameters)iParams;
		GameObject lTarget = (GameObject)lParams.AnimationTarget;
		if (lTarget == null || mObjectMounted != null || mObjectHeld != null || mObjectPushed != null) {
			XV_UI.Instance.Notify(2F, "Human can't handle the object.");
			return true;
		}

		ManifactureInteractable lMI = lTarget.GetComponent<ManifactureInteractable>();

		if (lMI == null) {
			XV_UI.Instance.Notify(2F, "An error occured when using target!");
			return true;
		}

		mObjectPushed = lMI;
		lMI.HoldHuman(this, HumanInteractionType.PUSH, OnManufactureStartMove, OnManufactureEndMove);
		OnPush();
		return true;
	}

	private void OnManufactureStartMove()
	{
		if (mObjectPushed != null)
			mAnimator.SetBool("Pushing", true);
	}

	private void OnManufactureEndMove()
	{
		if (mObjectPushed != null)
			mAnimator.SetBool("Pushing", false);
	}

	private void OnClickRelease(AEntity iEntity)
	{
		AnimationParameters lAnimationParameters = new AnimationParameters() {
			TargetType = AnimationParameters.AnimationTargetType.ENTITY,
		};

		List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

		lInteractionSteps.Add(new InteractionStep {
			tag = lAnimationParameters,
			action = ReleaseObjectCallback
		});

		int lId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps);

		GameManager.Instance.TimeLineSerialized.HumanInteractionList.Add(new HumanInteraction() {
			InteractionType = HumanInteractionType.STOP_PUSH,
			Time = TimelineManager.Instance.Time,
			TimeLineId = lId
		});
		GameManager.Instance.CurrentDataScene.Serialize();
	}

	private bool ReleaseObjectCallback(object iParams)
	{
		if (gameObject == null || iParams == null)
			return true;

        if (mObjectPushed == null) {
            XV_UI.Instance.Notify(2F, "Human can't release object.");
            return true;
        }

		mObjectPushed.DropHuman(this, OnManufactureStartMove, OnManufactureEndMove);
		OnRelease();
		return true;
	}

	private void OnPush()
	{
		ResetAnimator();

		mEntity.NavMeshObjstacleEnabled = false;
		mEntity.LockWorldEditorDeplacement = true;

		mMountObjectInteraction.Enabled = false;
		mPushObjectInteraction.Enabled = false;
		mTakeObjectInteraction.Enabled = false;

		if (!mEntity.ContainsBubbleInfoButton(mReleaseBubbleButton))
			mEntity.CreateBubbleInfoButton(mReleaseBubbleButton);

		mEntity.StashUIBubbleButtons(mReleaseBubbleButton);
	}

	private void OnRelease()
	{
		ResetAnimator();

		mEntity.NavMeshObjstacleEnabled = true;
		mEntity.LockWorldEditorDeplacement = false;

		mMountObjectInteraction.Enabled = true;
		mPushObjectInteraction.Enabled = true;
		mTakeObjectInteraction.Enabled = true;

		if (mEntity.ContainsBubbleInfoButton(mReleaseBubbleButton))
			mEntity.DestroyBubbleInfoButton(mReleaseBubbleButton);
		mEntity.StashPopUIBubbleInfoButtons();

		if (mObjectPushed != null)
			mObjectPushed = null;
	}

	#endregion PushObject

	private bool MoveToTargetCallback(object iParams)
	{
		if (gameObject == null || iParams == null)
			return true;

		AnimationParameters lParams = (AnimationParameters)iParams;
		GameObject lTarget = (GameObject)lParams.AnimationTarget;

		if (lTarget == null || mObjectMounted != null || mObjectPushed != null || mObjectHeld != null) {
			XV_UI.Instance.Notify(2F, "Human can't move to the object.");
			return true;
		}

		AInteraction lTargetBaseInteraction;
		if ((lTargetBaseInteraction = lTarget.GetComponent<AInteraction>()) == null) {
			XV_UI.Instance.Notify(2F, "Can't check the object is available.");
			mTargetIsAvailable = false;
			return true;
		}

		if (!(lTargetBaseInteraction.ReserveForInteraction(gameObject.GetHashCode()))) {
			mTargetIsAvailable = false;
			return true;
		}
		mTargetIsAvailable = true;

		mReleaseTargetObject = () => ReleaseTargetInteractionOnDestroy(lTargetBaseInteraction);

		if (mMovableEntity.MoveCallback(lTarget.transform.position, lParams) == false)
			return false;

		// doesnt work
		StartCoroutine(Utils.LookAtSlerpY(gameObject, lTarget));
		return true;
	}

	public override void ResetWorldState()
	{
		if (mObjectHeld != null)
			OnUnhold();
		if (mObjectMounted != null)
			OnUnmount();
		if (mObjectPushed != null)
			OnRelease();
		ResetAnimator();
	}

	private void ResetAnimator()
	{
		if (mAnimator == null)
			return;

		mAnimator.SetFloat("Forward", 0F);
		mAnimator.ResetTrigger("PickUp");
		mAnimator.SetBool("WalkingWithBox", false);
		mAnimator.SetBool("IdleWithBox", false);
		mAnimator.SetBool("Pushing", false);
	}

	private void OnStartMovement()
	{
		if (mObjectHeld == null)
			mAnimator.SetFloat("Forward", 0.8F);
		else
			mAnimator.SetBool("WalkingWithBox", true);
	}

	private void OnEndMovement()
	{
		if (mObjectHeld == null)
			mAnimator.SetFloat("Forward", 0F);
		else
			mAnimator.SetBool("WalkingWithBox", false);
	}

	private void CheckAndAddInteractionsSaved()
	{
		List<HumanInteraction> lMovableAnimationList = GameManager.Instance.TimeLineSerialized.HumanInteractionList;

		foreach (HumanInteraction lInter in lMovableAnimationList) {

			AnimationParameters lAnimationParameters;
			List<InteractionStep> lInteractionSteps;

			switch (lInter.InteractionType) {
			case HumanInteractionType.MOUNT:

				AEntity lEntity = AEntity.FindGUID(lInter.TargetGUID);
				if (lEntity == null) {
					Debug.LogError("[HUMAN INTERACTABLE] TargetGUID not found!");
					continue;
				}

				lAnimationParameters = new AnimationParameters() {
					TargetType = AnimationParameters.AnimationTargetType.ENTITY,
					AnimationTarget = lEntity.gameObject,
					Speed = lInter.Speed,
					Acceleration = lInter.Acceleration
				};

				lInteractionSteps = new List<InteractionStep>();

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = MoveToTargetCallback
				});

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = MountObjectCallback
				});

				lInter.TimeLineId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, lInter.Time);

				break;

			case HumanInteractionType.UNMOUNT:

				lAnimationParameters = new AnimationParameters() {
					TargetType = AnimationParameters.AnimationTargetType.ENTITY,
					Speed = mMovableEntity.ComputeSpeed(),
					Acceleration = mMovableEntity.ComputeAcceleration(),
				};

				lInteractionSteps = new List<InteractionStep>();

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = UnmountObjectCallback
				});

				lInter.TimeLineId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, lInter.Time);

				break;

			case HumanInteractionType.TAKE:

				lEntity = AEntity.FindGUID(lInter.TargetGUID);
				if (lEntity == null) {
					Debug.LogError("[HUMAN INTERACTABLE] TargetGUID not found!");
					continue;
				}

				lAnimationParameters = new AnimationParameters() {
					TargetType = AnimationParameters.AnimationTargetType.ENTITY,
					AnimationTarget = lEntity.gameObject,
					Speed = lInter.Speed,
					Acceleration = lInter.Acceleration
				};

				lInteractionSteps = new List<InteractionStep>();

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = MoveToTargetCallback
				});

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = TakeObjectAnimationTakeCallback
				});

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = TakeObjectWaitAnimationEndCallback
				});

				lInter.TimeLineId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, lInter.Time);

				break;

			case HumanInteractionType.TAKEOFF:

				lAnimationParameters = new AnimationParameters() {
					TargetType = AnimationParameters.AnimationTargetType.ENTITY,
				};

				lInteractionSteps = new List<InteractionStep>();

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = TakeOffObjectCallback
				});

				lInter.TimeLineId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, lInter.Time);

				break;

			case HumanInteractionType.PUSH:

				lEntity = AEntity.FindGUID(lInter.TargetGUID);
				if (lEntity == null) {
					Debug.LogError("[HUMAN INTERACTABLE] TargetGUID not found!");
					continue;
				}

				lAnimationParameters = new AnimationParameters() {
					TargetType = AnimationParameters.AnimationTargetType.ENTITY,
					AnimationTarget = lEntity.gameObject,
					Speed = lInter.Speed,
					Acceleration = lInter.Acceleration
				};

				lInteractionSteps = new List<InteractionStep>();

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = MoveToTargetCallback
				});

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = PushObjectCallback
				});

				lInter.TimeLineId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, lInter.Time);
				break;

			case HumanInteractionType.STOP_PUSH:

				lAnimationParameters = new AnimationParameters() {
					TargetType = AnimationParameters.AnimationTargetType.ENTITY,
					Speed = mMovableEntity.ComputeSpeed(),
					Acceleration = mMovableEntity.ComputeAcceleration(),
				};

				lInteractionSteps = new List<InteractionStep>();

				lInteractionSteps.Add(new InteractionStep {
					tag = lAnimationParameters,
					action = ReleaseObjectCallback
				});

				lInter.TimeLineId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, lInter.Time);

				break;
			}
		}
		GameManager.Instance.TimeLineSerialized.Serialize();
	}

}
