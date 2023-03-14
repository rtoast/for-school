using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationParameters
{
	public enum AnimationTargetType { ENTITY, POSITION };

	/// <summary>
	/// The target type use for this animation
	/// </summary>
	public AnimationTargetType TargetType;

	/// <summary>
	/// Animation's speed
	/// </summary>
	public float Speed = 1F;

	/// <summary>
	/// Animation's speed acceleration
	/// </summary>
	public float Acceleration = 1F;

	/// <summary>
	/// Object use as target for an Animation.
	/// Use AnimationTargetType to cast it later
	/// </summary>
	public object AnimationTarget;

	public Func<bool> Action;
}

/// <summary>
/// Base class for Item interaction script.
/// </summary>
public abstract class AInteraction : MonoBehaviour
{
	/// <summary>
	/// This class define an Interaction
	/// </summary>
	protected class ItemInteraction
	{
		/// <summary>
		/// Name of this Interaction.
		/// </summary>
		public string Name;

		/// <summary>
		/// Info to understand how to use this Interaction.
		/// </summary>
		public string Help;

		/// <summary>
		/// Subscriptions of this interaction, defined what type of Entity this interaction can interact with.
		/// </summary>
		public EntityParameters.EntityType[] InteractWith;

		/// <summary>
		/// UIBubbleInfoButton of this Animation, it will be displayed only when one of Subscription is present in the scene.
		/// </summary>
		public UIBubbleInfoButton Button;

		/// <summary>
		/// Will be called when the UI Button is display.
		/// </summary>
		public Action OnDisplay;

		/// <summary>
		/// Will be called when the UI Button is hide.
		/// </summary>
		public Action OnHide;

		public AInteraction AInteraction;

		private bool mIsDisplayed;

		private bool mEnabled;

		/// <summary>
		/// Return true if this Interaction UI is displayed.
		/// </summary>
		public bool IsDisplayed { get { return mIsDisplayed; } }

		/// <summary>
		/// Allow to force disable an interaction. This will hide a button even if it can interacts with something.
		/// </summary>
		public bool Enabled
		{
			get { return mEnabled; }

			set
			{
				mEnabled = value;
				if (value)
					AInteraction.UpdateAvailableInteraction();
				else
					HideUI();
			}
		}

		private AEntity mBindedObjectEntity;

		internal AEntity BindedObjectEntity { set { mBindedObjectEntity = value; } }

		/// <summary>
		/// Parameters for an Animation, with Animation's code, UI Button and Subscriptions.
		/// </summary>
		public ItemInteraction()
		{
			mEnabled = true;
			mIsDisplayed = false;
			mBindedObjectEntity = null;
			OnDisplay = null;
			OnHide = null;
			Help = null;
		}

		internal void DisplayUI()
		{
			// Check the Button is Hided
			if (!mIsDisplayed && mEnabled) {
				mIsDisplayed = true;
				if (mBindedObjectEntity != null && !mBindedObjectEntity.ContainsBubbleInfoButton(Button)) {
					mBindedObjectEntity.CreateBubbleInfoButton(Button);
					if (OnDisplay != null)
						OnDisplay();
				}
			}
		}

		internal void HideUI()
		{
			// Check the Button is Displayed
			if (mIsDisplayed) {
				mIsDisplayed = false;
				if (mBindedObjectEntity != null && mBindedObjectEntity.ContainsBubbleInfoButton(Button)) {
					mBindedObjectEntity.DestroyBubbleInfoButton(Button);
					if (OnHide != null)
						OnHide();
				}
			}
		}
	}

	/*
    **  Each index of this array correspond to an EntityType (HUMAN, TROLLEY, MEDIUM_ITEM, ...)
    **  On build object, the builded object increment it's corresponding index, according to it's EntityType.
    **  On destroy, the destroyed object decrement it's  corresponding index, according to it's EntityType.
    **  So we now exactly what type are currently present in the scene, with this information:
    **  We can display or not, in BuildObject, an Animation button, depending of what type are present in the scene.
    */
	private static int[] sEntityTypeCounter;

	public static int[] EntityTypeCounter
	{
		get
		{
			if (sEntityTypeCounter != null)
				return sEntityTypeCounter;
			else
				return new int[0];
		}
	}

	private static List<ItemInteraction>[] sTypeSubscribers;

	private EntityParameters mParameters;

	private List<ItemInteraction> mItemInteractions;

	private int mReservationHashCode;

	protected AEntity mEntity;

