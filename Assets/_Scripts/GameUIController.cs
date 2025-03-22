using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Zenject;
using System.Collections.Generic;

/// <summary>
/// Центральный контроллер UI для всей игры
/// Обрабатывает все взаимодействия с UI элементами и коммуницирует с геймплей-компонентами
/// </summary>
public class GameUIController : MonoBehaviour
{
    #region Fields

    [Header("Cannon Control UI")]
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _upButton;
    [SerializeField] private Button _downButton;
    [SerializeField] private TMP_Text _horizontalAngleText;
    [SerializeField] private TMP_Text _verticalAngleText;

    [Header("Powder Control UI")]
    [SerializeField] private Button _increasePowderButton;
    [SerializeField] private Button _decreasePowderButton;
    [SerializeField] private Slider _powderSlider;
    [SerializeField] private TMP_Text _powderPercentageText;

    [Header("Game State UI")]
    [SerializeField] private Button _fireButton;
    [SerializeField] private TMP_Text _projectilesRemainingText;
    [SerializeField] private TMP_Text _distanceText;
    [SerializeField] private TMP_Text _accuracyText;
    [SerializeField] private TMP_Text _highScoreText;

    [Header("Dependencies")]
    [Inject] private CannonController _cannonController;
    [Inject] private PowderController _powderController;
    [Inject] private CannonGameManager _gameManager;

