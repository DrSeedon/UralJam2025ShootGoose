using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(MonoBehaviour), true)]
public class UIElementAssignerEditor : Editor
{
    [MenuItem("CONTEXT/MonoBehaviour/Автозаполнение полей компонентов")]
    private static void AssignComponents(MenuCommand command)
    {
        MonoBehaviour script = (MonoBehaviour)command.context;

        if (script == null)
        {
            Debug.LogWarning("Не удалось получить выбранный скрипт.");
            return;
        }

        GameObject go = script.gameObject;

        // Получаем все публичные поля скрипта
        FieldInfo[] fields = script.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => typeof(Object).IsAssignableFrom(f.FieldType))
            .ToArray();

        int assignedCount = 0;

        // Группируем поля по их типам
        var groupedFields = fields.GroupBy(f => f.FieldType);

        foreach (var group in groupedFields)
        {
            System.Type fieldType = group.Key;

            // Получаем компоненты данного типа из дочерних объектов
            Component[] components = go.GetComponentsInChildren(fieldType, true);

            // Сопоставляем поля и компоненты по порядку
            var fieldArray = group.ToArray();
            int count = Mathf.Min(fieldArray.Length, components.Length);

            for (int i = 0; i < count; i++)
            {
                fieldArray[i].SetValue(script, components[i]);
                assignedCount++;
            }
        }

        if (assignedCount > 0)
        {
            Debug.Log($"Автоматически назначено {assignedCount} полей компонентов.");
            EditorUtility.SetDirty(script);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
        else
        {
            Debug.LogWarning("Не найдено компонентов для автозаполнения.");
        }
    }

    [MenuItem("CONTEXT/MonoBehaviour/Автозаполнение полей компонентов (с поддержкой массивов и списков)")]
    private static void AssignComponentsWithCollections(MenuCommand command)
    {
        MonoBehaviour script = (MonoBehaviour)command.context;

        if (script == null)
        {
            Debug.LogWarning("Не удалось получить выбранный скрипт.");
            return;
        }

        GameObject go = script.gameObject;

        // Получаем все публичные поля скрипта
        FieldInfo[] fields = script.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => typeof(Object).IsAssignableFrom(f.FieldType) || IsCollectionType(f.FieldType, out _))
            .ToArray();

        int assignedCount = 0;

        foreach (FieldInfo field in fields)
        {
            System.Type fieldType = field.FieldType;

            // Обработка массивов и списков
            if (IsCollectionType(fieldType, out System.Type elementType))
            {
                if (typeof(Object).IsAssignableFrom(elementType))
                {
                    Component[] components = go.GetComponentsInChildren(elementType, true);

                    if (fieldType.IsArray)
                    {
                        System.Array array = System.Array.CreateInstance(elementType, components.Length);
                        components.CopyTo(array, 0);
                        field.SetValue(script, array);
                        assignedCount++;
                    }
                    else if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        IList list = (IList)System.Activator.CreateInstance(fieldType);
                        foreach (var component in components)
                        {
                            list.Add(component);
                        }
                        field.SetValue(script, list);
                        assignedCount++;
                    }
                }
            }
            else if (typeof(Object).IsAssignableFrom(fieldType))
            {
                // Группируем поля по их типам
                var sameTypeFields = fields.Where(f => f.FieldType == fieldType).ToArray();
                Component[] components = go.GetComponentsInChildren(fieldType, true);

                int count = Mathf.Min(sameTypeFields.Length, components.Length);

                for (int i = 0; i < count; i++)
                {
                    sameTypeFields[i].SetValue(script, components[i]);
                    assignedCount++;
                }

                // Удаляем обработанные поля из списка, чтобы не повторять их обработку
                fields = fields.Except(sameTypeFields).ToArray();
            }
        }

        if (assignedCount > 0)
        {
            Debug.Log($"Автоматически назначено {assignedCount} полей компонентов, включая коллекции.");
            EditorUtility.SetDirty(script);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
        else
        {
            Debug.LogWarning("Не найдено компонентов для автозаполнения.");
        }
    }

    [MenuItem("CONTEXT/MonoBehaviour/Автозаполнение полей компонентов (по всей сцене)")]
    private static void AssignComponentsInScene(MenuCommand command)
    {
        MonoBehaviour script = (MonoBehaviour)command.context;

        if (script == null)
        {
            Debug.LogWarning("Не удалось получить выбранный скрипт.");
            return;
        }

        // Получаем все публичные поля скрипта
        FieldInfo[] fields = script.GetType()
            .GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => typeof(Object).IsAssignableFrom(f.FieldType) || IsCollectionType(f.FieldType, out _))
            .ToArray();

        int assignedCount = 0;

        foreach (FieldInfo field in fields)
        {
            System.Type fieldType = field.FieldType;

            // Обработка массивов и списков
            if (IsCollectionType(fieldType, out System.Type elementType))
            {
                if (typeof(Object).IsAssignableFrom(elementType))
                {
                    // Получаем все компоненты данного типа во всей сцене
                    Component[] components = GetAllComponentsInScene(elementType);

                    if (fieldType.IsArray)
                    {
                        System.Array array = System.Array.CreateInstance(elementType, components.Length);
                        components.CopyTo(array, 0);
                        field.SetValue(script, array);
                        assignedCount++;
                    }
                    else if (fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        IList list = (IList)System.Activator.CreateInstance(fieldType);
                        foreach (var component in components)
                        {
                            list.Add(component);
                        }
                        field.SetValue(script, list);
                        assignedCount++;
                    }
                }
            }
            else if (typeof(Object).IsAssignableFrom(fieldType))
            {
                // Группируем поля по их типам
                var sameTypeFields = fields.Where(f => f.FieldType == fieldType).ToArray();
                Component[] components = GetAllComponentsInScene(fieldType);

                int count = Mathf.Min(sameTypeFields.Length, components.Length);

                for (int i = 0; i < count; i++)
                {
                    sameTypeFields[i].SetValue(script, components[i]);
                    assignedCount++;
                }

                // Удаляем обработанные поля из списка, чтобы не повторять их обработку
                fields = fields.Except(sameTypeFields).ToArray();
            }
        }

        if (assignedCount > 0)
        {
            Debug.Log($"Автоматически назначено {assignedCount} полей компонентов из всей сцены.");
            EditorUtility.SetDirty(script);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(script.gameObject.scene);
        }
        else
        {
            Debug.LogWarning("Не найдено компонентов для автозаполнения во всей сцене.");
        }
    }

    private static Component[] GetAllComponentsInScene(System.Type componentType)
    {
        List<Component> componentsInScene = new List<Component>();

        // Проходим по всем активным сценам
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (scene.isLoaded)
            {
                GameObject[] rootObjects = scene.GetRootGameObjects();

                foreach (GameObject go in rootObjects)
                {
                    componentsInScene.AddRange(go.GetComponentsInChildren(componentType, true));
                }
            }
        }

        return componentsInScene.ToArray();
    }

    private static bool IsCollectionType(System.Type type, out System.Type elementType)
    {
        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return true;
        }
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = null;
        return false;
    }
}