using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;


public class GameSettings : EditorWindow    
{
    private Object _selection;
    private string _filterValue = string.Empty;

    [MenuItem("Window/Ishimine/ScriptableSettings")]
    public static void ShowExample()
    {
        GameSettings wnd = GetWindow<GameSettings>();
        wnd.titleContent = new GUIContent("ScriptableSettings");
    }

    public void OnEnable()
    {
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;

        // Import UXML
        var visualTree =
            Resources.Load<VisualTreeAsset>("GameSettings_Main");

        visualTree.CloneTree(root);
    
        // A stylesheet can be added to a VisualElement.    
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet =
             Resources.Load<StyleSheet>("GameSettings_Style");

        root.styleSheets.Add(styleSheet);

        PopulatePresetList();
    }

    private void Update()
    {
        TextField searchField = rootVisualElement.Q<TextField>("SearchField");
        if (searchField.value == null || searchField.value == _filterValue) return;

        _filterValue = searchField.value;
        PopulatePresetList();
    }

    private void PopulatePresetList()
    {
        ListView list = rootVisualElement.Q<ListView>("ListView");
        list.Clear();

        ScriptableSettingsManager gsm = ScriptableSettingsManager.Instance;

        string lowerFilterValue = this._filterValue.ToLowerInvariant();
        for (int i = 0; i < gsm.ScriptableSettings.Count; i++)
        {
            string fieldName = gsm.ScriptableSettings[i].name.ToLowerInvariant();
            if (!fieldName.Contains(lowerFilterValue)) continue;

            VisualElement listContainer = new VisualElement {name = "ListContainer"};
            Button button = new Button {text = gsm.ScriptableSettings[i].name};

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
        }
    }

    private void UpdateSelection(Object target)
    {
        _selection = target;
        PopulatePresetList();
    }
}
