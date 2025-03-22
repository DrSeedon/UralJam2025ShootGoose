using UnityEngine;
using Zenject;
using UnityEngine.Events;

/// <summary>
/// Контроллер пушки, отвечающий за горизонтальное и вертикальное вращение
/// </summary>
public class CannonController : MonoBehaviour
{
    #region Fields
    [Header("Rotation Settings")]
    [SerializeField] private float _horizontalRotationSpeed = 20f;
    [SerializeField] private float _verticalRotationSpeed = 15f;
    [SerializeField] private float _maxHorizontalAngle = 180f;
    [SerializeField] private float _minHorizontalAngle = -180f;
    [SerializeField] private float _maxVerticalAngle = 45f;
    [SerializeField] private float _minVerticalAngle = 0f;

    [Header("References")]
    [SerializeField] private Transform _cannonBase; // Горизонтальное вращение
    [SerializeField] private Transform _cannonBarrel; // Вертикальное вращение

    [Header("Audio")]
    [SerializeField] private AudioClip _rotationSound;
    
    [Inject] private AudioManager _audioManager;

    // Начальные углы для сохранения исходной позиции
    private float _initialHorizontalAngle = 0f;
    private float _initialVerticalAngle = 0f;
    private float _currentHorizontalAngle = 0f;
    private float _currentVerticalAngle = 0f;
    private bool _isRotatingLeft = false;
    private bool _isRotatingRight = false;
    private bool _isRotatingUp = false;
    private bool _isRotatingDown = false;
    
    private bool _shouldPlayRotationSound = false;
    private float _soundCooldown = 0.5f;
    private float _soundTimer = 0f;

    // События для UI
    [HideInInspector] public UnityEvent<float, float> onAngleChanged = new UnityEvent<float, float>();
    #endregion

