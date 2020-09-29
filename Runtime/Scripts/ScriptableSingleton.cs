using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Singleton que sea auto instancia e inicializa dentro de la carpeta Resources
/// </summary>
/// <typeparam name="T">Referencia circular a la propia clase de la que se quiere hacer Singleton</typeparam>
public abstract class ScriptableSingleton<T> : BaseScriptableSingleton where T : ScriptableSingleton<T>
{
    public static List<ScriptableObject> scriptableSingletons = new List<ScriptableObject>();
    private static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                try
                {
                    instance = Resources.LoadAll<T>("")[0];
                }
                catch (System.Exception error)
                {
                    Debug.Log(error);
                }

#if UNITY_EDITOR
                if (instance == null)
                {
                    instance = CreateInstance<T>();
                    AssetDatabase.CreateAsset(instance, instance.FilePath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
#endif
            }
            return instance;
        }
        set
        {
            instance = value;
            Debug.Log(" <Color=green> SCRIPTABLE_SINGLETON Initialized: </color> <Color=teal> " + instance + "</color> ");
        }
    }

    public static T GetInstance() => Instance;

    public string ResourcesPath => "Assets/Resources/";
    public virtual string BasePath => ResourcesPath;
    public virtual string FilePath => BasePath + typeof(T).Name + ".asset";

    public abstract T Myself
    {
        get;
    }

    public override void InitializeSingleton()
    {
        if (instance == null)
        {
            Instance = Myself;
            scriptableSingletons.Add(this);
        }
        else if (instance != this)
                Debug.LogError("<Color=red> " + this + "  SCRIPTABLE_SINGLETON ALREADY EXIST CONFLICT </color>");
    }
   
}

public abstract class BaseScriptableSingleton : ScriptableObject
{
    public abstract void InitializeSingleton();
}

#if UNITY_EDITOR
public static class ScriptableObjectExtension
{
    public static void CreateAsset<T> (this T myself) where T : ScriptableObject
	{
		T asset = ScriptableObject.CreateInstance<T> ();
 
		string path = AssetDatabase.GetAssetPath (Selection.activeObject);
		if (path == "") 
		{
			path = "Assets";
		} 
		else if (Path.GetExtension (path) != "") 
		{
			path = path.Replace (Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
		}
 
		string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath (path + "/New " + typeof(T).ToString() + ".asset");
 
		AssetDatabase.CreateAsset (asset, assetPathAndName);
		AssetDatabase.SaveAssets ();
        	AssetDatabase.Refresh();
		EditorUtility.FocusProjectWindow ();
		Selection.activeObject = asset;
	}
}
#endif
