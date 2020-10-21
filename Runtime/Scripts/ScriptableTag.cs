using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableTag : ScriptableObject
{
   [SerializeField]private List<ScriptableObject> elements = new List<ScriptableObject>();
   public List<ScriptableObject> Elements => elements;
}