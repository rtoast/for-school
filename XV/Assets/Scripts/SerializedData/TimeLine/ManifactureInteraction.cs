using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ManifactureInteraction
{
	public string EntityGUID { get; set; }

	public bool IsTakeObject { get; set; }

	public bool IsTakeOffObject { get; set; }

	public string TargetGUID { get; set; }

	public float Speed { get; set; }

	public float Acceleration { get; set; }

	public double Time { get; set; }

	public int TimeLineId { get; set; }
}
