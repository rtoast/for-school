using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(MovableEntity))]
public class ManifactureInteractable : AInteraction
{

	private MovableEntity mMovableEntity;

	private AEntity mObjectHeld;

	private AEntity mHumanEntity;

	private ItemInteraction mTakeObjectInteraction;

	private UIBubbleInfoButton mTakeOffBubbleButton;

	private Action mReleaseTargetObject;

	private bool mTargetIsAvailable;

	protected override void Start()
	{
		base.Start();

		mMovableEntity = GetComponent<MovableEntity>();

		mReleaseTargetObject = null;

		mTargetIsAvailable = true;
	}


	protected override void PostPoppingEntity()
	{
		if (mEntity.EntityParameters.Type == EntityParameters.EntityType.CUPBOARD)
			return;

		mTakeObjectInteraction = CreateInteraction(new ItemInteraction() {
			Name = "Take",
			Help = "Take an object",
			InteractWith = new EntityParameters.EntityType[] { EntityParameters.EntityType.MEDIUM_ITEM, EntityParameters.EntityType.HEAVY_ITEM },
			AInteraction = this,
			Button = new UIBubbleInfoButton() {
				Text = "Take",
				Tag = name + "_TAKE_OBJECT",
				ClickAction = OnClickTakeObject
			}
		});

		mTakeOffBubbleButton = new UIBubbleInfoButton() {
			Text = "Take off",
			Tag = "TakeOffButton",
			ClickAction = OnClickTakeOffObject
		};

		CheckAndAddInteractionsSaved();

		TimelineEvent.UIResizeClipEvent += OnDragClipEvent;
		TimelineEvent.UIDeleteClipEvent += OnDeleteClipEvent;
		TimelineEvent.StopEvent += OnStopEvent;
	}

	private void OnStopEvent(TimelineEventData iEvent)
	{
		if (mReleaseTargetObject != null)
			mReleaseTargetObject();
	}

	private void OnDragClipEvent(TimelineEventData iEvent)
	{
		if (iEvent.Type == TimelineData.EventType.INTERACTION) {
			TimeLineSerialized lTimeLineSerialized = GameManager.Instance.TimeLineSerialized;

			ManifactureInteraction lInter = lTimeLineSerialized.FindManifactureInteraction(iEvent.ClipID);
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

			ManifactureInteraction lInter = lTimeLineSerialized.FindManifactureInteraction(iEvent.ClipID);
			if (lInter != null) {
				lTimeLineSerialized.ManifactureInteractionList.Remove(lInter);
				lTimeLineSerialized.Serialize();
			}
		}
	}

