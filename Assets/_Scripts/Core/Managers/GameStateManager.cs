using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

// Отвечает только за управление состоянием игры
public class GameStateManager : MonoBehaviour
{
    [Inject] private ResultsPanel _resultsPanel;
    
    [SerializeField] private int _maxProjectiles = 5;
    
    // События жизненного цикла игры
    public UnityEvent onGameStarted = new UnityEvent();
    public UnityEvent onGameOver = new UnityEvent();
    public UnityEvent onRestartGame = new UnityEvent();
    
    // Счётчики и состояния
    private int _projectilesRemaining;
    private float _bestAccuracy = 0f;
    private float _highScore = 0f;
    private bool _isFiring = false;
    
    // События для UI
    public UnityEvent<int> onProjectileCountChanged = new UnityEvent<int>();
    public UnityEvent<float> onAccuracyCalculated = new UnityEvent<float>();
    public UnityEvent<float> onDistanceCalculated = new UnityEvent<float>();
    public UnityEvent<float> onHighScoreUpdated = new UnityEvent<float>();
    
    #region Properties
    public float BestAccuracy 
    { 
        get => _bestAccuracy; 
        set
        {
            _bestAccuracy = value;
            onAccuracyCalculated.Invoke(_bestAccuracy);
        }
    }
    
    public bool CanFire => ProjectilesRemaining > 0 && !IsFiring;
    
    public bool IsFiring
    {
        get => _isFiring;
        set => _isFiring = value;
    }
    
    public int ProjectilesRemaining
    {
        get => _projectilesRemaining;
        set
        {
            _projectilesRemaining = value;
            onProjectileCountChanged.Invoke(_projectilesRemaining);
        }
    }
    
    public float HighScore
    {
        get => _highScore;
        set
        {
            _highScore = value;
            onHighScoreUpdated.Invoke(_highScore);
            
            // Сохраняем рекорд
            PlayerPrefs.SetFloat("HighScore", _highScore);
            PlayerPrefs.Save();
        }
    }
    #endregion
    
    private void Awake()
    {
        // Загружаем рекорд точности
        _highScore = PlayerPrefs.GetFloat("HighScore", 0f);
        
    }
    
    private void Start()
    {
        // Подписываемся на события
        _resultsPanel.onPanelClosed.AddListener(() => onRestartGame?.Invoke());
        onRestartGame.AddListener(RestartGame);
        
        InitializeGame();
        
        // Обновляем UI сразу при старте
        onHighScoreUpdated.Invoke(_highScore);
        onDistanceCalculated.Invoke(0f);
        onAccuracyCalculated.Invoke(0f);
        
        Invoke(nameof(RestartGame), 0.1f);
    }
    
    public void RestartGame()
    {
        InitializeGame();
        BestAccuracy = 0f;
        _resultsPanel.ShowPanel(false);
    }
    
    private void InitializeGame()
    {
        ProjectilesRemaining = _maxProjectiles;
    }
    
    public void CheckGameOver()
    {
        if (ProjectilesRemaining <= 0)
        {
            // Показываем результаты с небольшой задержкой
            ShowResultsAfterDelayAsync().Forget();
        }
    }
    
    // Метод для обновления результатов при попадании
    public void UpdateAccuracy(float accuracy, Vector3 hitPosition)
    {
        // Вызываем событие с текущей точностью попадания
        onAccuracyCalculated.Invoke(accuracy);
        
        // Обновляем лучший результат в этой игре
        if (accuracy > BestAccuracy)
        {
            BestAccuracy = accuracy;
        }
        
        // Обновляем рекорд, если текущая точность лучше
        if (accuracy > HighScore)
        {
            HighScore = accuracy;
        }
    }
    
    // Метод для обновления и уведомления о расстоянии до цели
    public void UpdateDistance(float distance)
    {
        onDistanceCalculated.Invoke(distance);
    }
    
    private async UniTask ShowResultsAfterDelayAsync()
    {
        // Ждем немного перед показом результатов
        await UniTask.Delay(1500);
        
        // Показываем результаты
        ShowResults("Все снаряды использованы!");
    }
    
    public void ShowResults(string message)
    {
        _resultsPanel.ShowResults(message, BestAccuracy, HighScore);
    }
}