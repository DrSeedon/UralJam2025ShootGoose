using UnityEngine;
using Zenject;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// Класс, представляющий снаряд пушки
/// </summary>
public class Projectile : MonoBehaviour
{
    #region Fields
    [SerializeField] private float _mass = 5f;
    [SerializeField] private float _radius = 0.15f;
    [SerializeField] private TrailRenderer _trailRenderer;
    [SerializeField] private ParticleSystem _impactEffect;
    [SerializeField] private float _moveSpeed = 20f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip _impactSound;
    [SerializeField] private AudioClip _flyingSound;
    
    [Inject] private AudioManager _audioManager;
    [Inject] private GameStateManager _gameState;
    
    private bool _hasImpacted = false;
    #endregion

    #region Properties
    public float Mass => _mass;
    public float Radius => _radius;
    #endregion
    
    #region Unity Lifecycle
    
    [Inject]
    private void Initialize()
    {
        // Воспроизводим звук полета снаряда
        if (_flyingSound != null)
        {
            _audioManager.PlayLoopedSound(_flyingSound, transform, true);
        }
    }
    
    private void OnDestroy()
    {
        // Останавливаем все звуки при уничтожении объекта
        if (_audioManager != null && _flyingSound != null)
        {
            _audioManager.StopLoopedSound(_flyingSound, transform);
        }
    }
    
    #endregion

    #region Public Methods
    /// <summary>
    /// Вызывается при попадании снаряда в цель
    /// </summary>
    public void OnImpact()
    {
        if (_hasImpacted)
            return;
            
        _hasImpacted = true;
        
        // Воспроизводим эффект удара
        if (_impactEffect != null)
        {
            _impactEffect.Play();
        }
        
        // Воспроизводим звук удара
        if (_impactSound != null)
        {
            _audioManager.PlaySound(_impactSound, transform.position);
            
            // Останавливаем зацикленный звук полета
            if (_flyingSound != null)
            {
                _audioManager.StopLoopedSound(_flyingSound, transform);
            }
        }
        
        // Останавливаем трейл
        if (_trailRenderer != null)
        {
            _trailRenderer.emitting = false;
        }
        
        // Скрываем визуальную модель снаряда, но оставляем объект для эффектов
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        
        // Удаляем объект после воспроизведения эффектов
        Destroy(gameObject, 2f);
    }
    
    /// <summary>
    /// Перемещает снаряд по заданной траектории
    /// </summary>
    public async UniTask MoveAlongTrajectoryAsync(List<Vector3> trajectory, Target target)
    {
        if (trajectory == null || trajectory.Count < 2)
            return;
            
        // Последняя точка траектории - это точка удара
        Vector3 hitPosition = trajectory[trajectory.Count - 1];
        
        // Проверяем, попал ли снаряд в мишень (безопасно проверяем на null)
        bool hitTarget = false;
        Vector3 hitProjected = hitPosition;
        
        if (target != null)
        {
            Vector3 targetCenter = target.TargetCenter;
            
            // Проецируем точку попадания на плоскость мишени для корректного расчета
            hitProjected = Vector3.ProjectOnPlane(hitPosition - targetCenter, target.TargetNormal) + targetCenter;
            
            // Проверяем, находится ли проекция в пределах мишени
            hitTarget = target.IsPointInTargetPlane(hitProjected);
        }
        
        // Время для перемещения между точками
        float timeStep = 0.02f; // 50 FPS
        
        // Перемещаем снаряд по траектории
        for (int i = 0; i < trajectory.Count - 1; i++)
        {
            // Если снаряд уничтожен, выходим
            if (this == null)
                return;
            
            Vector3 currentPoint = trajectory[i];
            Vector3 nextPoint = trajectory[i + 1];
            
            // Анимируем движение от текущей точки к следующей
            float elapsedTime = 0f;
            float duration = Vector3.Distance(currentPoint, nextPoint) / _moveSpeed;
            
            if (duration <= 0)
                continue;
            
            while (elapsedTime < duration)
            {
                // Если снаряд уничтожен, выходим
                if (this == null)
                    return;
                
                float t = elapsedTime / duration;
                transform.position = Vector3.Lerp(currentPoint, nextPoint, t);
                
                // Направление снаряда по траектории
                if (i < trajectory.Count - 2)
                {
                    Vector3 direction = (nextPoint - currentPoint).normalized;
                    if (direction != Vector3.zero)
                    {
                        transform.forward = direction;
                    }
                }
                
                elapsedTime += timeStep;
                await UniTask.Delay((int)(timeStep * 1000));
            }
            
            // Устанавливаем точное положение в конце интерполяции
            if (this != null)
            {
                transform.position = nextPoint;
            }
        }
        
        // Вызываем эффект попадания снаряда
        OnImpact();
        
        // Обновляем расстояние после завершения движения снаряда
        if (target != null)
        {
            Vector3 targetCenter = target.TargetCenter;
            float distanceToCenter = Vector3.Distance(hitProjected, targetCenter);
            if (_gameState != null)
            {
                _gameState.UpdateDistance(distanceToCenter);
            }
        }
        
        // Проверяем попадание в мишень
        if (hitTarget && target != null)
        {
            CheckTargetHit(hitProjected, target);
        }
        else
        {
            // Воспроизводим звук удара о землю
            if (_impactSound != null)
            {
                _audioManager.PlaySound(_impactSound, hitPosition);
            }
        }
    }
    
    private void CheckTargetHit(Vector3 hitPosition, Target target)
    {
        // Вычисляем центр мишени
        Vector3 targetCenter = target.TargetCenter;
        
        // Рассчитываем расстояние до центра мишени
        float distanceToCenter = Vector3.Distance(hitPosition, targetCenter);
        
        // Рассчитываем точность попадания (чем ближе к центру, тем лучше)
        float accuracy = CalculateAccuracy(distanceToCenter, target);
        
        // Обновляем счет в GameStateManager
        if (_gameState != null)
        {
            // Вызываем еще раз явно UpdateDistance для гарантии обновления
            _gameState.UpdateDistance(distanceToCenter);
            // Затем обновляем точность
            _gameState.UpdateAccuracy(accuracy, hitPosition);
        }
        
        // Регистрируем попадание в мишени
        target.RegisterHit(hitPosition, accuracy);
    }
    
    private float CalculateAccuracy(float distanceToCenter, Target target)
    {
        // Получаем радиус мишени
        float targetRadius = target.GetTargetRadius();
        
        // Нормализуем расстояние относительно радиуса (0 - в центре, 1 - на краю)
        float normalizedDistance = Mathf.Clamp01(distanceToCenter / targetRadius);
        
        // Инвертируем и масштабируем до 100%
        return (1f - normalizedDistance) * 100f;
    }
    #endregion
} 