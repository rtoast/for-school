using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MovableAnimation
{
	public string EntityGUID { get; set; }

	public bool IsMoveAnim { get; set; }

	public bool IsRotateAnim { get; set; }

	public Vector3 TargetPosition { get; set; }

	public Vector3 TargetRotation { get; set; }

	public float Speed { get; set; }

	public float Acceleration { get; set; }

	public double Time { get; set; }

	public int TimeLineId { get; set; }
}