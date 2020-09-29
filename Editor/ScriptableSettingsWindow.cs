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
    private List<ScriptableSettingsTag> _activeTags = new List<ScriptableSettingsTag>();
    private string _filterValue = string.Empty;

    private const int leftPanelMaxWidth = 170;
    private bool _isTagFoldoutOpen;

    [MenuItem("Window/Ishimine/ScriptableSettings %&i", priority = 1)]
    public static void ShowExample()
    {
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
        
        ToolbarSearchField  _toolbarSearchField = rootVisualElement.Q<ToolbarSearchField>("SearchField");
         _toolbarSearchField.RegisterValueChangedCallback(OnSearchFieldChange);
         _toolbarSearchField.name = "SearchField";
                       
         _toolbarSearchField.AddToClassList("ListSearchField");
         rootVisualElement.Q<VisualElement>("LeftPanel").style.maxWidth = leftPanelMaxWidth;
                 
         Button refreshButton = rootVisualElement.Q<Button>("Update");
        refreshButton.clicked += () =>
        {
            ScriptableSettingsManager.Update();
            Close();
            ShowExample();
        };
        
         PopulateTags(false);
         PopulatePresetList();
    }

    private void PopulateTags(bool isOpen)
    {
        Foldout tagsFoldout = rootVisualElement.Q<Foldout>("TagsFoldout");
        tagsFoldout.SetValueWithoutNotify(isOpen);
        tagsFoldout.Clear();
        List<ScriptableSettingsTag> tags = ScriptableSettingsManager.Instance.Tags;
        for (int i = 0; i < tags.Count; i++)
        {
            Toggle toggle = new Toggle();
            toggle.text = " " + tags[i].name;
            int index = i;
            toggle.RegisterValueChangedCallback(x => OnToggleTag(tags[index], x));
            tagsFoldout.Add(toggle);
            
            
            ContextualMenuManipulator manipulator = new ContextualMenuManipulator(x =>
                {
                   x.menu.AppendAction("Delete",y => DeleteTag(tags[index])); 
                }){target = toggle};
            tagsFoldout.Add(toggle);
            
        }

        Button tagsAdd = rootVisualElement.Q<Button>("TagsAdd");
        tagsAdd.clicked -= GoToCreateNewTag;
        tagsAdd.clicked += GoToCreateNewTag;
    }

    private void OnToggleTag(ScriptableSettingsTag scriptableSettingsTag, ChangeEvent<bool> evt)
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

        List<ScriptableSettings> settings = GetFilteredTags();

        settings.Sort(SettinsSorter);
        for (int i = 0; i < settings.Count; i++)
        {
            string fieldName = settings[i].TabName.ToLowerInvariant();
            
            if (!fieldName.Contains(lowerFilterValue)) continue;

            VisualElement listContainer = new VisualElement {name = "ListContainer"};
            Button button = new Button {text = settings[i].TabName};

            //Applying a CSS class to an element
            button.AddToClassList("ListLabel");
            listContainer.Add(button);

            //Inserting element into list
            list.Insert(list.childCount, listContainer);
            ScriptableSettings value = settings[i];

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
            SetContextMenuManipulator(button, value);
        }
    }

    private static int SettinsSorter(ScriptableSettings x, ScriptableSettings y)=> string.Compare(x.TabName, y.TabName, StringComparison.Ordinal);

    private Foldout CreateSettingTagsFoldout(ref Foldout tagsFoldout, ScriptableSettings value,bool isOpen)
    {
        if(tagsFoldout== null)
            tagsFoldout= new Foldout();
        
        tagsFoldout.SetValueWithoutNotify(isOpen);
        tagsFoldout.Clear();
        List<ScriptableSettingsTag> tags = ScriptableSettingsManager.Instance.Tags;
        for (int j = 0; j < tags.Count; j++)
        {
            Toggle toggle = new Toggle {text = tags[j].name};
            int index = j;
            toggle.SetValueWithoutNotify(tags[index].Elements.Contains(value));
            toggle.RegisterValueChangedCallback(x => OnSettingsTagToggle(value, tags[index], x));
            tagsFoldout.Add(toggle);
        }
        return tagsFoldout;
    }

    private void DeleteTag(ScriptableSettingsTag tag)
    {
        ScriptableSettingsManager.DeleteTag(tag);
        PopulateTags(true);
        PopulatePresetList();
    }

    private void OnSettingsTagToggle(ScriptableSettings scriptableSettings, ScriptableSettingsTag tag,
        ChangeEvent<bool> evt)
    {
        if (evt.newValue)
            tag.Elements.Add(scriptableSettings);
        else
            tag.Elements.Remove(scriptableSettings);
        
        PopulatePresetList();
    }

    private List<ScriptableSettings> GetFilteredTags()
    {
        if(_activeTags == null || _activeTags.Count == 0) return ScriptableSettingsManager.Instance.ScriptableSettings;

        List<ScriptableSettings> settings = new List<ScriptableSettings>(ScriptableSettingsManager.Instance.ScriptableSettings);

        for (int index = settings.Count - 1; index >= 0; index--)
        {
            ScriptableSettings item = settings[index];
            foreach (ScriptableSettingsTag tag in _activeTags)
            {
                if (tag.Elements.Contains(item)) continue;
                settings.RemoveAt(index);
                break;
            }
        }
        return new List<ScriptableSettings>(settings);
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

    private void UpdateSelection(ScriptableSettings target)
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
        foreach (ScriptableSettingsTag tag in ScriptableSettingsManager.Instance.Tags)
            if (tag.name == arg)
                return false;
        return true;
    }
}