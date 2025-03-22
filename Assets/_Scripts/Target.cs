using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Класс, представляющий мишень для стрельбы
/// </summary>
public class Target : MonoBehaviour
{
    #region Fields
    [Header("Target Properties")]
    [SerializeField] private Transform _targetCenter;
    [SerializeField] private float _width = 5f;
    [SerializeField] private float _height = 5f;
    [SerializeField] private Vector3 _planeNormal = Vector3.forward;
    [SerializeField] private bool _showGizmos = true;
    [SerializeField] private Color _gizmoColor = new Color(1f, 0.5f, 0f, 0.5f);
    
    [Header("Hit Effects")]
    [SerializeField] private GameObject _perfectHitEffectPrefab;
    [SerializeField] private GameObject _goodHitEffectPrefab;
    [SerializeField] private GameObject _badHitEffectPrefab;
    [SerializeField] private GameObject _hitMarkerPrefab;
    
    [Header("Sounds")]
    [SerializeField] private AudioClip _perfectHitSound;
    [SerializeField] private AudioClip _goodHitSound;
    [SerializeField] private AudioClip _badHitSound;
    
    [Inject] private AudioManager _audioManager;
    
    // Список маркеров попаданий для очистки при перезапуске
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
    /// Нормаль плоскости из настроек
    /// </summary>
    public Vector3 PlaneNormal => _planeNormal.normalized;
    #endregion

    #region Unity Lifecycle
    private void OnValidate()
    {
        if (_targetCenter == null)
        {
            _targetCenter = transform;
        }
        
        // Обновить collider если он есть
        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            boxCollider.size = new Vector3(_width, _height, 0.1f);
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!_showGizmos) return;
        
        Vector3 center = TargetCenter;
        Vector3 normal = PlaneNormal;
        Vector3 right = Vector3.Cross(normal, Vector3.up).normalized * _width * 0.5f;
        Vector3 up = Vector3.Cross(right, normal).normalized * _height * 0.5f;
        
        // Рисуем плоскость цели
        Vector3[] corners = new Vector3[4]
        {
            center - right - up,  // Нижний левый
            center + right - up,  // Нижний правый
            center + right + up,  // Верхний правый
            center - right + up   // Верхний левый
        };
        
        Gizmos.color = _gizmoColor;
        
        // Рисуем плоскость
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
        
        // Рисуем диагонали
        Gizmos.DrawLine(corners[0], corners[2]);
        Gizmos.DrawLine(corners[1], corners[3]);
        
        // Рисуем нормаль
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(center, normal * 2f);
        
        // Рисуем центр
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(center, 0.2f);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Возвращает эффективный радиус мишени (половина диагонали)
    /// </summary>
    public float GetTargetRadius()
    {
        // Используем диагональ прямоугольника как эффективный радиус
        return Mathf.Sqrt(_width * _width + _height * _height) / 2f;
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
        return Mathf.Abs(localPoint.x) <= _width / 2f && 
               Mathf.Abs(localPoint.y) <= _height / 2f &&
               Mathf.Abs(localPoint.z) <= 0.1f; // Небольшой допуск по Z
    }

    /// <summary>
    /// Проверяет, находится ли точка внутри плоскости цели
    /// </summary>
    public bool IsPointInsideTargetPlane(Vector3 point)
    {
        Vector3 center = TargetCenter;
        Vector3 localPoint = point - center;
        
        // Проецируем точку на плоскость цели
        Vector3 projectedPoint = Vector3.ProjectOnPlane(localPoint, PlaneNormal);
        
        // Создаем локальную систему координат для плоскости
        Vector3 forward = PlaneNormal;
        Vector3 right = Vector3.Cross(forward, Vector3.up).normalized;
        Vector3 up = Vector3.Cross(right, forward).normalized;
        
        // Находим координаты в локальной системе
        float xCoord = Vector3.Dot(projectedPoint, right);
        float yCoord = Vector3.Dot(projectedPoint, up);
        
        // Проверяем, находится ли точка внутри прямоугольника мишени
        return Mathf.Abs(xCoord) <= _width / 2f && Mathf.Abs(yCoord) <= _height / 2f;
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

    /// <summary>
    /// Проверяет пересечение луча с плоскостью мишени
    /// </summary>
    public bool RayIntersectsTargetPlane(Vector3 rayOrigin, Vector3 rayDirection, out Vector3 intersectionPoint)
    {
        intersectionPoint = Vector3.zero;
        
        // Плоскость мишени
        Plane targetPlane = new Plane(TargetNormal, TargetCenter);
        
        // Создаем луч
        Ray ray = new Ray(rayOrigin, rayDirection.normalized);
        
        // Проверяем пересечение с плоскостью
        if (targetPlane.Raycast(ray, out float distance))
        {
            // Получаем точку пересечения
            intersectionPoint = ray.GetPoint(distance);
            
            // Проверяем, находится ли точка внутри мишени
            return IsPointInTargetPlane(intersectionPoint);
        }
        
        return false;
    }

    /// <summary>
    /// Регистрирует попадание снаряда в цель
    /// </summary>
    /// <param name="hitPosition">Позиция попадания</param>
    /// <param name="accuracy">Точность попадания (0-100%)</param>
    public void RegisterHit(Vector3 hitPosition, float accuracy)
    {
        // Создаем маркер попадания
        if (_hitMarkerPrefab != null)
        {
            GameObject marker = Instantiate(_hitMarkerPrefab, hitPosition, Quaternion.identity);
            _hitMarkers.Add(marker);
        }
        
        // Выбираем и создаем эффект в зависимости от точности
        GameObject effectPrefab = null;
        AudioClip hitSound = null;
        
        if (accuracy >= 90f)
        {
            // Отличное попадание
            effectPrefab = _perfectHitEffectPrefab;
            hitSound = _perfectHitSound;
        }
        else if (accuracy >= 50f)
        {
            // Хорошее попадание
            effectPrefab = _goodHitEffectPrefab;
            hitSound = _goodHitSound;
        }
        else
        {
            // Плохое попадание
            effectPrefab = _badHitEffectPrefab;
            hitSound = _badHitSound;
        }
        
        // Создаем выбранный эффект
        if (effectPrefab != null)
        {
            Instantiate(effectPrefab, hitPosition, Quaternion.identity);
        }
        
        // Воспроизводим звук попадания через AudioManager
        if (hitSound != null)
        {
            _audioManager.PlaySound(hitSound, hitPosition);
        }
    }
    
    /// <summary>
    /// Очищает все маркеры попаданий
    /// </summary>
    public void ClearHitMarkers()
    {
        foreach (GameObject marker in _hitMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        
        _hitMarkers.Clear();
    }
    #endregion
} 