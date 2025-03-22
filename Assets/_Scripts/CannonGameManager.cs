using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Zenject;
using Cysharp.Threading.Tasks;

/// <summary>
/// Главный менеджер игры, координирующий все системы и компоненты
/// </summary>
public class CannonGameManager : MonoBehaviour
{
    #region Fields

    [Header("References")]
    
    [Inject] private CannonController _cannonController;
    [Inject] private PowderController _powderController;
    [Inject] private BallisticsCalculator _ballisticsCalculator;
    [Inject] private Target _target;
    [Inject] private ResultsPanel _resultsPanel;
    [Inject] private AudioManager _audioManager;

    [SerializeField] private Transform _projectileSpawnPoint;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private Transform _cannonBarrel;

    [Header("Game Settings")] [SerializeField]
    private int _maxProjectiles = 5;

    [SerializeField] private float _fireDelay = 0.5f;
    [SerializeField] private float _projectileSpeed = 20f;

    [Header("Trajectory Visualization")] [SerializeField]
    private bool _showTrajectoryPreview = true;

    [SerializeField] private LineRenderer _trajectoryLineRenderer;
    [SerializeField] private int _trajectoryPreviewResolution = 20;
    [SerializeField] private float _trajectoryPreviewUpdateRate = 0.2f;

    [Header("Effects")] [SerializeField] private ParticleSystem _muzzleFlashEffect;
    [SerializeField] private ParticleSystem _explosionEffect;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _fireSound;
    [SerializeField] private AudioClip _explosionSound;
    [SerializeField] private AudioClip _impactSound;

    private int _projectilesRemaining;
    private bool _canFire = true;
    private bool _gameOver = false;
    private float _bestAccuracy = 0f;
    private float _trajectoryPreviewTimer = 0f;
    private float _highScore = 0f; // Рекорд точности за все время игры
    private bool _isFiring = false;

    // События для UI
    public UnityEvent<int> onProjectileCountChanged = new UnityEvent<int>();
    public UnityEvent<float> onAccuracyCalculated = new UnityEvent<float>();
    public UnityEvent<float> onDistanceCalculated = new UnityEvent<float>();
    public UnityEvent<float> onHighScoreUpdated = new UnityEvent<float>();
    public UnityEvent<float, float> onAngleChanged = new UnityEvent<float, float>();
    public UnityEvent<float> onPowderChanged = new UnityEvent<float>();
    public UnityEvent onGameOver = new UnityEvent();

    #endregion

    #region Properties
    public int ProjectilesRemaining => _projectilesRemaining;
    public float HighScore => _highScore;
    public float BestAccuracy => _bestAccuracy;
    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        // Загружаем рекорд точности из PlayerPrefs, если он есть
        _highScore = PlayerPrefs.GetFloat("HighScore", 0f);
        NotifyHighScoreUpdated();

