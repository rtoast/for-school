using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class UITrackNameBox : MonoBehaviour, IPointerClickHandler
{
	private UITrack mTrack;

	private void Awake()
	{
		Transform lTrackTransform = transform.parent;
		if (lTrackTransform != null) {
			mTrack = lTrackTransform.GetComponent<UITrack>();
		}
	}

	public void OnPointerClick(PointerEventData iData)
	{
		if (iData.button == PointerEventData.InputButton.Left) {
			if (mTrack != null) {
				GameObject lObject = TimelineManager.Instance.GetObjectFromID(mTrack.ID);
				CameraHandler lCam = Camera.main.GetComponent<CameraHandler>();
				if (lCam != null && lObject != null) {
					lCam.Focus(lObject.transform.position);
				}
			}
		}
	}
}