    // Словарь для хранения ссылок на EventTrigger для каждой кнопки
    private Dictionary<Button, EventTrigger> _buttonEventTriggers = new Dictionary<Button, EventTrigger>();

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        SetupButtonListeners();
        InitializeUIValues();
        SubscribeToEvents();
    }
    
    #endregion

    #region Initialization

    private void InitializeUIValues()
    {
        // Инициализация значений UI элементов
        UpdatePowderUI(_powderController.CurrentPowderPercentage);
        UpdateAnglesUI(0, 0); // Начальные углы
        UpdateProjectileCountUI(_gameManager.ProjectilesRemaining);
        UpdateHighScoreUI(_gameManager.HighScore);
        
        // Инициализируем слайдер, если он есть
        if (_powderSlider != null)
        {
            _powderSlider.onValueChanged.AddListener(OnPowderSliderChanged);
        }
    }

    private void SetupButtonListeners()
    {
        // Настройка кнопок управления пушкой
        if (_leftButton != null)
        {
            AddButtonEvents(_leftButton, OnLeftButtonDown, OnLeftButtonUp);
        }

        if (_rightButton != null)
        {
            AddButtonEvents(_rightButton, OnRightButtonDown, OnRightButtonUp);
        }

        if (_upButton != null)
        {
            AddButtonEvents(_upButton, OnUpButtonDown, OnUpButtonUp);
        }

        if (_downButton != null)
        {
            AddButtonEvents(_downButton, OnDownButtonDown, OnDownButtonUp);
        }

        // Настройка кнопок управления порохом
        if (_increasePowderButton != null)
        {
            AddButtonEvents(_increasePowderButton, OnIncreasePowderDown, OnIncreasePowderUp);
        }

        if (_decreasePowderButton != null)
        {
            AddButtonEvents(_decreasePowderButton, OnDecreasePowderDown, OnDecreasePowderUp);
        }

        // Настройка кнопки выстрела
        if (_fireButton != null)
        {
            _fireButton.onClick.AddListener(OnFireButtonClicked);
        }
    }

    private void SubscribeToEvents()
    {
        // Подписываемся на события CannonGameManager
        _gameManager.onProjectileCountChanged.AddListener(UpdateProjectileCountUI);
        _gameManager.onAccuracyCalculated.AddListener(UpdateAccuracyUI);
        _gameManager.onGameOver.AddListener(OnGameOver);
        _gameManager.onAngleChanged.AddListener(UpdateAnglesUI);
        _gameManager.onPowderChanged.AddListener(UpdatePowderUI);
        _gameManager.onDistanceCalculated.AddListener(UpdateDistanceUI);
        _gameManager.onHighScoreUpdated.AddListener(UpdateHighScoreUI);
    }
    #endregion

    #region Button Event Handlers

    // Обработчики для кнопок поворота пушки
    private void OnLeftButtonDown(BaseEventData eventData)
    {
        _cannonController.StartRotateLeft();
    }

    private void OnLeftButtonUp(BaseEventData eventData)
    {
        _cannonController.StopRotateLeft();
    }

    private void OnRightButtonDown(BaseEventData eventData)
    {
        _cannonController.StartRotateRight();
    }

    private void OnRightButtonUp(BaseEventData eventData)
    {
        _cannonController.StopRotateRight();
    }

    private void OnUpButtonDown(BaseEventData eventData)
    {
        _cannonController.StartRotateUp();
    }

    private void OnUpButtonUp(BaseEventData eventData)
    {
        _cannonController.StopRotateUp();
    }

    private void OnDownButtonDown(BaseEventData eventData)
    {
        _cannonController.StartRotateDown();
    }

    private void OnDownButtonUp(BaseEventData eventData)
    {
        _cannonController.StopRotateDown();
    }

    // Обработчики для кнопок управления порохом
    private void OnIncreasePowderDown(BaseEventData eventData)
    {
        _powderController.StartIncreasePowder();
    }

    private void OnIncreasePowderUp(BaseEventData eventData)
    {
        _powderController.StopIncreasePowder();
    }

    private void OnDecreasePowderDown(BaseEventData eventData)
    {
        _powderController.StartDecreasePowder();
    }

    private void OnDecreasePowderUp(BaseEventData eventData)
    {
        _powderController.StopDecreasePowder();
    }

    // Обработчик для слайдера пороха
    private void OnPowderSliderChanged(float value)
    {
        _powderController.SetPowderPercentage(value);
    }

    // Обработчик для кнопки выстрела
    private void OnFireButtonClicked()
    {
        _gameManager.FireCannon();
    }

    #endregion

    #region UI Update Methods

    // Обновление счётчика снарядов
    private void UpdateProjectileCountUI(int projectileCount)
    {
        if (_projectilesRemainingText != null)
        {
            _projectilesRemainingText.text = $"Снарядов: {projectileCount}";
        }
    }

    // Обновление отображения точности
    private void UpdateAccuracyUI(float accuracy)
    {
        if (_accuracyText != null)
        {
            _accuracyText.text = $"Точность: {accuracy:F1}%";
        }
    }

    // Обновление отображения расстояния до цели
    private void UpdateDistanceUI(float distance)
    {
        if (_distanceText != null)
        {
            _distanceText.text = $"Расстояние до центра: {distance:F2} м";
        }
    }

    // Обновление отображения рекорда
    private void UpdateHighScoreUI(float highScore)
    {
        if (_highScoreText != null)
        {
            _highScoreText.text = $"Рекорд точности: {highScore:F1}%";
        }
    }

    // Обновление отображения углов пушки
    private void UpdateAnglesUI(float horizontalAngle, float verticalAngle)
    {
        if (_horizontalAngleText != null)
        {
            string formattedAngle = FormatAngle(horizontalAngle);
            _horizontalAngleText.text = $"По горизонтали: {formattedAngle}";
        }

        if (_verticalAngleText != null)
        {
            string formattedAngle = FormatAngle(verticalAngle);
            _verticalAngleText.text = $"По вертикали: {formattedAngle}";
        }
    }

    // Обновление отображения заряда пороха
    private void UpdatePowderUI(float powderPercentage)
    {
        // Обновляем слайдер
        if (_powderSlider != null)
        {
            _powderSlider.value = powderPercentage;
        }

        // Обновляем текст
        if (_powderPercentageText != null)
        {
            _powderPercentageText.text = $"Порох: {powderPercentage:F1}%";
        }
    }
    #endregion

    #region Helper Methods

    private void AddButtonEvents(Button button, UnityEngine.Events.UnityAction<BaseEventData> downAction, UnityEngine.Events.UnityAction<BaseEventData> upAction)
    {
        // Добавляем EventTrigger для обработки событий нажатия и отпускания
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Очищаем существующие триггеры
        trigger.triggers.Clear();
        
        // Добавляем событие нажатия кнопки (PointerDown)
        EventTrigger.Entry entryDown = new EventTrigger.Entry();
        entryDown.eventID = EventTriggerType.PointerDown;
        entryDown.callback.AddListener(downAction);
        trigger.triggers.Add(entryDown);
        
        // Добавляем событие отпускания кнопки (PointerUp)
        EventTrigger.Entry entryUp = new EventTrigger.Entry();
        entryUp.eventID = EventTriggerType.PointerUp;
        entryUp.callback.AddListener(upAction);
        trigger.triggers.Add(entryUp);
        
        // Добавляем событие выхода за пределы кнопки (PointerExit)
        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener(upAction);
        trigger.triggers.Add(entryExit);

        _buttonEventTriggers[button] = trigger;
    }

    private string FormatAngle(float angle)
    {
        // Преобразуем угол в градусы и минуты
        int degrees = Mathf.FloorToInt(Mathf.Abs(angle));
        float minutesFloat = (Mathf.Abs(angle) - degrees) * 60f;
        int minutes = Mathf.RoundToInt(minutesFloat);
        
        // Обрабатываем случай, когда минуты округляются до 60
        if (minutes == 60)
        {
            minutes = 0;
            degrees++;
        }
        
        string sign = angle < 0 ? "-" : "";
        return $"{sign}{degrees}° {minutes:00}'";
    }

    private void OnGameOver()
    {
        // Можно добавить дополнительную обработку конца игры здесь
    }

    #endregion
} 