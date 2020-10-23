using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;

public class ScriptableSettingsWindow : EditorWindow    
{
    private Object _selection;
    private List<ScriptableTag> _activeTags = new List<ScriptableTag>();
    private string _filterValue = string.Empty;

    private const int leftPanelMaxWidth = 170;
    private bool _isTagFoldoutOpen;
    Toggle includeSS;
    [MenuItem("Window/Ishimine/ScriptableSettings %#i", priority = 1)]
    public static void ShowWindow()
    {
        ScriptableSettingsManager.Update();
        ScriptableSettingsWindow wnd = GetWindow<ScriptableSettingsWindow>();
        wnd.titleContent = new GUIContent("ScriptableSettings");
    }

    public void OnEnable()
    {
        VisualElement root = rootVisualElement;

        var visualTree = Resources.Load<VisualTreeAsset>("GameSettings_Main");  

        visualTree.CloneTree(root);

        var styleSheet = Resources.Load<StyleSheet>("GameSettings_Style");

        root.styleSheets.Add(styleSheet);

        ToolbarSearchField _toolbarSearchField = rootVisualElement.Q<ToolbarSearchField>("SearchField");
        _toolbarSearchField.RegisterValueChangedCallback(OnSearchFieldChange);
        _toolbarSearchField.name = "SearchField";

        _toolbarSearchField.AddToClassList("ListSearchField");
        rootVisualElement.Q<VisualElement>("LeftPanel").style.maxWidth = leftPanelMaxWidth;

        includeSS = root.Q<Toggle>("IncludeSSToggle");
        includeSS.SetValueWithoutNotify(ScriptableSettingsManager.ShowRuntimeScriptableSingleton);
        includeSS.RegisterValueChangedCallback(OnIncludeSSToggle);

        PopulateTags(false);
        PopulatePresetList();
    }

    private void OnIncludeSSToggle(ChangeEvent<bool> evt)
    {
        ScriptableSettingsManager.ShowRuntimeScriptableSingleton = evt.newValue;
        PopulatePresetList();
    }

    private void PopulateTags(bool isOpen)
    {
        Foldout tagsFoldout = rootVisualElement.Q<Foldout>("TagsFoldout");
        tagsFoldout.SetValueWithoutNotify(isOpen);
        tagsFoldout.Clear();
        ListView listView = CreateListViewForTags();
        tagsFoldout.Add(listView);

        List<ScriptableTag> tags = ScriptableSettingsManager.Instance.Tags;
        for (int i = 0; i < tags.Count; i++)
        {
            Toggle toggle = new Toggle();
            toggle.text = " " + tags[i].name;
            int index = i;
            toggle.RegisterValueChangedCallback(x => OnToggleTag(tags[index], x));
            ContextualMenuManipulator manipulator = new ContextualMenuManipulator(x =>
                {
                   x.menu.AppendAction("Delete",y => DeleteTag(tags[index])); 
                }){target = toggle};
            listView.Add(toggle);
        }

        listView.style.height = Mathf.Min(tags.Count * 20, 100);
        Button tagsAdd = rootVisualElement.Q<Button>("TagsAdd");
        tagsAdd.clicked -= GoToCreateNewTag;
        tagsAdd.clicked += GoToCreateNewTag;
    }

    private static ListView CreateListViewForTags()
    {
        ListView listView = new ListView();
        listView.style.width = StyleKeyword.Auto;
        listView.style.height = 110;
        listView.style.flexShrink = 1;
        listView.style.flexGrow = 1;
        return listView;
    }

    private void OnToggleTag(ScriptableTag scriptableSettingsTag, ChangeEvent<bool> evt)
    {
        if (_activeTags.Contains(scriptableSettingsTag)) _activeTags.Remove(scriptableSettingsTag);
        else _activeTags.Add(scriptableSettingsTag);
        PopulatePresetList();
    }

