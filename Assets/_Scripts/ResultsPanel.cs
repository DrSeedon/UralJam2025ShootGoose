using Michsky.MUIP;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using Zenject;

/// <summary>
/// Панель отображения результатов игры
/// </summary>
public class ResultsPanel : MonoBehaviour
{
    #region Fields
    [Header("References")]
    public GameObject panel;
    
    [Header("UI References")]
    public TMP_Text _messageText;
    public TMP_Text _scoreText;
    public TMP_Text _highScoreText;
    public ButtonManager _closeButton;
    
    [Header("Settings")]
    [SerializeField] private string _closeButtonText = "Играть снова";
    [SerializeField] private bool _autoHideOnStart = true;
    
    [Inject] private CannonGameManager _gameManager;
    
    public UnityEvent onPanelClosed = new UnityEvent();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        if (_closeButton != null)
        {
            _closeButton.SetText(_closeButtonText);
            _closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }
    
    private void Start()
    {
        if (_autoHideOnStart)
        {
            panel.SetActive(false);
        }
    }
    
    private void OnDestroy()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
        }
        
        onPanelClosed.RemoveAllListeners();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Показывает или скрывает панель
    /// </summary>
    public void ShowPanel(bool value)
    {
        panel.SetActive(value);
    }
    
    /// <summary>
    /// Отображает результаты игры
    /// </summary>
    /// <param name="message">Основное сообщение</param>
    /// <param name="score">Текущий результат</param>
    /// <param name="highScore">Лучший результат</param>
    public void ShowResults(string message, float score, float highScore)
    {
        panel.SetActive(true);
        
        if (_messageText != null)
        {
            _messageText.text = message;
        }
        
        if (_scoreText != null)
        {
            _scoreText.text = $"Ваш результат: {score:F1}%";
        }
        
        if (_highScoreText != null)
        {
            _highScoreText.text = $"Лучший результат: {highScore:F1}%";
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Обработчик кнопки закрытия панели
    /// </summary>
    private void OnCloseButtonClicked()
    {
        // Скрываем панель
        panel.SetActive(false);
        
        // Перезапускаем игру, если найден менеджер
        if (_gameManager != null)
        {
            _gameManager.StartNewGame();
        }
        
        // Вызываем событие закрытия панели
        onPanelClosed.Invoke();
    }
    #endregion
} 