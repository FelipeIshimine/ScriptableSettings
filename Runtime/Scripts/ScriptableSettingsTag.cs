using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableSettingsTag : ScriptableObject
{
   [SerializeField]private List<ScriptableSettings> elements = new List<ScriptableSettings>();
   public List<ScriptableSettings> Elements => elements;
}