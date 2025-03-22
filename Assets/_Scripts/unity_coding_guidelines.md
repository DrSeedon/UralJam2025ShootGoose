# Unity Coding Guidelines 2024

## 🎯 Быстрый старт
```csharp
// 👉 Три главных принципа:
// 1. Безопасность > Производительность
// 2. Читаемость > Краткость
// 3. Явные проверки > Неочевидная магия
```

## 🎮 Базовые принципы

### Структура MonoBehaviour
```csharp
public class PlayerController : MonoBehaviour 
{
    #region Fields
    [SerializeField] private float _moveSpeed = 5f;
    public bool IsMoving;
    #endregion

    #region Unity Lifecycle
    private void Start() { }
    private void Update() { }
    #endregion

    #region Public Methods
    public void Initialize() { }
    #endregion

    #region Private Methods
    private void HandleMovement() { }
    #endregion
}
```

### Логирование
```csharp
// ❌ ПЛОХО: Логи без контекста
Debug.LogError("Ошибка загрузки");
Debug.Log("Файл создан");
// ❌ ПЛОХО: Прямое имя класса
Debug.LogError("[GeoModifierCreationWindow] Ошибка загрузки");
// ❌ ПЛОХО: Эмодзи в логах
Debug.LogError($"[{GetType().Name}] ⚠️ Файл не найден");
// ❌ ПЛОХО: Лишние неинформативные логи
Debug.Log($"[{GetType().Name}] Процесс запущен");
Debug.Log($"[{GetType().Name}] Выполняем операцию...");

// ✅ ХОРОШО: Автоматическое имя класса + описание ошибки
Debug.LogError($"[{GetType().Name}] Ошибка загрузки таблиц");
Debug.Log($"[{GetType().Name}] Создан файл: {fileName}");
// ✅ ХОРОШО: Информативные логи с контекстом
Debug.Log($"[{GetType().Name}] Создан файл {fileName} в директории {path}");
Debug.LogError($"[{GetType().Name}] Ошибка при обработке строки {rowNumber}: {exception.Message}");

// 📝 ПРАВИЛА:
// 1. ВСЕГДА добавляй имя класса через GetType().Name
// 2. Используй string interpolation ($"...")
// 3. Формат: [ИмяКласса] Сообщение
// 4. Сообщение должно быть информативным
// 5. НИКОГДА не используй эмодзи в логах - они не отображаются в Unity UI
// 6. Не создавай избыточные логи без конкретной полезной информации
// 7. Логируй только то, что важно для отладки и диагностики
``` 

### Правила именования
- 🔷 MonoBehaviour: без суффиксов
- 🔶 ScriptableObject: суффикс `SO` или `Data`
- 🔷 Сервисы: префикс `I` для интерфейсов
- 🔶 Абстрактные классы: префикс `Abstract`
- 🔷 Enum: суффикс `Type`

### Говорящие имена
- Без лишних слов типа Manager, Controller, Helper
- Глаголы для методов: Attack(), Move(), Die()
- Существительные/прилагательные для свойств: health, isAlive

### Пространства имен
- ❌ НЕ создавать собственные пространства имен (namespace)
- ✅ Использовать только те, что предоставляются:
  1. Unity Engine
  2. Сторонними пакетами

## 🏗 Архитектура

### Dependency Injection (Zenject)
```csharp
// ❌ НИКОГДА: Синглтоны
public class GameManager : MonoBehaviour 
{
    private static GameManager _instance;
    public static GameManager Instance => _instance;
}

// ✅ ВСЕГДА: Dependency Injection
public class GameManager : MonoBehaviour 
{
    [Inject] private IInputService _inputService;
    [Inject] private IGameStateService _gameState;
}

// 📝 УСТАНОВКА ЗАВИСИМОСТЕЙ:
public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Всегда используй FromComponentInHierarchy для MonoBehaviour
        Container.Bind<NavigationManager>().FromComponentInHierarchy().AsSingle();
    }
}
```

### События
```csharp
// ❌ НЕТ: C# события
public event Action<int> OnValueChanged;

// ✅ ДА: UnityEvent
public UnityEvent<int> onValueChanged = new UnityEvent<int>();

// 🔍 ВАЖНО: UnityEvent автоматически очищается при уничтожении GameObject
```

#### Исключения для C# events:
1. Производительность в критических местах
2. События только для кода (не используются в инспекторе)

## 🛠 Работа с Unity

### Асинхронность
```csharp
// ❌ ЗАПРЕЩЕНО: Корутины
IEnumerator LoadData()
{
    yield return new WaitForSeconds(1f);
}

// ✅ ПРАВИЛЬНО: async/await + UniTask
async UniTask LoadData()
{
    await UniTask.Delay(1000);
}

// 📝 ПРАВИЛА:
// 1. async void ТОЛЬКО для UI событий
// 2. В остальных случаях возвращать UniTask
```