        InitializeGame();
        SetupTrajectoryPreview();
        SubscribeToEvents();
    }

    private void Update()
    {
        if (_showTrajectoryPreview && !_gameOver)
        {
            UpdateTrajectoryPreview();
        }
    }

    private void OnDisable()
    {
        CleanupEvents();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Запускает новую игру
    /// </summary>
    public void StartNewGame()
    {
        InitializeGame();
        _gameOver = false;
        _bestAccuracy = 0f;
        _canFire = true; // Сбрасываем флаг, позволяя снова стрелять

        _resultsPanel.ShowPanel(false);

        // Возвращаем ствол пушки в видимое состояние (на случай, если была взрыв)
        if (_cannonBarrel != null)
        {
            _cannonBarrel.gameObject.SetActive(true);
        }
        
        // Включаем предпросмотр траектории, если он должен быть включен
        if (_trajectoryLineRenderer != null)
        {
            _trajectoryLineRenderer.enabled = _showTrajectoryPreview;
        }
        
        // Сбрасываем углы пушки в исходное положение
        if (_cannonController != null)
        {
            _cannonController.ResetToInitialAngles();
        }
        
        // Сбрасываем процент пороха на начальное значение
        if (_powderController != null)
        {
            _powderController.ResetPowderToDefault();
        }
    }

    /// <summary>
    /// Обрабатывает нажатие на кнопку выстрела
    /// </summary>
    public void FireCannon()
    {
        if (!_canFire || _gameOver || _projectilesRemaining <= 0 || _isFiring)
            return;

        _canFire = false;
        _isFiring = true;
        FireProjectileAsync().Forget();
    }

    #endregion

    #region Private Methods

    private void InitializeGame()
    {
        // Сначала очищаем все существующие обработчики событий
        CleanupEvents();
        
        _projectilesRemaining = _maxProjectiles;
        NotifyProjectileCountChanged();
    }

    private void SubscribeToEvents()
    {
        // Подписываемся на события от других компонентов
        if (_cannonController != null)
        {
            _cannonController.onAngleChanged.AddListener(OnCannonAngleChanged);
        }
        
        if (_powderController != null)
        {
            _powderController.onPowderChanged.AddListener(OnPowderChanged);
        }
    }

    private void OnCannonAngleChanged(float horizontalAngle, float verticalAngle)
    {
        // Передаем изменение угла в UI через событие
        onAngleChanged.Invoke(horizontalAngle, verticalAngle);
    }

    private void OnPowderChanged(float powderPercentage)
    {
        // Передаем изменение процента пороха в UI через событие
        onPowderChanged.Invoke(powderPercentage);
    }

    private void SetupTrajectoryPreview()
    {
        if (_trajectoryLineRenderer == null && _showTrajectoryPreview)
        {
            // Создаем LineRenderer, если его нет
            _trajectoryLineRenderer = gameObject.AddComponent<LineRenderer>();
            _trajectoryLineRenderer.startWidth = 0.1f;
            _trajectoryLineRenderer.endWidth = 0.05f;
            _trajectoryLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _trajectoryLineRenderer.startColor = new Color(1f, 1f, 0f, 0.3f); // Полупрозрачный желтый
            _trajectoryLineRenderer.endColor = new Color(1f, 0.5f, 0f, 0.1f); // Полупрозрачный оранжевый
            _trajectoryLineRenderer.positionCount = 0;
        }

        if (_trajectoryLineRenderer != null)
        {
            _trajectoryLineRenderer.enabled = _showTrajectoryPreview;
        }
    }

    private void UpdateTrajectoryPreview()
    {
        _trajectoryPreviewTimer -= Time.deltaTime;

        if (_trajectoryPreviewTimer <= 0)
        {
            _trajectoryPreviewTimer = _trajectoryPreviewUpdateRate;

            if (_trajectoryLineRenderer != null)
            {
                // Получаем текущие параметры стрельбы
                float powderPercentage = _powderController.CurrentPowderPercentage;
                Vector3 direction = _projectileSpawnPoint.forward;

                // Если порох равен 0, то ставим очень короткую траекторию (снаряд просто падает)
                if (powderPercentage <= 0.1f)
                {
                    List<Vector3> shortTrajectory = new List<Vector3>();
                    shortTrajectory.Add(_projectileSpawnPoint.position);
                    shortTrajectory.Add(_projectileSpawnPoint.position + _projectileSpawnPoint.forward * 0.5f -
                                        Vector3.up * 2f);

                    _trajectoryLineRenderer.positionCount = shortTrajectory.Count;
                    _trajectoryLineRenderer.SetPositions(shortTrajectory.ToArray());
                    return;
                }

                // Рассчитываем траекторию
                List<Vector3> trajectory = _ballisticsCalculator.CalculateTrajectory(
                    _projectileSpawnPoint.position,
                    direction,
                    powderPercentage
                );

                // Упрощаем траекторию, если она слишком детализирована
                if (trajectory.Count > _trajectoryPreviewResolution)
                {
                    float step = (float)trajectory.Count / _trajectoryPreviewResolution;
                    List<Vector3> simplifiedTrajectory = new List<Vector3>();

                    for (int i = 0; i < _trajectoryPreviewResolution; i++)
                    {
                        int index = Mathf.Min(Mathf.FloorToInt(i * step), trajectory.Count - 1);
                        simplifiedTrajectory.Add(trajectory[index]);
                    }

                    // Добавляем последнюю точку
                    simplifiedTrajectory.Add(trajectory[trajectory.Count - 1]);
                    trajectory = simplifiedTrajectory;
                }

                // Применяем к LineRenderer
                _trajectoryLineRenderer.positionCount = trajectory.Count;
                _trajectoryLineRenderer.SetPositions(trajectory.ToArray());
            }
        }
    }

    private void NotifyHighScoreUpdated()
    {
        onHighScoreUpdated.Invoke(_highScore);
    }

    private void CleanupEvents()
    {
        // Отписываемся от всех событий
        onProjectileCountChanged.RemoveAllListeners();
        onAccuracyCalculated.RemoveAllListeners();
        onGameOver.RemoveAllListeners();
        onDistanceCalculated.RemoveAllListeners();
        onHighScoreUpdated.RemoveAllListeners();
        onAngleChanged.RemoveAllListeners();
        onPowderChanged.RemoveAllListeners();
        
        // Отписываемся от событий других компонентов
        if (_cannonController != null)
        {
            _cannonController.onAngleChanged.RemoveAllListeners();
        }
        
        if (_powderController != null)
        {
            _powderController.onPowderChanged.RemoveAllListeners();
        }
    }

    private async UniTask FireProjectileAsync()
    {
        if (_gameOver || _projectilesRemaining <= 0)
        {
            _canFire = true;
            _isFiring = false;
            return;
        }

        // Уменьшаем количество снарядов
        _projectilesRemaining--;
        NotifyProjectileCountChanged();
        
        // Событие изменения количества снарядов
        onProjectileCountChanged.Invoke(_projectilesRemaining);

        // Воспроизводим эффекты выстрела
        PlayMuzzleEffect();
        
        // Воспроизводим звук выстрела через AudioManager
        if (_fireSound != null)
        {
            _audioManager.PlaySound(_fireSound, _projectileSpawnPoint.position);
        }

        // Создаем снаряд
        GameObject projectile = Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
        
        // Получаем текущие параметры стрельбы
        float powderPercentage = _powderController.CurrentPowderPercentage;
        
        // Проверяем безопасность заряда пороха
        bool isSafeCharge = _ballisticsCalculator.IsPowderSafe(powderPercentage);
        
        // Если заряд небезопасен, есть шанс взрыва пушки
        if (!isSafeCharge && Random.value > 0.5f)
        {
            // Взрыв пушки из-за перезаряда
            await CannonExplosionAsync();
            Destroy(projectile); // Уничтожаем снаряд, так как пушка взорвалась
            return;
        }
        
        // Рассчитываем траекторию
        List<Vector3> trajectory = _ballisticsCalculator.CalculateTrajectory(
            _projectileSpawnPoint.position,
            _projectileSpawnPoint.forward,
            powderPercentage
        );
        
        // Если траектория пустая (например, слишком мало пороха)
        if (trajectory.Count < 2)
        {
            _canFire = true;
            _isFiring = false;
            Destroy(projectile);
            return;
        }
        
        // Задержка после выстрела
        await UniTask.Delay((int)(_fireDelay * 1000));
        
        // Отключаем предпросмотр траектории на время полета снаряда
        if (_trajectoryLineRenderer != null)
        {
            _trajectoryLineRenderer.enabled = false;
        }
        
        // Запускаем движение снаряда по траектории
        await MoveProjectileAlongTrajectoryAsync(projectile, trajectory);
        
        // Включаем предпросмотр траектории обратно
        if (_trajectoryLineRenderer != null && _showTrajectoryPreview && !_gameOver)
        {
            _trajectoryLineRenderer.enabled = true;
        }
        
        _canFire = true;
        _isFiring = false;
        
        // Проверяем, не закончилась ли игра
        CheckGameOver();
    }

    private async UniTask MoveProjectileAlongTrajectoryAsync(GameObject projectile, List<Vector3> trajectory)
    {
        // Получаем компонент снаряда, если он есть
        Projectile projectileComponent = projectile.GetComponent<Projectile>();
        
        // Последняя точка траектории - это точка удара
        Vector3 hitPosition = trajectory[trajectory.Count - 1];
        
        // Проверяем, попал ли снаряд в мишень
        bool hitTarget = _target.IsPointInsideTargetPlane(hitPosition);
        
        // Время для перемещения между точками
        float timeStep = 0.02f; // 50 FPS
        
        // Перемещаем снаряд по траектории
        for (int i = 0; i < trajectory.Count - 1; i++)
        {
            // Если снаряд уничтожен, выходим
            if (projectile == null)
                return;
            
            Vector3 currentPoint = trajectory[i];
            Vector3 nextPoint = trajectory[i + 1];
            
            // Анимируем движение от текущей точки к следующей
            float elapsedTime = 0f;
            float duration = Vector3.Distance(currentPoint, nextPoint) / _projectileSpeed;
            
            if (duration <= 0)
                continue;
            
            while (elapsedTime < duration)
            {
                // Если снаряд уничтожен, выходим
                if (projectile == null)
                    return;
                
                float t = elapsedTime / duration;
                projectile.transform.position = Vector3.Lerp(currentPoint, nextPoint, t);
                
                // Направление снаряда по траектории
                if (i < trajectory.Count - 2)
                {
                    Vector3 direction = (nextPoint - currentPoint).normalized;
                    if (direction != Vector3.zero)
                    {
                        projectile.transform.forward = direction;
                    }
                }
                
                elapsedTime += timeStep;
                await UniTask.Delay((int)(timeStep * 1000));
            }
            
            // Устанавливаем точное положение в конце интерполяции
            if (projectile != null)
            {
                projectile.transform.position = nextPoint;
            }
        }
        
        // Вызываем эффект попадания снаряда
        if (projectileComponent != null)
        {
            projectileComponent.OnImpact();
        }
        
        // Проверяем попадание в мишень
        if (hitTarget)
        {
            CheckTargetHit(hitPosition);
        }
        else
        {
            // Воспроизводим звук удара о землю через AudioManager
            if (_impactSound != null)
            {
                _audioManager.PlaySound(_impactSound, hitPosition);
            }
        }
    }

    private void CheckTargetHit(Vector3 hitPosition)
    {
        // Вычисляем центр мишени
        Vector3 targetCenter = _target.TargetCenter;
        
        // Проецируем точку попадания на плоскость мишени, если она еще не на ней
        Vector3 hitProjected = Vector3.ProjectOnPlane(hitPosition - targetCenter, _target.PlaneNormal) + targetCenter;
        
        // Рассчитываем расстояние до центра мишени
        float distanceToCenter = Vector3.Distance(hitProjected, targetCenter);
        
        // Рассчитываем точность попадания (чем ближе к центру, тем лучше)
        float accuracy = CalculateAccuracy(distanceToCenter);
        
        // Обновляем лучший результат в этой игре
        _bestAccuracy = Mathf.Max(_bestAccuracy, accuracy);
        
        // Обновляем рекорд, если текущая точность лучше
        if (accuracy > _highScore)
        {
            _highScore = accuracy;
            PlayerPrefs.SetFloat("HighScore", _highScore);
            PlayerPrefs.Save();
            NotifyHighScoreUpdated();
        }
        
        // Отправляем события с информацией о попадании
        onDistanceCalculated.Invoke(distanceToCenter);
        onAccuracyCalculated.Invoke(accuracy);
        
        // Регистрируем попадание в мишени
        _target.RegisterHit(hitProjected, accuracy);
    }

    private float CalculateAccuracy(float distanceToCenter)
    {
        // Получаем радиус мишени
        float targetRadius = _target.GetTargetRadius();
        
        // Нормализуем расстояние относительно радиуса (0 - в центре, 1 - на краю)
        float normalizedDistance = Mathf.Clamp01(distanceToCenter / targetRadius);
        
        // Инвертируем и масштабируем до 100%
        return (1f - normalizedDistance) * 100f;
    }

    private async UniTask CannonExplosionAsync()
    {
        // Скрываем ствол пушки
        if (_cannonBarrel != null)
        {
            _cannonBarrel.gameObject.SetActive(false);
        }
        
        // Воспроизводим взрыв
        if (_explosionEffect != null)
        {
            _explosionEffect.transform.position = _projectileSpawnPoint.position;
            _explosionEffect.Play();
        }
        
        // Воспроизводим звук взрыва через AudioManager
        if (_explosionSound != null)
        {
            _audioManager.PlaySound(_explosionSound, _projectileSpawnPoint.position);
        }
        
        // Задержка после взрыва
        await UniTask.Delay(1000);
        
        // Показываем результаты
        ShowResults("Перезаряд привел к взрыву пушки!");
        
        // Игра окончена
        _gameOver = true;
        
        // Вызываем событие окончания игры
        onGameOver.Invoke();
    }

    private void CheckGameOver()
    {
        if (_projectilesRemaining <= 0 && !_gameOver)
        {
            _gameOver = true;
            
            // Показываем результаты с небольшой задержкой
            ShowResultsAfterDelayAsync().Forget();
            
            // Вызываем событие окончания игры
            onGameOver.Invoke();
        }
    }

    private async UniTask ShowResultsAfterDelayAsync()
    {
        // Ждем немного перед показом результатов
        await UniTask.Delay(1500);
        
        // Показываем результаты
        ShowResults("Все снаряды использованы!");
    }

    private void ShowResults(string message)
    {
        _resultsPanel.ShowResults(message, _bestAccuracy, _highScore);
    }

    private void NotifyProjectileCountChanged()
    {
        onProjectileCountChanged.Invoke(_projectilesRemaining);
    }

    private void PlayMuzzleEffect()
    {
        if (_muzzleFlashEffect != null)
        {
            _muzzleFlashEffect.Stop();
            _muzzleFlashEffect.Play();
        }
    }

    #endregion
}