	protected virtual void Start()
	{
		mReservationHashCode = 0;

		mEntity = GetComponent<AEntity>();
		mParameters = GetComponent<EntityParameters>();

		if (mEntity == null || mParameters == null) {
			//Debug.LogError("[AInteraction] Start abort");
			enabled = false;
			return;
		}

		// Add all this code to the PostPopping callback of ObjectEntity
		mEntity.PostPoppingAction.Add(() => {
			// Increase the entity counter with the type of this new ObjectEntity
			AddType();

			// Child post popping
			PostPoppingEntity();

			UpdateAvailableInteraction();
		});

		mItemInteractions = new List<ItemInteraction>();

		int lLenght = (int)EntityParameters.EntityType.COUNT;

		if (sTypeSubscribers == null) {
			sTypeSubscribers = new List<ItemInteraction>[lLenght];
			for (int i = 0; i < lLenght; i++)
				sTypeSubscribers[i] = new List<ItemInteraction>();
		}

		if (sEntityTypeCounter == null)
			sEntityTypeCounter = new int[(int)EntityParameters.EntityType.COUNT];

	}

	public abstract void ResetWorldState();

	protected abstract void PostPoppingEntity();

	/// <summary>
	/// Ask to use this AInteraction, if it is available return true
	/// and mark this object unavailable to interact with.
	/// </summary>
	/// <returns>True if the reservation success, false otherwise. (Already in used)</returns>
	public bool ReserveForInteraction(int iHashCode)
	{
		// New reservation
		if (mReservationHashCode == 0) {
			mReservationHashCode = iHashCode;
			return true;
		}

		// Already reserve by the caller
		if (mReservationHashCode == iHashCode)
			return true;

		XV_UI.Instance.Notify(2F, "The target is already in used by another object.");
		return false;
	}

	/// <summary>
	/// Release this AInteraction, the object will be available to interact with.
	/// </summary>
	public void ReleaseForInteraction()
	{
		mReservationHashCode = 0;
	}

	/// <summary>
	/// Display UI of available interaction according to Entities in the scene.
	/// </summary>
	public void UpdateAvailableInteraction()
	{
		foreach (ItemInteraction lAnimationParameter in mItemInteractions) {
			foreach (EntityParameters.EntityType lType in lAnimationParameter.InteractWith) {
				if (sEntityTypeCounter[(int)lType] > 0) {
					lAnimationParameter.DisplayUI();
				}
			}
		}
	}

	// Increase the entity counter with the type of this new ObjectEntity
	private void AddType()
	{
		if (mParameters != null) {
			if (Enum.IsDefined(typeof(EntityParameters.EntityType), mParameters.Type)) {

				int lIndex = (int)mParameters.Type;

				bool lValueWasZero = sEntityTypeCounter[lIndex] == 0;
				sEntityTypeCounter[lIndex]++;

				if (lValueWasZero && sEntityTypeCounter[lIndex] == 1) {
					FireOnTypeAppear(mParameters.Type);
				}
			}
		}
	}

	// Decrease the entity counter with the type of this new ObjectEntity
	private void RemoveType()
	{
		if (mParameters != null) {
			if (Enum.IsDefined(typeof(EntityParameters.EntityType), mParameters.Type)) {

				int lType = (int)mParameters.Type;
				sEntityTypeCounter[lType]--;
				if (sEntityTypeCounter[lType] < 0)
					sEntityTypeCounter[lType] = 0;

				if (sEntityTypeCounter[lType] == 0)
					FireOnTypeDisappear(mParameters.Type);
			}
		}
	}

	private static void FireOnTypeAppear(EntityParameters.EntityType iType)
	{
		foreach (ItemInteraction lAnimationParameters in sTypeSubscribers[(int)iType])
			lAnimationParameters.DisplayUI();
	}

	private static void FireOnTypeDisappear(EntityParameters.EntityType iType)
	{
		foreach (ItemInteraction lAnimationParameters in sTypeSubscribers[(int)iType])
			lAnimationParameters.HideUI();
	}

	protected ItemInteraction GetItemInteraction(string iName)
	{
		foreach (ItemInteraction lInteraction in mItemInteractions) {
			if (lInteraction.Name == iName)
				return lInteraction;
		}
		return null;
	}

	/// <summary>
	/// Return true if iInteractionName can interact with iType Entity.
	/// </summary>
	/// <returns><c>true</c>, if interact with was caned, <c>false</c> otherwise.</returns>
	/// <param name="iInteractionName">I interaction name.</param>
	/// <param name="iType">I type.</param>
	protected bool IsInteractionCanInteractType(string iInteractionName, EntityParameters.EntityType iType)
	{
		foreach (ItemInteraction lInteraction in mItemInteractions) {

			// Find the requested Interaction
			if (lInteraction.Name == iInteractionName) {

				// Check if the request EntityType can interact with this interaction
				foreach (EntityParameters.EntityType lType in lInteraction.InteractWith) {
					if (iType == lType)
						return true;
				}
			}
		}
		return false;
	}

	protected virtual void OnDestroy()
	{
		RemoveType();
	}

