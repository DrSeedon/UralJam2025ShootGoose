using System;

[AttributeUsage(AttributeTargets.Method)] // Указываем, что атрибут можно использовать только с методами
public class ButtonAttribute : Attribute  // Наследуемся от Attribute вместо PropertyAttribute
{
    public string Name { get; private set; }

    public ButtonAttribute(string name = null)
    {
        Name = name;
    }
}