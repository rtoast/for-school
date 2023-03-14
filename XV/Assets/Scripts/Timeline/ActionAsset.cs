using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

using AnimAction = System.Predicate<object>;

public class ActionAsset : PlayableAsset
{
	public List<AnimAction> Actions { get; set; }
	public List<object> Parameters { get; set; }
	public ActionTrack Track { get; set; }
	public string Description { get; set; }

	public void Awake()
	{
		Actions = new List<AnimAction>();
		Parameters = new List<object>();
	}

	public override Playable CreatePlayable(PlayableGraph iGraph, GameObject iOwner)
	{
		ScriptPlayable<ActionBehaviour> lPlayable = ScriptPlayable<ActionBehaviour>.Create(iGraph);
		ActionBehaviour lBehaviour = lPlayable.GetBehaviour();
		lBehaviour.Actions = Actions;
		lBehaviour.Parameters = Parameters;
		lBehaviour.Track = Track;
		lBehaviour.Description = Description;
		return lPlayable;
	}

}
