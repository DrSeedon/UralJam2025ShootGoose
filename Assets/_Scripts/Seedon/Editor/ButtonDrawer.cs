using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(MonoBehaviour), true)]
public class ButtonDrawer : Editor
{
    public override void OnInspectorGUI()
    {
        // Рисуем стандартные поля
        DrawDefaultInspector();

        // Получаем целевой объект
        var mono = target as MonoBehaviour;

        // Получаем все методы объекта
        var methods = mono.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            // Ищем наш атрибут
            var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
            if (buttonAttribute != null)
            {
                string buttonName = string.IsNullOrEmpty(buttonAttribute.Name) 
                    ? method.Name 
                    : buttonAttribute.Name;

                // Рисуем кнопку
                if (GUILayout.Button(buttonName))
                {
                    method.Invoke(mono, null);
                }
            }
        }
    }
}