    private void PopulatePresetList()
    {
        ListView list = rootVisualElement.Q<ListView>("ListView");
        list.Clear();
        
        string lowerFilterValue = this._filterValue.ToLowerInvariant();

        List<ScriptableSettings> settings = GetScriptableSettingFilteredByTags();

        List<BaseRuntimeScriptableSingleton> baseScriptableSingletons = (includeSS.value)? GetScriptableObjectsFilteredByTags(): new List<BaseRuntimeScriptableSingleton>();

        List<ScriptableObject> scriptableObjects = new List<ScriptableObject>(settings);
        scriptableObjects.AddRange(baseScriptableSingletons);        
        scriptableObjects.Sort(SettinsSorter);
        for (int i = 0; i < scriptableObjects.Count; i++)
        {
            if (scriptableObjects[i] is ScriptableSettings scriptableSettingsA)
                if (!scriptableSettingsA.TabName.ToLowerInvariant().Contains(lowerFilterValue)) continue;
            else
                if (!scriptableObjects[i].name.ToLowerInvariant().Contains(lowerFilterValue)) continue;

            VisualElement listContainer = new VisualElement {name = "ListContainer"};
            Button button = new Button
            {
                text = scriptableObjects[i] is ScriptableSettings scriptableSettingsB
                    ? scriptableSettingsB.TabName           //Es un Settings
                    : scriptableObjects[i].name             //Es un RuntimeSingleton
            };

            //Applying a CSS class to an element
            button.AddToClassList("ListLabel");
            listContainer.Add(button);

            //Inserting element into list
            list.Insert(list.childCount, listContainer);
            ScriptableObject value = scriptableObjects[i];

            if (_selection == value)
            {
                button.style.backgroundColor = new StyleColor(new Color(0.25f, 0.35f, 0.6f, 1f));
                UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(value);
                rootVisualElement.Q<IMGUIContainer>("Target").onGUIHandler = () => editor.OnInspectorGUI();
                Foldout tagFoldout = rootVisualElement.Q<Foldout>("SettingTagsSelection");
                
                CreateSettingTagsFoldout(ref tagFoldout, value, _isTagFoldoutOpen);
                
                tagFoldout.name = "SettingTagsSelection";
                tagFoldout.RegisterValueChangedCallback(x => _isTagFoldoutOpen = x.newValue);
                rootVisualElement.Q<VisualElement>("RightPanel").Add(tagFoldout);
            }

            button.clicked += () => UpdateSelection(value);
            if(value is ScriptableSettings scriptableSettings)
                SetContextMenuManipulator(button, scriptableSettings);
        }
    }

    private static int SettinsSorter(ScriptableObject x, ScriptableObject y)
    {
        string sA = x is ScriptableSettings sX ? sX.TabName : x.name;
        string sB = x is ScriptableSettings sY ? sY.TabName : y.name;
        return string.Compare(sA, sB, StringComparison.Ordinal);
    }

    private Foldout CreateSettingTagsFoldout(ref Foldout tagsFoldout, ScriptableObject value,bool isOpen)
    {
        if(tagsFoldout== null)
            tagsFoldout= new Foldout();
        tagsFoldout.SetValueWithoutNotify(isOpen);
        tagsFoldout.Clear();

        ListView listView = CreateListViewForTags();
        tagsFoldout.Add(listView);
        
        List<ScriptableTag> tags = ScriptableSettingsManager.Instance.Tags;
        for (int j = 0; j < tags.Count; j++)
        {
            Toggle toggle = new Toggle {text = tags[j].name};
            int index = j;
            toggle.SetValueWithoutNotify(tags[index].Elements.Contains(value));
            toggle.RegisterValueChangedCallback(x => OnSettingsTagToggle(value, tags[index], x));
            listView.Add(toggle);
        }
        listView.style.height = Mathf.Min(tags.Count * 20, 100);
        return tagsFoldout;
    }

    private void DeleteTag(ScriptableTag tag)
    {
        if (EditorUtility.DisplayDialog($"Delete tag {tag.name}",
            $"Are you sure you want to delete the tag {tag.name}?", "Yes", "Cancel"))
        {
            ScriptableSettingsManager.DeleteTag(tag);
            PopulateTags(true);
            PopulatePresetList();
        }
    }

