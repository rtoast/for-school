using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseHandler : MonoBehaviour,
	IPointerClickHandler,
	IPointerEnterHandler,
	IPointerDownHandler,
	IPointerExitHandler,
	IPointerUpHandler
{

	public Action OnMouseEnterAction { get; set; }

	public Action OnMouseExitAction { get; set; }

	public Action OnMouseUpAction { get; set; }

	public Action OnMouseDownAction { get; set; }

	public Action OnMouseDragAction { get; set; }

	public Action OnMouseOverAction { get; set; }


	public Action<PointerEventData> OnPointerClickAction { get; set; }

	public Action<PointerEventData> OnPointerEnterAction { get; set; }

	public Action<PointerEventData> OnPointerDownAction { get; set; }

	public Action<PointerEventData> OnPointerUpAction { get; set; }

	public Action<PointerEventData> OnPointerOverAction { get; set; }

	public Action<PointerEventData> OnPointerExitAction { get; set; }

	private void OnMouseEnter()
	{
		if (OnMouseEnterAction != null)
			OnMouseEnterAction();
	}

	private void OnMouseExit()
	{
		if (OnMouseExitAction != null)
			OnMouseExitAction();
	}

	private void OnMouseUp()
	{
		if (OnMouseUpAction != null)
			OnMouseUpAction();
	}

	private void OnMouseDown()
	{
		if (OnMouseDownAction != null)
			OnMouseDownAction();
	}

	private void OnMouseDrag()
	{
		if (OnMouseDragAction != null)
			OnMouseDragAction();
	}

	private void OnMouseOver()
	{
		if (OnMouseOverAction != null)
			OnMouseOverAction();
	}

	public void OnPointerClick(PointerEventData iEventData)
	{
		if (OnPointerClickAction != null)
			OnPointerClickAction(iEventData);
	}

	public void OnPointerEnter(PointerEventData iEventData)
	{
		if (OnPointerEnterAction != null)
			OnPointerEnterAction(iEventData);
	}

	public void OnPointerDown(PointerEventData iEventData)
	{
		if (OnPointerDownAction != null)
			OnPointerDownAction(iEventData);
	}

	public void OnPointerExit(PointerEventData iEventData)
	{
		if (OnPointerExitAction != null)
			OnPointerExitAction(iEventData);
	}

	public void OnPointerUp(PointerEventData iEventData)
	{
		if (OnPointerUpAction != null)
			OnPointerUpAction(iEventData);
	}


}
