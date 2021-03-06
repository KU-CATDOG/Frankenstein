﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Type
{
    All,
    BodyPart,
    Consumable,
    Tool,
    Ingredient
}

public enum Race
{
    All,
    Human,
    Goblin,
    Elf,
    Oak,
    Machine,
    Null
}

[CreateAssetMenu]
public class Item : ScriptableObject
{ 
    [Header("")]
    public int id;
    public string itemName;
    public string description;
    public Sprite itemImage;
    [Header("")]
    public Type type;
    public Race race;
    public int energyPotential;
}