### Сериализация
```csharp
// ❌ ПЛОХО: Unity-типы напрямую
public Vector2 Position { get; set; }

// ✅ ХОРОШО: Разделение на компоненты
private float _positionX, _positionY;
[JsonIgnore]
public Vector2 Position
{
    get => new Vector2(_positionX, _positionY);
    set
    {
        _positionX = value.x;
        _positionY = value.y;
    }
}
```

## 🎨 UI

### UI компоненты
```csharp
public class MenuController : MonoBehaviour 
{
    // ВСЕГДА public без [SerializeField] для UI
    public Button startButton;
    public TMP_Text statusText; // Только TMP_Text
    public CanvasGroup mainPanel;
}
```

### Очистка ресурсов
```csharp
private void OnDisable()
{
    // Отписка от событий
    onValueChanged.RemoveAllListeners();
    
    // Остановка анимаций
    DOTween.Kill(this);
}
```

## 🚀 Производительность

### Оптимизации
```csharp
// ❌ ПЛОХО: Создание объектов в Update
void Update() {
    var position = new Vector3(x, y, z);
}

// ✅ ХОРОШО: Переиспользование
private Vector3 _position;
void Update() {
    _position.Set(x, y, z);
}
```

## 🛡 Безопасность

### Защита от дурака
1. **Проверяй входные данные**:
   - Проверь что метод вообще может принять эти данные
   - Проверь что данные не пустые/не нулевые
   - Проверь что данные в нужном формате
   - Если что-то не так - пиши в лог и выходи

2. **Защищай состояние**:
   - Не давай вызывать метод когда нельзя
   - Не давай вызывать метод повторно если он уже работает

3. **Защищай ресурсы**:
   - Проверяй что ресурс существует перед использованием
   - Чисти за собой когда закончил
   - Не держи ресурсы дольше чем нужно

### Проверки и защита
- Ранний выход через return
- Null-check только для опциональных компонентов
- Понятные условия в if

## 🔧 Утилиты

### Утилитные методы (Helper Methods)
Утилитные методы - это небольшие, переиспользуемые методы, которые делают код чище и понятнее.

#### Характеристики хорошего утилитного метода:
1. Делает ОДНУ конкретную операцию
2. Имеет понятное название, отражающее что он делает
3. Легко переиспользуется в разных местах
4. Возвращает новые данные, не изменяя входные
5. Использует LINQ для работы с коллекциями
6. Имеет параметры по умолчанию для гибкости

#### Примеры:
```csharp
// Плохо - длинный запутанный код
var items = allItems
    .Where(i => i.Type == currentType)
    .Where(i => i.Status == "active" || i.Status == "pending")
    .OrderByDescending(i => i.Date)
    .ToList();

// Хорошо - утилитные методы делают код понятным
var relevantItems = GetItemsByType(currentType);
var activeItems = FilterByStatuses(relevantItems, "active", "pending");
var sortedItems = SortByDateDescending(activeItems);

// Примеры утилитных методов:
protected bool IsBatchRelevant(BaseBatchInfo batch) => 
    batch.ProcessorType.Equals(ProcessorType, StringComparison.OrdinalIgnoreCase);

protected IEnumerable<T> FilterByStatuses<T>(
    IEnumerable<T> items, 
    params string[] statuses) where T : IStatusHolder =>
    items.Where(i => statuses.Contains(i.Status));

protected IEnumerable<T> SortByDateDescending<T>(
    IEnumerable<T> items) where T : IDateHolder =>
    items.OrderByDescending(i => i.Date);
```

### Условия как вопросы
```csharp
// Плохо - куча проверок
if (hp > 0 && !isDead && canMove && !isStunned)

// Хорошо - один метод с понятным названием
if (CanPerformAction())

// Еще лучше - набор понятных методов
if (IsAlive() && CanMove() && !IsStunned())
```

## 🚫 Запрещённые практики

1. Синглтоны (используй DI)
2. Корутины (используй async/await)
3. FindObjectOfType и GameObject.Find
4. GetComponent в Update
5. Создание объектов в Update
6. Публичные поля без необходимости (кроме UI)
7. Создание собственных namespace

## 💡 Как применять
1. Выделяйте общую логику в базовые классы
2. Используйте абстрактные классы и интерфейсы для определения общего поведения
3. Создавайте универсальные методы вместо копирования кода
4. Если видите похожий код в разных местах - это сигнал к рефакторингу

### Исключения
1. Иногда небольшое дублирование лучше, чем сложная абстракция
2. Если код похож, но развивается в разных направлениях - возможно, его не стоит объединять

### Результат:
- Код короткий, но понятный
- Легко читать и поддерживать
- Хорошая производительность
- Минимум шансов накосячить

## 🎯 Заключение

### Главные принципы
1. Безопасность превыше всего
2. Читаемость важнее умного кода
3. Тестируй всё, что может сломаться
4. Оптимизируй только после профилирования

Помни: Хороший код - это не тот, к которому нечего добавить, а тот, от которого нечего убрать.
