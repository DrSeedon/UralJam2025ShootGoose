using UnityEngine;
using Zenject;

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
    
    [Header("Audio")]
    [SerializeField] private AudioClip _impactSound;
    [SerializeField] private AudioClip _flyingSound;
    
    [Inject] private AudioManager _audioManager;
    
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
    #endregion
} 