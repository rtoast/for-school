using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Use that for Item that don't have Interaction but need to be interactable, example: A box
/// </summary>
public sealed class GenericInteractable: AInteraction {


	protected override void PostPoppingEntity()
    {
		
    }

	public override void ResetWorldState() {}

}