    private void OnSettingsTagToggle(ScriptableObject scriptableSettings, ScriptableTag tag,
        ChangeEvent<bool> evt)
    {
        if (evt.newValue)
            tag.Elements.Add(scriptableSettings);
        else
            tag.Elements.Remove(scriptableSettings);
        
        PopulatePresetList();
    }

    private List<ScriptableSettings> GetScriptableSettingFilteredByTags()
    {
        if(_activeTags == null || _activeTags.Count == 0) return ScriptableSettingsManager.Instance.ScriptableSettings;

        List<ScriptableSettings> settings = new List<ScriptableSettings>(ScriptableSettingsManager.Instance.ScriptableSettings);

        for (int index = settings.Count - 1; index >= 0; index--)
        {
            ScriptableSettings item = settings[index];
            foreach (ScriptableTag tag in _activeTags)
            {
                if (tag.Elements.Contains(item)) continue;
                settings.RemoveAt(index);
                break;
            }
        }
        return new List<ScriptableSettings>(settings);
    }
    
    private List<BaseRuntimeScriptableSingleton> GetScriptableObjectsFilteredByTags()
    {
        List<BaseRuntimeScriptableSingleton> baseRuntimeScriptableSingletons = FindAssetsByType<BaseRuntimeScriptableSingleton>();
        if(_activeTags == null || _activeTags.Count == 0) return baseRuntimeScriptableSingletons;

        for (int index = baseRuntimeScriptableSingletons.Count - 1; index >= 0; index--)
        {
            BaseRuntimeScriptableSingleton item = baseRuntimeScriptableSingletons[index];
            foreach (ScriptableTag tag in _activeTags)
            {
                if (tag.Elements.Contains(item)) continue;
                baseRuntimeScriptableSingletons.RemoveAt(index);
                break;
            }
        }
        return baseRuntimeScriptableSingletons;
    }
    
    public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
 {
     List<T> assets = new List<T>();
     string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T)));
     for( int i = 0; i < guids.Length; i++ )
     {
         string assetPath = AssetDatabase.GUIDToAssetPath( guids[i] );
         T asset = AssetDatabase.LoadAssetAtPath<T>( assetPath );
         if( asset != null )
         {
             assets.Add(asset);
         }
     }
     return assets;
 }

    private void OnSearchFieldChange(ChangeEvent<string> evt)
    {
        _filterValue = evt.newValue;
        PopulatePresetList();
    }
    
    void SetContextMenuManipulator(Button element, ScriptableSettings scriptableSettings)
    {
        ContextualMenuManipulator m = new ContextualMenuManipulator(x => ShowButtonContextMenu(scriptableSettings,x)) {target = element};
    }

    private void ShowButtonContextMenu(ScriptableSettings settings, ContextualMenuPopulateEvent contextualMenuPopulateEvent)
    {
        contextualMenuPopulateEvent.menu.AppendAction("Rename", x => RenameScriptableSetting(settings));
    }

    private void RenameScriptableSetting(ScriptableSettings settings)
    {
        ChangeTabNameWindow.Show(settings, IsValidSettingsName, ()=> PopulatePresetList());
    }

    private static bool IsValidSettingsName(string nName)
    {
        if (nName == string.Empty)
            return false;

        ScriptableSettingsManager gsm = ScriptableSettingsManager.Instance;
        for (int i = 0; i < gsm.ScriptableSettings.Count; i++)
        {
            if (gsm.ScriptableSettings[i].TabName == nName)
                return false;
        }
        return true;
    }    

    private void UpdateSelection(Object target)
    {
        _selection = target;
        PopulatePresetList();
    }
    
    private void GoToCreateNewTag()
    {
        CreateNewTagWindow.Show(IsValidTagName, OnTagNameSelected);
    }

    private void OnTagNameSelected(string obj)
    {
        ScriptableSettingsManager.CreateNewTag(obj);
        PopulatePresetList();
        PopulateTags(true);
    }

    public bool IsValidTagName(string arg)
    {
        foreach (ScriptableTag tag in ScriptableSettingsManager.Instance.Tags)
            if (tag.name == arg)
                return false;
        return true;
    }
}