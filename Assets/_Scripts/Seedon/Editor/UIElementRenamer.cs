using UnityEngine;
using UnityEditor;
using System.Reflection;
using TMPro;

[CustomEditor(typeof(MonoBehaviour), true)]
public class UIElementRenamerEditor : Editor
{
    [MenuItem("CONTEXT/MonoBehaviour/Переименовать UI элементы")]
    private static void RenameUIElements(MenuCommand command)
    {
        MonoBehaviour script = (MonoBehaviour)command.context;

        if (script == null)
        {
            Debug.LogWarning("Не удалось получить выбранный скрипт.");
            return;
        }

        int renamedCount = 0;
        FieldInfo[] fields = script.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(script);

            if (value is Component component && component != null)
            {
                string newName = CapitalizeFirstLetter(field.Name);
                component.gameObject.name = newName;
                renamedCount++;
            }
            else if (value is GameObject go && go != null)
            {
                string newName = CapitalizeFirstLetter(field.Name);
                go.name = newName;
                renamedCount++;
            }
        }

        if (renamedCount > 0)
        {
            Debug.Log($"Переименовано {renamedCount} UI элементов.");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
        else
        {
            Debug.LogWarning("Не найдено публичных полей компонентов для переименования.");
        }
    }

    [MenuItem("CONTEXT/MonoBehaviour/Обновить текст в UI элементах")]
    private static void UpdateUIText(MenuCommand command)
    {
        MonoBehaviour script = (MonoBehaviour)command.context;

        if (script == null)
        {
            Debug.LogWarning("Не удалось получить выбранный скрипт.");
            return;
        }

        int updatedCount = 0;
        FieldInfo[] fields = script.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            object value = field.GetValue(script);
            string newText = CapitalizeFirstLetter(field.Name);

            if (value is Component component && component != null)
            {
                if (UpdateTextInObject(component.gameObject, newText))
                {
                    updatedCount++;
                }
            }
            else if (value is GameObject go && go != null)
            {
                if (UpdateTextInObject(go, newText))
                {
                    updatedCount++;
                }
            }
        }

        if (updatedCount > 0)
        {
            Debug.Log($"Обновлено текстов у {updatedCount} UI элементов.");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
        else
        {
            Debug.LogWarning("Не найдено публичных полей компонентов для обновления текста.");
        }
    }

    private static bool UpdateTextInObject(GameObject obj, string newText)
    {
        // Проверяем наличие TMP_Text на самом объекте
        TMP_Text tmpText = obj.GetComponent<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = newText;
            return true;
        }
        else
        {
            // Ищем TMP_Text в дочерних объектах
            tmpText = obj.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
            {
                tmpText.text = newText;
                return true;
            }
        }
        return false;
    }

    private static string CapitalizeFirstLetter(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1);
    }
}