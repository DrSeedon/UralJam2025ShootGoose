using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Zenject;
using Cysharp.Threading.Tasks;

/// <summary>
/// Менеджер фоновой музыки с плавным переходом между треками
/// </summary>
public class MusicManager : MonoBehaviour
{
    #region Fields
    [Header("Music Tracks")]
    [SerializeField] private List<AudioClip> _musicTracks;
    
    [Header("Settings")]
    [SerializeField] private float _defaultVolume = 0.5f;
    [SerializeField] private float _fadeDuration = 2.0f;
    [SerializeField] private bool _playOnAwake = true;
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _shuffle = false;
    [SerializeField] private AudioMixerGroup _mixerGroup;
    
    // Аудио источники для кросс-фейдинга музыки
    private AudioSource _audioSource1;
    private AudioSource _audioSource2;
    
    // Текущий играющий источник (1 или 2)
    private int _currentAudioSourceIndex = 1;
    
    // Индекс текущего трека
    private int _currentTrackIndex = -1;
    
    // Показывает, что идет процесс перехода
    private bool _isFading = false;
    
    // Очередь треков при перемешивании
    private List<int> _shuffledQueue = new List<int>();
    
    // События для интеграции с другими системами
    public delegate void MusicChangedHandler(AudioClip newTrack, int trackIndex);
    public event MusicChangedHandler OnMusicChanged;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Настраиваем аудио источники
        SetupAudioSources();
    }
    
    private void Start()
    {
        if (_playOnAwake && _musicTracks.Count > 0)
        {
            // Начинаем проигрывать первый трек
            PlayTrack(GetFirstTrackIndex());
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Настраиваем аудио источники для кросс-фейдинга
    /// </summary>
    private void SetupAudioSources()
    {
        // Первый аудио источник
        _audioSource1 = gameObject.AddComponent<AudioSource>();
        _audioSource1.playOnAwake = false;
        _audioSource1.loop = false;
        _audioSource1.volume = 0f;
        if (_mixerGroup != null)
        {
            _audioSource1.outputAudioMixerGroup = _mixerGroup;
        }
        
        // Второй аудио источник
        _audioSource2 = gameObject.AddComponent<AudioSource>();
        _audioSource2.playOnAwake = false;
        _audioSource2.loop = false;
        _audioSource2.volume = 0f;
        if (_mixerGroup != null)
        {
            _audioSource2.outputAudioMixerGroup = _mixerGroup;
        }
    }
    
    /// <summary>
    /// Воспроизводит трек по индексу с плавным переходом
    /// </summary>
    public void PlayTrack(int trackIndex)
    {
        if (_musicTracks.Count == 0) return;
        
        // Проверяем индекс на валидность
        trackIndex = Mathf.Clamp(trackIndex, 0, _musicTracks.Count - 1);
        
        // Сохраняем текущий индекс
        _currentTrackIndex = trackIndex;
        
        // Получаем нужный трек
        AudioClip newTrack = _musicTracks[trackIndex];
        
        // Определяем текущий и следующий аудио источник
        AudioSource currentSource = (_currentAudioSourceIndex == 1) ? _audioSource1 : _audioSource2;
        AudioSource nextSource = (_currentAudioSourceIndex == 1) ? _audioSource2 : _audioSource1;
        
        // Настраиваем следующий аудио источник
        nextSource.clip = newTrack;
        nextSource.volume = 0f;
        nextSource.Play();
        
        // Запускаем плавный переход
        CrossFade(currentSource, nextSource).Forget();
        
        // Меняем активный источник
        _currentAudioSourceIndex = (_currentAudioSourceIndex == 1) ? 2 : 1;
        
        // Вызываем событие смены трека
        OnMusicChanged?.Invoke(newTrack, trackIndex);
    }
    
    /// <summary>
    /// Воспроизводит следующий трек в списке
    /// </summary>
    public void PlayNextTrack()
    {
        if (_musicTracks.Count == 0) return;
        
        int nextIndex = GetNextTrackIndex();
        PlayTrack(nextIndex);
    }
    
    /// <summary>
    /// Воспроизводит предыдущий трек в списке
    /// </summary>
    public void PlayPreviousTrack()
    {
        if (_musicTracks.Count == 0) return;
        
        int prevIndex = GetPreviousTrackIndex();
        PlayTrack(prevIndex);
    }
    
    /// <summary>
    /// Воспроизводит трек по имени
    /// </summary>
    public void PlayTrackByName(string trackName)
    {
        for (int i = 0; i < _musicTracks.Count; i++)
        {
            if (_musicTracks[i] != null && _musicTracks[i].name == trackName)
            {
                PlayTrack(i);
                return;
            }
        }
    }
    
    /// <summary>
    /// Останавливает воспроизведение с плавным затуханием
    /// </summary>
    public void StopMusic()
    {
        // Определяем текущий аудио источник
        AudioSource currentSource = (_currentAudioSourceIndex == 1) ? _audioSource1 : _audioSource2;
        
        // Запускаем плавное затухание
        FadeOut(currentSource).Forget();
    }
    
    /// <summary>
    /// Устанавливает громкость музыки
    /// </summary>
    public void SetVolume(float volume)
    {
        _defaultVolume = Mathf.Clamp01(volume);
        
        // Если не идет процесс перехода, применяем громкость сразу
        if (!_isFading)
        {
            AudioSource currentSource = (_currentAudioSourceIndex == 1) ? _audioSource1 : _audioSource2;
            currentSource.volume = _defaultVolume;
        }
    }
    
    /// <summary>
    /// Включает/выключает режим перемешивания
    /// </summary>
    public void SetShuffle(bool shuffleEnabled)
    {
        _shuffle = shuffleEnabled;
        if (_shuffle)
        {
            ShuffleQueue();
        }
    }
    
    /// <summary>
    /// Добавляет новый музыкальный трек в список
    /// </summary>
    public void AddMusicTrack(AudioClip track)
    {
        if (track != null && !_musicTracks.Contains(track))
        {
            _musicTracks.Add(track);
            
            // Если включено перемешивание, обновляем очередь
            if (_shuffle)
            {
                ShuffleQueue();
            }
        }
    }
    
    /// <summary>
    /// Удаляет музыкальный трек из списка
    /// </summary>
    public void RemoveMusicTrack(AudioClip track)
    {
        if (track != null && _musicTracks.Contains(track))
        {
            _musicTracks.Remove(track);
            
            // Если включено перемешивание, обновляем очередь
            if (_shuffle)
            {
                ShuffleQueue();
            }
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Осуществляет плавный переход между двумя аудио источниками
    /// </summary>
    private async UniTask CrossFade(AudioSource fadeOutSource, AudioSource fadeInSource)
    {
        if (fadeOutSource == null || fadeInSource == null) return;
        
        _isFading = true;
        
        // Начальные значения громкости
        float startVolumeOut = fadeOutSource.volume;
        float startVolumeIn = 0f;
        
        // Целевые значения громкости
        float targetVolumeOut = 0f;
        float targetVolumeIn = _defaultVolume;
        
        // Время начала перехода
        float startTime = Time.time;
        float elapsedTime = 0f;
        
        // Выполняем переход
        while (elapsedTime < _fadeDuration)
        {
            elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / _fadeDuration);
            
            // Плавно изменяем громкость источников
            fadeOutSource.volume = Mathf.Lerp(startVolumeOut, targetVolumeOut, t);
            fadeInSource.volume = Mathf.Lerp(startVolumeIn, targetVolumeIn, t);
            
            await UniTask.Yield();
        }
        
        // Устанавливаем финальные значения
        fadeOutSource.volume = targetVolumeOut;
        fadeInSource.volume = targetVolumeIn;
        
        // Останавливаем первый источник
        fadeOutSource.Stop();
        
        // Если включен цикл, ожидаем завершения трека и запускаем следующий
        if (_loop)
        {
            await WaitForTrackEnd(fadeInSource);
        }
        
        _isFading = false;
    }
    
    /// <summary>
    /// Осуществляет плавное затухание аудио источника
    /// </summary>
    private async UniTask FadeOut(AudioSource source)
    {
        if (source == null) return;
        
        _isFading = true;
        
        // Начальное значение громкости
        float startVolume = source.volume;
        
        // Целевое значение громкости
        float targetVolume = 0f;
        
        // Время начала затухания
        float startTime = Time.time;
        float elapsedTime = 0f;
        
        // Выполняем затухание
        while (elapsedTime < _fadeDuration)
        {
            elapsedTime = Time.time - startTime;
            float t = Mathf.Clamp01(elapsedTime / _fadeDuration);
            
            // Плавно уменьшаем громкость
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            
            await UniTask.Yield();
        }
        
        // Устанавливаем финальное значение и останавливаем
        source.volume = 0f;
        source.Stop();
        
        _isFading = false;
    }
    
    /// <summary>
    /// Ожидает окончания трека и воспроизводит следующий
    /// </summary>
    private async UniTask WaitForTrackEnd(AudioSource source)
    {
        if (source == null || source.clip == null) return;
        
        // Вычисляем время до конца трека
        float timeLeft = source.clip.length - source.time;
        
        // Ожидаем, пока трек не закончится
        await UniTask.Delay((int)(timeLeft * 1000));
        
        // Проверяем, что источник все еще действителен и трек не сменился
        if (source != null && source.isPlaying)
        {
            // Воспроизводим следующий трек
            PlayNextTrack();
        }
    }
    
    /// <summary>
    /// Получаем индекс следующего трека
    /// </summary>
    private int GetNextTrackIndex()
    {
        if (_musicTracks.Count <= 1) return 0;
        
        // Если включено перемешивание, используем очередь
        if (_shuffle)
        {
            // Если очередь пуста, формируем новую
            if (_shuffledQueue.Count == 0)
            {
                ShuffleQueue();
            }
            
            // Извлекаем первый элемент из очереди
            int nextIndex = _shuffledQueue[0];
            _shuffledQueue.RemoveAt(0);
            
            return nextIndex;
        }
        else
        {
            // Обычный режим - следующий трек по порядку
            int nextIndex = _currentTrackIndex + 1;
            
            // Если достигли конца списка, начинаем с начала
            if (nextIndex >= _musicTracks.Count)
            {
                nextIndex = 0;
            }
            
            return nextIndex;
        }
    }
    
    /// <summary>
    /// Получаем индекс предыдущего трека
    /// </summary>
    private int GetPreviousTrackIndex()
    {
        if (_musicTracks.Count <= 1) return 0;
        
        // Если включено перемешивание, всё равно идем в обратном порядке
        // (т.к. для "предыдущего" трека это логичнее)
        int prevIndex = _currentTrackIndex - 1;
        
        // Если достигли начала списка, переходим в конец
        if (prevIndex < 0)
        {
            prevIndex = _musicTracks.Count - 1;
        }
        
        return prevIndex;
    }
    
    /// <summary>
    /// Получаем индекс первого трека при запуске
    /// </summary>
    private int GetFirstTrackIndex()
    {
        if (_musicTracks.Count == 0) return -1;
        
        // Если включено перемешивание, выбираем случайный трек
        if (_shuffle)
        {
            // Если очередь пуста, формируем новую
            if (_shuffledQueue.Count == 0)
            {
                ShuffleQueue();
            }
            
            // Извлекаем первый элемент из очереди
            int firstIndex = _shuffledQueue[0];
            _shuffledQueue.RemoveAt(0);
            
            return firstIndex;
        }
        else
        {
            // В обычном режиме просто берем первый трек
            return 0;
        }
    }
    
    /// <summary>
    /// Формирует перемешанную очередь треков
    /// </summary>
    private void ShuffleQueue()
    {
        _shuffledQueue.Clear();
        
        // Добавляем все индексы треков в очередь
        for (int i = 0; i < _musicTracks.Count; i++)
        {
            if (i != _currentTrackIndex) // Исключаем текущий трек
            {
                _shuffledQueue.Add(i);
            }
        }
        
        // Перемешиваем очередь
        int n = _shuffledQueue.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            int temp = _shuffledQueue[k];
            _shuffledQueue[k] = _shuffledQueue[n];
            _shuffledQueue[n] = temp;
        }
    }
    
    /// <summary>
    /// Возвращает список доступных треков
    /// </summary>
    public List<AudioClip> GetAvailableTracks()
    {
        return new List<AudioClip>(_musicTracks);
    }
    
    /// <summary>
    /// Возвращает текущий трек
    /// </summary>
    public AudioClip GetCurrentTrack()
    {
        if (_currentTrackIndex >= 0 && _currentTrackIndex < _musicTracks.Count)
        {
            return _musicTracks[_currentTrackIndex];
        }
        
        return null;
    }
    
    /// <summary>
    /// Возвращает индекс текущего трека
    /// </summary>
    public int GetCurrentTrackIndex()
    {
        return _currentTrackIndex;
    }
    #endregion
} 