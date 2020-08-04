﻿using System;
using UnityEngine;


[CreateAssetMenu]
public class IntVariable : ScriptableObject, ISerializationCallbackReceiver
{
	public int initialValue;
	[NonSerialized]
	public int runtimeValue;

	public void OnAfterDeserialize()
	{
		runtimeValue = initialValue;
	}

	public void OnBeforeSerialize() { }
}