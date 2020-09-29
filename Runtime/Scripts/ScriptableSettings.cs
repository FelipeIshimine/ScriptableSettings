using System;
using UnityEngine;

public abstract class ScriptableSettings : ScriptableObject
{
        public static T GetDefault<T>() where  T : ScriptableSettings => ScriptableSettingsManager.Get<T>();
        [SerializeField,HideInInspector] private string tabName;
        public string TabName
        {
                get
                {
                        if (string.IsNullOrEmpty(tabName)) tabName = DefaultTabName;
                        return tabName;
                }
                set => tabName = value;
        }        

        public string DefaultTabName => GetType().Name.Replace("Settings", string.Empty);
}