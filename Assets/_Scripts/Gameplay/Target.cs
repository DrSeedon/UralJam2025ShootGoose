using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс, представляющий мишень для стрельбы
/// </summary>
public class Target : MonoBehaviour
{
    #region Fields
    [Header("Target Settings")]
    [SerializeField] private Transform _targetCenter; // Центр мишени
    [SerializeField] private float _targetWidth = 3f; // Ширина плоскости мишени
    [SerializeField] private float _targetHeight = 3f; // Высота плоскости мишени
    [SerializeField] private bool _drawTargetGizmo = true; // Нужно ли отображать гизмо плоскости
    [SerializeField] private Color _targetPlaneColor = new Color(1f, 0f, 0f, 0.3f); // Цвет плоскости мишени
    
    [Header("Hit Visualization")]
    [SerializeField] private GameObject _hitMarkerPrefab; // Префаб маркера попадания
    [SerializeField] private float _hitMarkerLifetime = 30f; // Время жизни маркера попадания
    [SerializeField] private int _maxHitMarkers = 10; // Максимальное количество маркеров
    [SerializeField] private GameObject[] _hitEffectPrefabs; // Префабы эффектов попадания для разных уровней точности
    
    // Список активных маркеров попадания
    private List<GameObject> _hitMarkers = new List<GameObject>();
    #endregion

    #region Properties
    /// <summary>
    /// Возвращает позицию центра мишени в мировых координатах
    /// </summary>
    public Vector3 TargetCenter => _targetCenter != null ? _targetCenter.position : transform.position;

    /// <summary>
    /// Нормаль плоскости мишени (направление, в котором "смотрит" мишень)
    /// </summary>
    public Vector3 TargetNormal => _targetCenter != null ? _targetCenter.forward : transform.forward;

    /// <summary>
    /// Нормаль плоскости мишени (синоним для TargetNormal)
    /// </summary>
    public Vector3 PlaneNormal => TargetNormal;

    /// <summary>
    /// Возвращает эффективный радиус мишени (половина диагонали)
    /// </summary>
    public float GetTargetRadius()
    {
        // Используем диагональ прямоугольника как эффективный радиус
        return Mathf.Sqrt(_targetWidth * _targetWidth + _targetHeight * _targetHeight) / 2f;
    }

    /// <summary>
    /// Проверяет, находится ли точка внутри плоскости мишени
    /// </summary>
    public bool IsPointInTargetPlane(Vector3 point)
    {
        if (_targetCenter == null) return false;
        
        // Переводим точку в локальные координаты мишени
        Vector3 localPoint = _targetCenter.InverseTransformPoint(point);
        
        // Проверяем, лежит ли точка в пределах плоскости мишени
        return Mathf.Abs(localPoint.x) <= _targetWidth / 2f && 
               Mathf.Abs(localPoint.y) <= _targetHeight / 2f &&
               Mathf.Abs(localPoint.z) <= 0.1f; // Небольшой допуск по Z
    }

    /// <summary>
    /// Проверяет, находится ли точка внутри плоскости мишени (синоним для IsPointInTargetPlane)
    /// </summary>
    public bool IsPointInsideTargetPlane(Vector3 point)
    {
        return IsPointInTargetPlane(point);
    }

    /// <summary>
    /// Пересекает ли отрезок плоскость мишени
    /// </summary>
    public bool IsLineIntersectingTargetPlane(Vector3 lineStart, Vector3 lineEnd, out Vector3 intersectionPoint)
    {
        intersectionPoint = Vector3.zero;
        if (_targetCenter == null) return false;
        
        // Плоскость мишени
        Plane targetPlane = new Plane(TargetNormal, TargetCenter);
        
        // Направление линии
        Vector3 lineDirection = lineEnd - lineStart;
        float lineMagnitude = lineDirection.magnitude;
        
        if (lineMagnitude < Mathf.Epsilon) return false;
        
        Ray ray = new Ray(lineStart, lineDirection / lineMagnitude);
        
        if (targetPlane.Raycast(ray, out float distance))
        {
            // Проверяем, находится ли точка пересечения в пределах отрезка
            if (distance <= lineMagnitude)
            {
                // Получаем точку пересечения
                intersectionPoint = ray.GetPoint(distance);
                
                // Проверяем, находится ли точка пересечения внутри плоскости мишени
                return IsPointInTargetPlane(intersectionPoint);
            }
        }
        
        return false;
    }
    #endregion

