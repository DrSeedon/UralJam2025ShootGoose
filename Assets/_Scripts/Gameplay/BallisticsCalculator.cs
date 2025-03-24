using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Калькулятор баллистической траектории снаряда
/// </summary>
public class BallisticsCalculator : MonoBehaviour
{
    #region Fields
    [Header("Settings")]
    [SerializeField] private float _baseProjectileSpeed = 20f; // Базовая скорость снаряда
    [SerializeField] private float _gravity = 9.81f; // Сила гравитации
    [SerializeField] private float _airResistance = 0.1f; // Сопротивление воздуха
    [SerializeField] private float _powderEfficiency = 0.5f; // Коэффициент эффективности пороха
    [SerializeField] private float _maxPowderPercentage = 90f; // Максимально безопасный процент пороха
    [SerializeField] private int _trajectoryResolution = 50; // Детализация расчета траектории
    [SerializeField] private float _maxTrajectoryDistance = 100f; // Максимальная длина траектории
    [SerializeField] private float _minPowderPercentage = 0.1f; // Минимальный процент пороха для полета
    
    [Header("Gizmos")]
    [SerializeField] private bool _drawTrajectoryGizmo = true;
    [SerializeField] private Color _trajectoryColor = Color.yellow;
    [SerializeField] private float _trajectoryWidth = 0.1f;
    
    [Inject] private Target _target;
    
    // Сохраненная траектория для отображения в Gizmos
    private List<Vector3> _lastCalculatedTrajectory = new List<Vector3>();
    #endregion

    #region Unity Lifecycle
    private void OnDrawGizmos()
    {
        if (!_drawTrajectoryGizmo || _lastCalculatedTrajectory.Count < 2) 
            return;
        
        Gizmos.color = _trajectoryColor;
        
        // Рисуем линии траектории
        for (int i = 0; i < _lastCalculatedTrajectory.Count - 1; i++)
        {
            Vector3 start = _lastCalculatedTrajectory[i];
            Vector3 end = _lastCalculatedTrajectory[i + 1];
            
            // Линии траектории
            Gizmos.DrawLine(start, end);
            
            // Маленькие сферы на узловых точках
            if (i % 5 == 0 || i == _lastCalculatedTrajectory.Count - 2)
            {
                Gizmos.DrawSphere(start, _trajectoryWidth * 0.5f);
            }
        }
        
        // Сфера в конечной точке
        if (_lastCalculatedTrajectory.Count > 0)
        {
            Vector3 impactPoint = _lastCalculatedTrajectory[_lastCalculatedTrajectory.Count - 1];
            Gizmos.DrawSphere(impactPoint, _trajectoryWidth * 0.8f);
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Рассчитывает траекторию снаряда
    /// </summary>
    /// <param name="startPosition">Начальная позиция</param>
    /// <param name="direction">Направление выстрела</param>
    /// <param name="powderPercentage">Процент заряда пороха (0-100)</param>
    /// <returns>Список точек траектории</returns>
    public List<Vector3> CalculateTrajectory(Vector3 startPosition, Vector3 direction, float powderPercentage)
    {
        List<Vector3> trajectory = new List<Vector3>();
        
        // Проверяем минимальный порох
        bool minimalPowder = powderPercentage <= _minPowderPercentage;
        
        // Нормализуем направление
        direction.Normalize();
        
        // Начальная позиция
        Vector3 position = startPosition;
        
        // Добавляем начальную точку
        trajectory.Add(position);
        
        // Для случая с минимальным порохом делаем упрощенную траекторию падения
        if (minimalPowder)
        {
            // Просто создаем короткую траекторию вниз-вперед
            Vector3 endPos = startPosition + direction * 0.5f - Vector3.up * 2f;
            trajectory.Add(endPos);
            
            _lastCalculatedTrajectory = new List<Vector3>(trajectory);
            return trajectory;
        }
        
        // Рассчитываем начальную скорость на основе заряда пороха
        float initialSpeed = _baseProjectileSpeed * (1f + powderPercentage * _powderEfficiency / 100f);
        
        // Начальная скорость
        Vector3 velocity = direction * initialSpeed;
        
        // Шаг времени (фиксированное значение для предсказуемости)
        float timeStep = 0.05f;
        
        for (int i = 0; i < _trajectoryResolution; i++)
        {
            // Сохраняем предыдущую позицию для проверки пересечения с мишенью
            Vector3 prevPosition = position;
            
            // Применяем гравитацию
            velocity.y -= _gravity * timeStep;
            
            // Применяем сопротивление воздуха
            velocity *= (1f - _airResistance * timeStep);
            
            // Обновляем позицию
            position += velocity * timeStep;
            
            // Добавляем точку в траекторию
            trajectory.Add(position);
            
            // Проверяем пересечение с мишенью
            if (_target != null && _target.IsLineIntersectingTargetPlane(prevPosition, position, out Vector3 intersectionPoint))
            {
                // Заменяем последнюю точку на точку пересечения с мишенью
                trajectory[trajectory.Count - 1] = intersectionPoint;
                break; // Прерываем расчет, так как снаряд попал в мишень
            }
            
            // Проверяем, достигла ли траектория земли или максимальной дистанции
            if (position.y <= 0 || Vector3.Distance(startPosition, position) > _maxTrajectoryDistance)
            {
                // Корректируем Y-координату, чтобы траектория заканчивалась точно на земле
                if (position.y < 0)
                {
                    float t = prevPosition.y / (prevPosition.y - position.y);
                    position = Vector3.Lerp(prevPosition, position, t);
                    trajectory[trajectory.Count - 1] = position;
                }
                break;
            }
        }
        
        // Сохраняем траекторию для отображения в Gizmos
        _lastCalculatedTrajectory = new List<Vector3>(trajectory);
        
        return trajectory;
    }
    
    /// <summary>
    /// Проверяет, безопасен ли заряд пороха
    /// </summary>
    public bool IsPowderSafe(float powderPercentage)
    {
        return powderPercentage <= _maxPowderPercentage;
    }
    
    /// <summary>
    /// Проверяет, достаточен ли заряд пороха для полета
    /// </summary>
    public bool IsPowderSufficient(float powderPercentage)
    {
        return powderPercentage > _minPowderPercentage;
    }
    #endregion
} 