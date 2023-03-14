using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIBubbleInfoButton
{
	/// <summary>
	/// Label of the UI Button
	/// </summary>
	public string Text { get; set; }

	/// <summary>
	/// Tag of the Button, useful to destroy a specific Button.
	/// </summary>
	public string Tag { get; set; }

	/// <summary>
	/// Callback OnClick of this Button.
	/// </summary>
	public Action<AEntity> ClickAction { get; set; }
}