    #region Unity Lifecycle
    private void OnDrawGizmos()
    {
        if (!_drawTargetGizmo || _targetCenter == null) return;
        
        // Сохраняем текущий цвет гизмо
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        
        // Устанавливаем матрицу трансформации для гизмо
        Gizmos.matrix = _targetCenter.localToWorldMatrix;
        
        // Рисуем плоскость мишени
        Gizmos.color = _targetPlaneColor;
        Vector3 center = Vector3.zero;
        Vector3 size = new Vector3(_targetWidth, _targetHeight, 0.01f);
        Gizmos.DrawCube(center, size);
        
        // Рисуем контур плоскости более ярким цветом
        Gizmos.color = new Color(_targetPlaneColor.r, _targetPlaneColor.g, _targetPlaneColor.b, 1f);
        Gizmos.DrawWireCube(center, size);
        
        // Рисуем перекрестие в центре мишени
        float crossSize = Mathf.Min(_targetWidth, _targetHeight) * 0.1f;
        Gizmos.DrawLine(new Vector3(-crossSize, 0, 0), new Vector3(crossSize, 0, 0));
        Gizmos.DrawLine(new Vector3(0, -crossSize, 0), new Vector3(0, crossSize, 0));
        
        // Рисуем круг, показывающий эффективный радиус для расчета точности
        Gizmos.color = Color.yellow;
        DrawCircle(center, GetTargetRadius(), 32);
        
        // Восстанавливаем настройки гизмо
        Gizmos.color = previousColor;
        Gizmos.matrix = previousMatrix;
    }
    
    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Регистрирует попадание в мишень
    /// </summary>
    /// <param name="hitPosition">Позиция попадания</param>
    /// <param name="accuracy">Точность попадания (0-100)</param>
    public void RegisterHit(Vector3 hitPosition, float accuracy)
    {
        // Создаем маркер попадания
        if (_hitMarkerPrefab != null)
        {
            // Очищаем старые маркеры, если их слишком много
            CleanupOldHitMarkers();
            
            // Создаем новый маркер
            GameObject marker = Instantiate(_hitMarkerPrefab, hitPosition, Quaternion.identity);
            _hitMarkers.Add(marker);
            
            // Уничтожаем маркер через некоторое время
            Destroy(marker, _hitMarkerLifetime);
        }
        
        // Воспроизводим эффект попадания в зависимости от точности
        PlayHitEffect(hitPosition, accuracy);
    }
    #endregion

    #region Private Methods
    private void PlayHitEffect(Vector3 position, float accuracy)
    {
        if (_hitEffectPrefabs == null || _hitEffectPrefabs.Length == 0) 
            return;
            
        // Выбираем эффект в зависимости от точности
        int effectIndex;
        
        if (accuracy >= 90f)
            effectIndex = 0; // Самый лучший эффект для высокой точности
        else if (accuracy >= 60f)
            effectIndex = Mathf.Min(1, _hitEffectPrefabs.Length - 1);
        else if (accuracy >= 30f)
            effectIndex = Mathf.Min(2, _hitEffectPrefabs.Length - 1);
        else
            effectIndex = Mathf.Min(3, _hitEffectPrefabs.Length - 1);
            
        // Проверяем доступность выбранного эффекта
        if (effectIndex < _hitEffectPrefabs.Length && _hitEffectPrefabs[effectIndex] != null)
        {
            Instantiate(_hitEffectPrefabs[effectIndex], position, Quaternion.identity);
        }
    }
    
    private void CleanupOldHitMarkers()
    {
        // Если маркеров больше максимального количества, удаляем самые старые
        while (_hitMarkers.Count >= _maxHitMarkers)
        {
            if (_hitMarkers[0] != null)
            {
                Destroy(_hitMarkers[0]);
            }
            _hitMarkers.RemoveAt(0);
        }
        
        // Очищаем список от null ссылок (уже уничтоженных маркеров)
        _hitMarkers.RemoveAll(marker => marker == null);
    }
    #endregion
}