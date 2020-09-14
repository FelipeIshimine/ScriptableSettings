using UnityEngine;

public abstract class ScriptableSettings : ScriptableObject
{
        public static T GetDefault<T>() where  T : ScriptableSettings => ScriptableSettingsManager.Get<T>();
}