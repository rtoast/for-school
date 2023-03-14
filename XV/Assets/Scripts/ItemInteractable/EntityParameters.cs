using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityParameters : MonoBehaviour
{

	public enum EntityType
	{
		SMALL_ITEM,
		MEDIUM_ITEM,
		HEAVY_ITEM,
		FIX_ITEM,
		TROLLEY,
		VEHICLE,
		CUPBOARD,
		HUMAN,
		COUNT,
	}

	[Header("Choose a type for this entity")]
	[SerializeField]
	private EntityType TypeOf;

	public EntityType Type
	{
		get { return TypeOf; }
	}

	[Header("Only for vehicle")]
	[SerializeField]
	private Vector3 SitPosition;

	public Vector3 VehiculeSitPosition
	{
		get { return SitPosition; }
	}

	[SerializeField]
	private Vector3 HoldPosition;

	public Vector3 VehiculeHoldPosition
	{
		get { return HoldPosition; }
	}

	[SerializeField]
	private Vector3 DropPosition;

	public Vector3 VehiculeDropPosition
	{
		get { return DropPosition; }
	}

	[Header("Active or not, movement & rotation animation")]
	[SerializeField]
	private bool CanMove;

	public bool Movable
	{
		get { return CanMove; }
	}

    public EntityParameters()
    {
        TypeOf = EntityType.FIX_ITEM;
        SitPosition = Vector3.zero;
        HoldPosition = Vector3.zero;
        DropPosition = Vector3.zero;
        CanMove = false;
    }

}