	private void ReleaseTargetInteractionOnDetroy(AInteraction iTarget)
	{
		if (iTarget != null)
			iTarget.ReleaseForInteraction();
		mReleaseTargetObject = null;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		TimelineEvent.AddClipEvent -= OnDragClipEvent;
		TimelineEvent.DeleteClipEvent -= OnDeleteClipEvent;
		TimelineEvent.StopEvent -= OnStopEvent;

		if (mReleaseTargetObject != null)
			mReleaseTargetObject();
	}

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
				action = TakeObjectMoveToTargetCallback
			});

			lInteractionSteps.Add(new InteractionStep {
				tag = lAnimationParameters,
				action = TakeObjectPickCallback
			});

			int lId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, TimelineManager.Instance.Time);

			GameManager.Instance.TimeLineSerialized.ManifactureInteractionList.Add(new ManifactureInteraction {
				EntityGUID = mEntity.AODS.GUID,
				IsTakeObject = true,
				TargetGUID = iTargetEntityParameters.GetComponent<AEntity>().AODS.GUID, //j'aime le danger
				Speed = lAnimationParameters.Speed,
				Acceleration = lAnimationParameters.Acceleration,
				Time = TimelineManager.Instance.Time,
				TimeLineId = lId
			});
			GameManager.Instance.CurrentDataScene.Serialize();

		}));
	}

	private bool TakeObjectMoveToTargetCallback(object iParams)
	{
		if (gameObject == null || iParams == null)
			return true;

		AnimationParameters lParams = (AnimationParameters)iParams;
		GameObject lTarget = (GameObject)lParams.AnimationTarget;

		if (lTarget == null || mObjectHeld != null) {
            XV_UI.Instance.Notify(2F, "Can't take the object.");
            return true;
        }

        AInteraction lTargetBaseInteraction;
		if ((lTargetBaseInteraction = lTarget.GetComponent<AInteraction>()) == null) {
			XV_UI.Instance.Notify(2F, "Can't check the object is available.");
			mTargetIsAvailable = false;
			return true;
		}

		if (!lTargetBaseInteraction.ReserveForInteraction(gameObject.GetHashCode())) {
			mTargetIsAvailable = false;
			return true;
		}
		mTargetIsAvailable = true;
		mReleaseTargetObject = () => ReleaseTargetInteractionOnDetroy(lTargetBaseInteraction);

		if (mMovableEntity.MoveCallback(lTarget.transform.position, lParams) == false)
			return false;

		// doesnt work
		StartCoroutine(Utils.LookAtSlerpY(gameObject, lTarget));
		return true;
	}

	private bool TakeObjectPickCallback(object iParams)
	{
		if (gameObject == null || iParams == null || !mTargetIsAvailable)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.STOP)
			return true;

		if (TimelineManager.sGlobalState == TimelineManager.State.PAUSE)
			return false;

		AnimationParameters lParams = (AnimationParameters)iParams;
		GameObject lTarget = (GameObject)lParams.AnimationTarget;

        if (lTarget == null || mObjectHeld != null)
            return true;

		mObjectHeld = lTarget.GetComponent<AEntity>();
		mObjectHeld.Selected = false;
		mObjectHeld.NavMeshObjstacleEnabled = false;
		mObjectHeld.LockWorldEditorDeplacement = true;
		mObjectHeld.StashUIBubbleButtons();

		lTarget.transform.parent = gameObject.transform;
		lTarget.transform.localPosition = mEntity.EntityParameters.VehiculeHoldPosition;
		//lTarget.transform.localRotation = Quaternion.Euler(0, 0, 0);
		OnHold();

		return true;
	}

	private void OnClickTakeOffObject(AEntity iEntity)
	{
		AnimationParameters lAnimationParameters = new AnimationParameters() {
			TargetType = AnimationParameters.AnimationTargetType.ENTITY,
			AnimationTarget = mObjectHeld
		};

		List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

		lInteractionSteps.Add(new InteractionStep {
			tag = lAnimationParameters,
			action = TakeOffObjectCallback
		});

		int lId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, TimelineManager.Instance.Time);

		GameManager.Instance.TimeLineSerialized.ManifactureInteractionList.Add(new ManifactureInteraction {
			EntityGUID = mEntity.AODS.GUID,
			IsTakeOffObject = true,
			Time = TimelineManager.Instance.Time,
			TimeLineId = lId
		});
		GameManager.Instance.CurrentDataScene.Serialize();
	}

	private bool TakeOffObjectCallback(object iParams)
	{
		if (iParams == null || gameObject == null)
			return true;

        if (mObjectHeld == null) {
            XV_UI.Instance.Notify(2F, "Can't drop the object.");
            return true;
        }

		AInteraction lTargetBaseInteraction;
		if ((lTargetBaseInteraction = mObjectHeld.GetComponent<AInteraction>()) == null) {
			XV_UI.Instance.Notify(2F, "Can't check the object is available.");
		}
		lTargetBaseInteraction.ReleaseForInteraction();

		mReleaseTargetObject = null;

		mObjectHeld.transform.localPosition = mEntity.EntityParameters.VehiculeDropPosition;
		mObjectHeld.transform.parent = null;

		OnUnhold();
		return true;
	}

	private void OnHold()
	{
		mTakeObjectInteraction.Enabled = false;
		if (!mEntity.ContainsBubbleInfoButton(mTakeOffBubbleButton.Tag))
			mEntity.CreateBubbleInfoButton(mTakeOffBubbleButton);
	}

	private void OnUnhold()
	{
		mEntity.DestroyBubbleInfoButton(mTakeOffBubbleButton);
		mTakeObjectInteraction.Enabled = true;
		if (mObjectHeld != null) {
			mObjectHeld.LockWorldEditorDeplacement = false;
			mObjectHeld.StashPopUIBubbleInfoButtons();
			mObjectHeld.NavMeshObjstacleEnabled = true;
			mObjectHeld = null;
		} else
			Debug.LogWarning("[HUMAN INTERACTABLE] Object Held shouldn't be null in OnUnhold");
	}

	#endregion TakeObject

	public void HoldHuman(HumanInteractable iHuman, HumanInteractionType iInteractionType, Action iOnStartMovement = null, Action iOnEndMovement = null)
	{
		if (iInteractionType == HumanInteractionType.MOUNT) {
			iHuman.transform.parent = transform;
			iHuman.transform.localPosition = mEntity.EntityParameters.VehiculeSitPosition;
			iHuman.transform.localRotation = Quaternion.Euler(0, 0, 0);

			// Eventually add callback
			if (iOnStartMovement != null)
				mMovableEntity.OnStartMovement.Add(iOnStartMovement);
			if (iOnEndMovement != null)
				mMovableEntity.OnStartMovement.Add(iOnEndMovement);
		} else if (iInteractionType == HumanInteractionType.PUSH) {
			iHuman.transform.parent = transform;
			iHuman.transform.localPosition = mEntity.EntityParameters.VehiculeSitPosition;
			iHuman.transform.localRotation = Quaternion.Euler(0, 0, 0);

			// Eventually add callback
			if (iOnStartMovement != null)
				mMovableEntity.OnStartMovement.Add(iOnStartMovement);
			if (iOnEndMovement != null)
				mMovableEntity.OnEndMovement.Add(iOnEndMovement);
		}
	}

	public void DropHuman(HumanInteractable iHuman, Action iOnStartMovement = null, Action iOnEndMovement = null)
	{
		iHuman.transform.localPosition = new Vector3(0, 0, -2);
		iHuman.transform.parent = null;

		// Remove given callback
		if (iOnStartMovement != null)
			mMovableEntity.OnStartMovement.Remove(iOnStartMovement);
		if (iOnEndMovement != null)
			mMovableEntity.OnEndMovement.Remove(iOnEndMovement);
	}

	public override void ResetWorldState()
	{
		if (mObjectHeld != null)
			OnUnhold();
	}

	private void CheckAndAddInteractionsSaved()
	{
		List<ManifactureInteraction> lManifactureInteractionList = GameManager.Instance.TimeLineSerialized.ManifactureInteractionList;

		string lMyGUID = mEntity.AODS.GUID;

		foreach (ManifactureInteraction lInter in lManifactureInteractionList) {

			if (lMyGUID == lInter.EntityGUID) {

				if (lInter.IsTakeObject) {

					AEntity lEntity = AEntity.FindGUID(lInter.TargetGUID);
					if (lEntity == null) {
						Debug.LogError("[HUMAN INTERACTABLE] TargetGUID not found!");
						continue;
					}

					AnimationParameters lAnimationParameters = new AnimationParameters() {
						TargetType = AnimationParameters.AnimationTargetType.ENTITY,
						AnimationTarget = lEntity.gameObject,
						Speed = lInter.Speed,
						Acceleration = lInter.Acceleration
					};

					List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

					lInteractionSteps.Add(new InteractionStep {
						tag = lAnimationParameters,
						action = TakeObjectMoveToTargetCallback
					});

					lInteractionSteps.Add(new InteractionStep {
						tag = lAnimationParameters,
						action = TakeObjectPickCallback
					});

					lInter.TimeLineId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, lInter.Time);

				} else if (lInter.IsTakeOffObject) {

					AnimationParameters lAnimationParameters = new AnimationParameters() {
						TargetType = AnimationParameters.AnimationTargetType.ENTITY,
						AnimationTarget = mObjectHeld
					};

					List<InteractionStep> lInteractionSteps = new List<InteractionStep>();

					lInteractionSteps.Add(new InteractionStep {
						tag = lAnimationParameters,
						action = TakeOffObjectCallback
					});

					lInter.TimeLineId = TimelineManager.Instance.AddInteraction(gameObject, lInteractionSteps, lInter.Time);
				}
			}
		}
		GameManager.Instance.TimeLineSerialized.Serialize();
	}
}
