using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = System.Diagnostics.Debug;

public class ChangeTabNameWindow : EditorWindow
{
    public float rotationAmount = 0.33f;
    public string selected = "";

    public ScriptableSettings settings;
    private Func<string, bool> _newNameValidator;
    private TextField _textField;
    private Action _onDone;

    private static ChangeTabNameWindow window;
    public static void Show(ScriptableSettings settings, Func<string, bool> newNameValidator, Action onDone)
    {
        if(window == null)
            window = CreateInstance(typeof(ChangeTabNameWindow)) as ChangeTabNameWindow;
        
        Debug.Assert(window != null, nameof(window) + " != null");
        window.titleContent.text = "Rename tab";
        window.Focus();
        window.SetSettings(settings);
        window._newNameValidator = newNameValidator;
        window.ShowUtility();
        window._onDone = onDone;
    }

    private void SetSettings(ScriptableSettings scriptableSettings)
    {
        settings = scriptableSettings;
        _textField.SetValueWithoutNotify(scriptableSettings.TabName);
    }

    private void OnEnable()
    {
        VisualElement root = rootVisualElement;

        var visualTree = Resources.Load<VisualTreeAsset>("RenameTab_Main");

        visualTree.CloneTree(root);

        _textField = rootVisualElement.Q<TextField>("TextField");
        Button acceptButton = rootVisualElement.Q<Button>("AcceptB");
        Button cancelButton = rootVisualElement.Q<Button>("CancelB");
        Button defaultButton = rootVisualElement.Q<Button>("DefaultB");

        acceptButton.clicked += () => ChangeName(_textField.value);
        cancelButton.clicked += Close;
        defaultButton.clicked += () => _textField.SetValueWithoutNotify(settings.DefaultTabName);
    }

    private void ChangeName(string newName)
    {
        if (_newNameValidator.Invoke(newName))
        {
            settings.TabName = newName;
            _onDone.Invoke();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Close();
        }
        else
        {
            if(newName != string.Empty && newName != settings.TabName)
                EditorUtility.DisplayDialog("Invalid name", $"Cant use '{newName}' as new name, already in use", "OK");
        }
    }

    private void OnGUI()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            ChangeName(_textField.value);
        else if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }
}