using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[CreateAssetMenu(menuName = "GameSettings/Manager", fileName = "GameSettingsManager")]
public class ScriptableSettingsManager : ScriptableSingleton<ScriptableSettingsManager>
{
    public override ScriptableSettingsManager Myself => this;

    private readonly List<ScriptableSettings> _scriptableSettings = new List<ScriptableSettings>();

    public List<ScriptableSettings> ScriptableSettings
    {
        get
        {
            if (_scriptableSettings == null)
                InitializeAllSettings();
            return _scriptableSettings;
        }
    }

    private Dictionary<string, ScriptableSettings> _allSettings;

    public Dictionary<string, ScriptableSettings> AllSettings
    {
        get
        {
            if (_allSettings == null)
                InitializeAllSettings();
            return _allSettings;
        }
    }

    private void InitializeAllSettings()
    {
        _allSettings = new Dictionary<string, ScriptableSettings>();
        foreach (ScriptableSettings item in _scriptableSettings)
            _allSettings.Add(GetKey(item.GetType()), item);
    }

    private static string GetKey(Type type) => type.FullName?.Replace("+Settings", string.Empty);

    public const string AssetsPath = "Assets/GameSettings/Resources";

    public static T Get<T>() where T : ScriptableSettings
    {
        return Instance.AllSettings[GetKey(typeof(T))] as T;
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    public static void Update()
    {
        Instance._Update();
    }

    public void _Update()
    {
        _scriptableSettings.Clear();

        IEnumerable<Type> types = GetAllSubclassTypes<ScriptableSettings>();

        if (!AssetDatabase.IsValidFolder("Assets/GameSettings"))
            AssetDatabase.CreateFolder("Assets", "GameSettings");

        if (!AssetDatabase.IsValidFolder("Assets/GameSettings/Resources"))
            AssetDatabase.CreateFolder("Assets/GameSettings", "Resources");

        foreach (Type item in types)
        {
            string currentPath = $"{AssetsPath}/{GetKey(item)}.asset";
            string localPath = $"{GetKey(item)}";
            UnityEngine.Object uObject = Resources.Load(localPath, item);
            if (uObject == null)
            {
                Debug.Log($"Created: {currentPath}");
                uObject = CreateInstance(item);
                AssetDatabase.CreateAsset(uObject, $"{currentPath}");
                AssetDatabase.SaveAssets();
            }

            _scriptableSettings.Add(uObject as ScriptableSettings);
        }
    }

    private static IEnumerable<Type> GetAllSubclassTypes<T>()
    {
        return from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            where (type.IsSubclassOf(typeof(T)) && !type.IsAbstract)
            select type;
    }
#endif
}