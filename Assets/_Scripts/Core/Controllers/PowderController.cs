using UnityEngine;
using Zenject;
using UnityEngine.Events;

/// <summary>
/// Контроллер порохового заряда пушки
/// </summary>
public class PowderController : MonoBehaviour
{
    #region Fields
    [Header("Settings")]
    [SerializeField] private float _powderChangeSpeed = 20f;
    [SerializeField] private float _initialPowderPercentage = 50f;

    [Header("Audio")]
    [SerializeField] private AudioClip _powderAdjustSound;
    
    [Inject] private AudioManager _audioManager;

    private float _currentPowderPercentage;
    private bool _isIncreasingPowder;
    private bool _isDecreasingPowder;
    private bool _shouldPlayAdjustSound = false;
    private float _soundCooldown = 0.5f;
    private float _soundTimer = 0f;

    // События для UI
    [HideInInspector] public UnityEvent<float> onPowderChanged = new UnityEvent<float>();
    #endregion

    #region Properties
    public float CurrentPowderPercentage => _currentPowderPercentage;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        InitializePowderControls();
    }

    private void Update()
    {
        HandlePowderAdjustment();
        
        // Управление звуком регулировки пороха
        if (_shouldPlayAdjustSound)
        {
            _soundTimer -= Time.deltaTime;
            if (_soundTimer <= 0)
            {
                // Используем AudioManager для воспроизведения звука
                if (_powderAdjustSound != null)
                {
                    _audioManager.PlaySound(_powderAdjustSound, transform.position);
                }
                _soundTimer = _soundCooldown;
            }
        }
        else
        {
            _soundTimer = 0;
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Устанавливает значение порохового заряда напрямую
    /// </summary>
    public void SetPowderPercentage(float percentage)
    {
        _currentPowderPercentage = Mathf.Clamp(percentage, 0f, 100f);
        NotifyPowderChanged();
    }
    
    // Методы для управления порохом из UI
    public void StartIncreasePowder()
    {
        _isIncreasingPowder = true;
        _shouldPlayAdjustSound = true;
    }

    public void StopIncreasePowder()
    {
        _isIncreasingPowder = false;
        if (!_isDecreasingPowder)
            _shouldPlayAdjustSound = false;
    }
    
    public void StartDecreasePowder()
    {
        _isDecreasingPowder = true;
        _shouldPlayAdjustSound = true;
    }

    public void StopDecreasePowder()
    {
        _isDecreasingPowder = false;
        if (!_isIncreasingPowder)
            _shouldPlayAdjustSound = false;
    }
    
    /// <summary>
    /// Сбрасывает порох на значение по умолчанию
    /// </summary>
    public void ResetPowderToDefault()
    {
        // Сбрасываем значение пороха
        _currentPowderPercentage = _initialPowderPercentage;
        
        // Останавливаем изменение пороха
        _isIncreasingPowder = false;
        _isDecreasingPowder = false;
        
        // Уведомляем об изменении
        NotifyPowderChanged();
    }
    #endregion

    #region Private Methods
    private void InitializePowderControls()
    {
        _currentPowderPercentage = _initialPowderPercentage;
        NotifyPowderChanged();
    }

    private void HandlePowderAdjustment()
    {
        bool needUpdate = false;

        if (_isIncreasingPowder)
        {
            _currentPowderPercentage += _powderChangeSpeed * Time.deltaTime;
            needUpdate = true;
        }
        else if (_isDecreasingPowder)
        {
            _currentPowderPercentage -= _powderChangeSpeed * Time.deltaTime;
            needUpdate = true;
        }

        if (needUpdate)
        {
            _currentPowderPercentage = Mathf.Clamp(_currentPowderPercentage, 0f, 100f);
            NotifyPowderChanged();
        }
    }

    private void NotifyPowderChanged()
    {
        onPowderChanged.Invoke(_currentPowderPercentage);
    }
    #endregion
} 