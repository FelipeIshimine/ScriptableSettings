using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Object = System.Object;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

[CreateAssetMenu(menuName = "GameSettings/Manager", fileName = "GameSettingsManager")]
public class ScriptableSettingsManager : ScriptableSingleton<ScriptableSettingsManager>
{
#if UNITY_EDITOR
    public static class UpdateGit
    {
        [MenuItem("Window/Ishimine/UpdateScriptableSettings")]
        public static void SelectMe()
        {
            AddRequest request = Client.Add("https://github.com/FelipeIshimine/ScriptableSettings.git");
        }
    }
#endif

    public override ScriptableSettingsManager Myself => this;

    [SerializeField] private  List<ScriptableSettings> scriptableSettings = new List<ScriptableSettings>();

    public List<ScriptableSettings> ScriptableSettings
    {
        get
        {
            if (scriptableSettings == null)
                InitializeAllSettings();
            return scriptableSettings;
        }
    }

    public Dictionary<string, ScriptableSettings> _allSettings;
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
        foreach (ScriptableSettings item in scriptableSettings)
            _allSettings.Add(GetKey(item.GetType()), item);
    }

    private static string GetKey(Type type) => type.FullName;
    public const string AssetsPath = "Assets/ScriptableSettings/Resources";
    public static T Get<T>() where T : ScriptableSettings
    {
        string key = GetKey(typeof(T));
        Debug.Log("Searching for key: " + key);
        
        if (!Instance.AllSettings.ContainsKey(key))
            Instance.InitializeAllSettings();
        
        return Instance.AllSettings[key] as T;
    }

#if UNITY_EDITOR
    public static void Update()
    {
        Instance._Update();
    }

    public void _Update()
    {
        scriptableSettings.Clear();

        IEnumerable<Type> types = GetAllSubclassTypes<ScriptableSettings>();

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableSettings"))
            AssetDatabase.CreateFolder("Assets", "ScriptableSettings");

        if (!AssetDatabase.IsValidFolder("Assets/ScriptableSettings/Resources"))
            AssetDatabase.CreateFolder("Assets/ScriptableSettings", "Resources");

        foreach (Type item in types)
        {
            string key = GetKey(item);
            string currentPath = $"{AssetsPath}/{key}.asset";
            string localPath = $"{key}";
            UnityEngine.Object uObject = Resources.Load(localPath, item);
            if (uObject == null)
            {
                Debug.Log($"Created: {currentPath}");
                uObject = CreateInstance(item);
                AssetDatabase.CreateAsset(uObject, $"{currentPath}");
            }
            scriptableSettings.Add(uObject as ScriptableSettings);
        }
        AssetDatabase.SaveAssets();
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