    #region Properties
    public float CurrentHorizontalAngle => _currentHorizontalAngle;
    public float CurrentVerticalAngle => _currentVerticalAngle;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Сохраняем начальные углы поворота из сцены
        InitializeStartingAngles();
    }
    
    private void Update()
    {
        HandleRotation();
        
        // Управление звуком вращения
        if (_shouldPlayRotationSound)
        {
            _soundTimer -= Time.deltaTime;
            if (_soundTimer <= 0)
            {
                // Используем AudioManager для воспроизведения звука
                if (_rotationSound != null)
                {
                    _audioManager.PlaySound(_rotationSound, transform.position);
                }
                _soundTimer = _soundCooldown;
            }
        }
        else
        {
            _soundTimer = 0;
        }
    }
    
    private void OnDestroy()
    {
        onAngleChanged.RemoveAllListeners();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Устанавливает углы пушки напрямую
    /// </summary>
    public void SetCannonAngles(float horizontalAngle, float verticalAngle)
    {
        _currentHorizontalAngle = Mathf.Clamp(horizontalAngle, _minHorizontalAngle, _maxHorizontalAngle);
        _currentVerticalAngle = Mathf.Clamp(verticalAngle, _minVerticalAngle, _maxVerticalAngle);
        
        ApplyRotation();
        NotifyAngleChanged();
    }
    
    /// <summary>
    /// Сбрасывает углы пушки к начальным значениям
    /// </summary>
    public void ResetToInitialAngles()
    {
        _currentHorizontalAngle = 0f; // Нулевое значение теперь соответствует начальному повороту
        _currentVerticalAngle = 0f;   // Нулевое значение теперь соответствует начальному повороту
        
        ApplyRotation();
        NotifyAngleChanged();
        
        // Сбрасываем флаги вращения
        _isRotatingLeft = false;
        _isRotatingRight = false;
        _isRotatingUp = false;
        _isRotatingDown = false;
    }
    
    // Методы для управления вращением из UI
    public void StartRotateLeft() 
    { 
        _isRotatingLeft = true; 
        _shouldPlayRotationSound = true;
    }
    
    public void StopRotateLeft() 
    { 
        _isRotatingLeft = false; 
        if (!_isRotatingRight && !_isRotatingUp && !_isRotatingDown)
            _shouldPlayRotationSound = false;
    }
    
    public void StartRotateRight() 
    { 
        _isRotatingRight = true; 
        _shouldPlayRotationSound = true;
    }
    
    public void StopRotateRight() 
    { 
        _isRotatingRight = false; 
        if (!_isRotatingLeft && !_isRotatingUp && !_isRotatingDown)
            _shouldPlayRotationSound = false;
    }
    
    public void StartRotateUp() 
    { 
        _isRotatingUp = true; 
        _shouldPlayRotationSound = true;
    }
    
    public void StopRotateUp() 
    { 
        _isRotatingUp = false; 
        if (!_isRotatingLeft && !_isRotatingRight && !_isRotatingDown)
            _shouldPlayRotationSound = false;
    }
    
    public void StartRotateDown() 
    { 
        _isRotatingDown = true; 
        _shouldPlayRotationSound = true;
    }
    
    public void StopRotateDown() 
    { 
        _isRotatingDown = false; 
        if (!_isRotatingLeft && !_isRotatingRight && !_isRotatingUp)
            _shouldPlayRotationSound = false;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Инициализирует начальные углы на основе фактического положения пушки в сцене
    /// </summary>
    private void InitializeStartingAngles()
    {
        if (_cannonBase != null)
        {
            // Получаем текущий угол поворота основания
            _initialHorizontalAngle = _cannonBase.localRotation.eulerAngles.y;
            // Нормализуем угол в диапазоне от -180 до 180 градусов
            if (_initialHorizontalAngle > 180f)
                _initialHorizontalAngle -= 360f;
        }
        
        if (_cannonBarrel != null)
        {
            // Получаем текущий угол наклона ствола (отрицательный, потому что это X-поворот)
            _initialVerticalAngle = -_cannonBarrel.localRotation.eulerAngles.x;
            // Нормализуем угол
            if (_initialVerticalAngle > 180f)
                _initialVerticalAngle -= 360f;
            else if (_initialVerticalAngle < -180f)
                _initialVerticalAngle += 360f;
            
            // Берем абсолютное значение, так как для удобства работаем с положительными углами возвышения
            _initialVerticalAngle = Mathf.Abs(_initialVerticalAngle);
        }
        
        // Устанавливаем текущие углы равными начальным (т.е. начинаем с нуля)
        _currentHorizontalAngle = 0f;
        _currentVerticalAngle = 0f;
    }

    private void HandleRotation()
    {
        bool needUpdate = false;

        if (_isRotatingLeft)
        {
            _currentHorizontalAngle -= _horizontalRotationSpeed * Time.deltaTime;
            needUpdate = true;
        }
        else if (_isRotatingRight)
        {
            _currentHorizontalAngle += _horizontalRotationSpeed * Time.deltaTime;
            needUpdate = true;
        }

        if (_isRotatingUp)
        {
            _currentVerticalAngle += _verticalRotationSpeed * Time.deltaTime;
            needUpdate = true;
        }
        else if (_isRotatingDown)
        {
            _currentVerticalAngle -= _verticalRotationSpeed * Time.deltaTime;
            needUpdate = true;
        }

        if (needUpdate)
        {
            _currentHorizontalAngle = Mathf.Clamp(_currentHorizontalAngle, _minHorizontalAngle, _maxHorizontalAngle);
            _currentVerticalAngle = Mathf.Clamp(_currentVerticalAngle, _minVerticalAngle, _maxVerticalAngle);
            
            ApplyRotation();
            NotifyAngleChanged();
        }
    }

    private void ApplyRotation()
    {
        if (_cannonBase != null)
        {
            // Используем начальный угол как базовый, к которому прибавляем текущий угол отклонения
            float finalHorizontalAngle = _initialHorizontalAngle + _currentHorizontalAngle;
            _cannonBase.localRotation = Quaternion.Euler(0, finalHorizontalAngle, 0);
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] Основание пушки не назначено");
        }

        if (_cannonBarrel != null)
        {
            // Используем начальный угол как базовый
            float finalVerticalAngle = _initialVerticalAngle + _currentVerticalAngle;
            _cannonBarrel.localRotation = Quaternion.Euler(-finalVerticalAngle, 0, 0);
        }
        else
        {
            Debug.LogError($"[{GetType().Name}] Ствол пушки не назначен");
        }
    }

    private void NotifyAngleChanged()
    {
        onAngleChanged.Invoke(_currentHorizontalAngle, _currentVerticalAngle);
    }
    #endregion
} 