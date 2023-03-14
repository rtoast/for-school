using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractionStep
{
	public object tag;
	public Predicate<object> action;
}