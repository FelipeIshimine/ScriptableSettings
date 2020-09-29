using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = System.Diagnostics.Debug;

public class CreateNewTagWindow : EditorWindow
{
    public float rotationAmount = 0.33f;
    public string selected = "";
    
    private Func<string, bool> _newNameValidator;
    private TextField _textField;
    private Action<string>  _onDone;

    private static CreateNewTagWindow _window;
    
    public static void Show(Func<string, bool> newNameValidator, Action<string> onNameSelected)
    {
        if(_window == null)
            _window = CreateInstance(typeof(CreateNewTagWindow)) as CreateNewTagWindow;
        
        Debug.Assert(_window != null, nameof(_window) + " != null");
        _window.titleContent.text = "New Tag";
        _window.Focus();
        _window._newNameValidator = newNameValidator;
        _window.ShowUtility();
        _window._onDone = onNameSelected;
    }

    private void OnEnable()
    {
        VisualElement root = rootVisualElement;

        var visualTree = Resources.Load<VisualTreeAsset>("NewTag_Main");

        visualTree.CloneTree(root);

        _textField = rootVisualElement.Q<TextField>("TextField");
        Button acceptButton = rootVisualElement.Q<Button>("Create");
        Button cancelButton = rootVisualElement.Q<Button>("CancelB");

        acceptButton.clicked += () => ChangeName(_textField.value);
        cancelButton.clicked += Close;
    }

    private void ChangeName(string newName)
    {
        if (_newNameValidator.Invoke(newName))
        {
            _onDone.Invoke(newName);
            AssetDatabase.SaveAssets();
            Close();
        }
        else
        {
            if(newName != string.Empty)
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