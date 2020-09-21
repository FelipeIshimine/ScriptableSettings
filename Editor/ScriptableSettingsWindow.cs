using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;

public class ScriptableSettingsWindow : EditorWindow    
{
    private Object _selection;
    private string _filterValue = string.Empty;

    private const int leftPanelMaxWidth = 170;

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
        
         PopulatePresetList();
    }

    private void PopulatePresetList()
    {
        ListView list = rootVisualElement.Q<ListView>("ListView");
        list.Clear();
        
        ScriptableSettingsManager gsm = ScriptableSettingsManager.Instance;

        string lowerFilterValue = this._filterValue.ToLowerInvariant();

        bool forceUpdate = false;
        for (int i = 0; i < gsm.ScriptableSettings.Count; i++)
        {
            if (gsm.ScriptableSettings[i] == null)
            {
                forceUpdate = true;
                break;
            }
            string fieldName = gsm.ScriptableSettings[i].TabName.ToLowerInvariant();
            if (!fieldName.Contains(lowerFilterValue)) continue;

            VisualElement listContainer = new VisualElement {name = "ListContainer"};
            Button button = new Button {text = gsm.ScriptableSettings[i].TabName};

            //Applying a CSS class to an element
            button.AddToClassList("ListLabel");
            listContainer.Add(button);

            //Inserting element into list
            list.Insert(list.childCount, listContainer);
            ScriptableSettings value = gsm.ScriptableSettings[i];

            if (_selection == value)
            {
                button.style.backgroundColor = new StyleColor(new Color(0.25f, 0.35f, 0.6f, 1f));
                UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(value);
                rootVisualElement.Q<IMGUIContainer>("Target").onGUIHandler = () => editor.OnInspectorGUI();
            }
            button.clicked += () => UpdateSelection(value);
            SetContextMenuManipulator(button, value);
        }

        if (forceUpdate)
        {
            gsm._Update();
            PopulatePresetList();
        }
    }

    private void OnSearchFieldChange(ChangeEvent<string> evt)
    {
        _filterValue = evt.newValue;
        PopulatePresetList();
    }
    
    void SetContextMenuManipulator(Button element, ScriptableSettings scriptableSettings)
    {
        ContextualMenuManipulator m = new ContextualMenuManipulator(x => ShowButtonContextMenu(scriptableSettings,x)) {target = element};
        
        //Context
    }

    private void ShowButtonContextMenu(ScriptableSettings settings, ContextualMenuPopulateEvent contextualMenuPopulateEvent)
    {
        contextualMenuPopulateEvent.menu.AppendAction("Rename", x => RenameScriptableSetting(settings));
    }

    private void RenameScriptableSetting(ScriptableSettings settings)
    {
        ChangeTabNameWindow.Show(settings, IsValidName, PopulatePresetList);
    }

    private static bool IsValidName(string nName)
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

}