	/// <summary>
	/// Add and handle an animation, it's button will be displayed only when available,
	/// according to Subscriptions field of AnimationParameters.
	/// </summary>
	/// <param name="iInteraction"></param>
	protected ItemInteraction CreateInteraction(ItemInteraction iInteraction)
	{
		if (iInteraction != null) {

			// Check all field are correctly set
			if (string.IsNullOrEmpty(iInteraction.Name) || iInteraction.InteractWith == null
				|| iInteraction.InteractWith.Length == 0) {
				Debug.LogError("[INTERACTION] - AnimationParameters not correctly set");
				return null;
			}

			// Update Callback for EntityType Counter
			foreach (EntityParameters.EntityType lType in iInteraction.InteractWith) {

				int lIntType = (int)lType;

				if (lIntType < 0 || lIntType > sTypeSubscribers.Length) {
					Debug.LogError("[INTERACTION] - Subscribers list out of range index access.");
				} else {
					iInteraction.BindedObjectEntity = mEntity;
					sTypeSubscribers[lIntType].Add(iInteraction);
				}
			}
			mItemInteractions.Add(iInteraction);
		}
		return iInteraction;
	}

	protected IEnumerator InteractionWaitForTarget(string iInteractionName, Action<EntityParameters> iOnFoundTarget)
	{
		AEntity.HideNoInteractable(GetItemInteraction(iInteractionName).InteractWith, mEntity);
		yield return new WaitWhile(() => {

			if (Input.GetKeyDown(KeyCode.Escape)) {
				Debug.LogWarning("[TARGET SELECTOR] Exit target selector !");
				return false;
			}

			if (Input.GetMouseButtonDown(0)) {

				RaycastHit lHit;
				Ray lRay = Camera.main.ScreenPointToRay(Input.mousePosition);
				if (Physics.Raycast(lRay, out lHit, 100000, LayerMask.GetMask("dropable"))) {

					Debug.DrawRay(lRay.origin, lRay.direction * lHit.distance, Color.red, 1);

					// If we don't found EntityParameters, stop with an error.
					EntityParameters lEntityParam;
					if ((lEntityParam = lHit.collider.gameObject.GetComponentInParent<EntityParameters>()) == null) {
						Debug.LogWarning("[TARGET SELECTOR] Exit target selector !");
						return false;
					}

					// If Subscribers contain the clicked Entity type use it as target and add animation to timeline
					if (IsInteractionCanInteractType(iInteractionName, lEntityParam.Type)) {
						iOnFoundTarget(lEntityParam);
						return false;
					}
					Debug.LogWarning("[TARGET SELECTOR] The object you click on is not interactable with this object !");
				}

				return false;
			}
			return true;
		});
		AEntity.DisableHideNoInteractable();
	}

}


/*
**  GLOSSAIRE:
**
**  deplacable: Le mot deplacable est utilise pour designer une interaction / animation.
**              (Ex: 'poser le carton sur la table').
**              Il ne designe donc pas la translation d'objet (edition) mais son deplacement via une interaction / animations.
**
**  animation: Definit une action simple, (Ex: Chariot -> 'Monter les fourche du chariot')
**             Ne pas confondre avec une animation de l'animator, car nous somme dans le contexte de la TIMELINE.
**             (Ex d'animation qui ne sera pas implementer via l'Animator: Humain -> 'aller ici ...')
**
**  interaction: Definit un comportement et implique obligatoirement plusieur objets.
**               Une interaction peut comporter plusieurs animation d'objets.
**               (Ex: Chariot -> 'Prendre la palette' ou Humain -> 'aller a la table d'emballage')
**
**
**  ------ CLASSIFICATION DES ITEMS  -------
**
**  SMALL_ITEM:
**  Objets ramassable, qui se place dans un inventaire. Peut debloquer des interactions pour un HUMAN.
**  (Ex: un document, etiquette)
**
**  MEDIUM_ITEM:
**  Objets deplacable par un HUMAN ou par TROLLEY & portable par un HUMAN.
**  (Ex: un carton)
**
**  HEAVY_ITEM:
**  Objets deplacable uniquement via TROLLEY ou VEHICLE & non portable par un HUMAN.
**  (Ex: palette)
**
**  FIX_ITEM:
**  Objets non deplacables mais qui comporte quand meme des animations / interactions.
**  (Ex: Table d'emballage -> 'Emballer/Etiquetter un carton')
**
**  TROLLEY:
**  Objets assimilable a un 'chariot' (trolley), utilisables et qui peut donner ou non la possibilite
**  de deplacer les HEAVY_ITEM / MEDIUM_ITEM. (Ex: un chariot donne acces a -> 'deplacer palette')
**  
**  VEHICLE:
**  Objets utilisables et qui donne la possibilite de deplacer les HEAVY_ITEM.
**  (Ex: un chariot donne acces a -> 'deplacer palette')
**
**  HUMAN:
**  Humain pouvant ramasser, porter des objets, pousser des TROLLEY ou conduire des VEHICLE.
**
**  ------------- UI -----------
**
**  Les objets deplacable Deplacement / Rotations auront un bouton pour ces features dans leurs UIBubleInfo.
**  Tout les objets comportants animations ou interactions en plus, auront un bouton Animer dans l'UIBubleInfo.
*/
