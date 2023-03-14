using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum ObjectDataSceneType
{
	BUILT_IN,

	EXTERN,
}

[Serializable]
public class AObjectDataScene
{
	public string Name { get; set; }

	// This variable shouldn't be edited manually!
	public string GUID { get; set; }

	public ObjectDataSceneType Type { get; set; }

	public string PrefabName { get; set; }

	public Vector3 Position { get; set; }

	public Vector3 Rotation { get; set; }

	public float Speed { get; set; }

	public bool IsColored { get; set; }

	public List<Color> OriginalColorsMaterial { get; set; }

	public Color Color { get; set; }

	public AObjectDataScene()
	{
		OriginalColorsMaterial = new List<Color>();

		if (GUID == null)
			GUID = Guid.NewGuid().ToString();
	}
}
