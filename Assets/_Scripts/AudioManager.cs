using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Cysharp.Threading.Tasks;

/// <summary>
/// Менеджер звуковых эффектов, создает временные AudioSource для воспроизведения
/// </summary>
public class AudioManager : MonoBehaviour
{
    #region Fields
    [Header("Settings")]
    [SerializeField] private float _defaultVolume = 1f;
    [SerializeField] private bool _useSpatialSound = true;
    [SerializeField] private float _spatialBlend = 1f;
    [SerializeField] private float _minDistance = 1f;
    [SerializeField] private float _maxDistance = 50f;
    
    // Префикс для создаваемых объектов со звуками
    private const string SOUND_OBJ_PREFIX = "Sound_";
    private const string LOOPED_SOUND_PREFIX = "LoopedSound_";
    
    // Список активных звуковых объектов
    private List<GameObject> _activeSoundObjects = new List<GameObject>();
    
    // Словарь для зацикленных звуков, привязанных к объектам
    private Dictionary<Transform, Dictionary<AudioClip, GameObject>> _loopedSounds = 
        new Dictionary<Transform, Dictionary<AudioClip, GameObject>>();
    #endregion

    #region Unity Lifecycle
    private void Update()
    {
        // Удаляем объекты с завершившимися звуками
        CleanupFinishedSounds();
        
        // Обновляем позиции зацикленных звуков, привязанных к объектам
        UpdateLoopedSoundPositions();
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Воспроизводит звук в указанной позиции
    /// </summary>
    public void PlaySound(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        
        // Создаем новый объект для звука
        GameObject soundObj = new GameObject(SOUND_OBJ_PREFIX + clip.name);
        soundObj.transform.position = position;
        soundObj.transform.parent = transform;
        
        // Добавляем и настраиваем AudioSource
        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume * _defaultVolume;
        audioSource.playOnAwake = false;
        
        // Настраиваем пространственность звука
        if (_useSpatialSound)
        {
            audioSource.spatialBlend = _spatialBlend;
            audioSource.minDistance = _minDistance;
            audioSource.maxDistance = _maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
        
        // Воспроизводим звук
        audioSource.Play();
        
        // Добавляем в список активных звуков
        _activeSoundObjects.Add(soundObj);
        
        // Автоматически удаляем объект после окончания звука
        DestroyAfterPlay(soundObj, clip.length).Forget();
    }
    
    /// <summary>
    /// Воспроизводит звук без привязки к позиции (глобально)
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        PlaySound(clip, Vector3.zero, volume);
    }
    
    /// <summary>
    /// Воспроизводит зацикленный звук, привязанный к объекту
    /// </summary>
    public void PlayLoopedSound(AudioClip clip, Transform target, bool follow = true, float volume = 1f)
    {
        if (clip == null || target == null) return;
        
        // Проверяем, есть ли уже такой звук
        if (IsLoopedSoundPlaying(clip, target))
        {
            return;
        }
        
        // Создаем новый объект для звука
        GameObject soundObj = new GameObject(LOOPED_SOUND_PREFIX + clip.name);
        soundObj.transform.position = target.position;
        soundObj.transform.parent = follow ? null : transform; // Если follow=true, объект будет перемещаться вручную
        
        // Добавляем и настраиваем AudioSource
        AudioSource audioSource = soundObj.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.volume = volume * _defaultVolume;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        
        // Настраиваем пространственность звука
        if (_useSpatialSound)
        {
            audioSource.spatialBlend = _spatialBlend;
            audioSource.minDistance = _minDistance;
            audioSource.maxDistance = _maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
        }
        
        // Воспроизводим звук
        audioSource.Play();
        
        // Добавляем в словарь зацикленных звуков
        if (!_loopedSounds.ContainsKey(target))
        {
            _loopedSounds[target] = new Dictionary<AudioClip, GameObject>();
        }
        
        _loopedSounds[target][clip] = soundObj;
    }
    
    /// <summary>
    /// Останавливает зацикленный звук, привязанный к объекту
    /// </summary>
    public void StopLoopedSound(AudioClip clip, Transform target)
    {
        if (clip == null || target == null) return;
        
        if (_loopedSounds.ContainsKey(target) && _loopedSounds[target].ContainsKey(clip))
        {
            GameObject soundObj = _loopedSounds[target][clip];
            if (soundObj != null)
            {
                Destroy(soundObj);
            }
            
            _loopedSounds[target].Remove(clip);
            
            // Если больше нет звуков для этого объекта, удаляем его из словаря
            if (_loopedSounds[target].Count == 0)
            {
                _loopedSounds.Remove(target);
            }
        }
    }
    
    /// <summary>
    /// Останавливает все зацикленные звуки, привязанные к объекту
    /// </summary>
    public void StopAllLoopedSounds(Transform target)
    {
        if (target == null) return;
        
        if (_loopedSounds.ContainsKey(target))
        {
            foreach (GameObject soundObj in _loopedSounds[target].Values)
            {
                if (soundObj != null)
                {
                    Destroy(soundObj);
                }
            }
            
            _loopedSounds.Remove(target);
        }
    }
    
    /// <summary>
    /// Проверяет, воспроизводится ли зацикленный звук для объекта
    /// </summary>
    public bool IsLoopedSoundPlaying(AudioClip clip, Transform target)
    {
        if (clip == null || target == null) return false;
        
        return _loopedSounds.ContainsKey(target) && 
               _loopedSounds[target].ContainsKey(clip) && 
               _loopedSounds[target][clip] != null;
    }
    
    /// <summary>
    /// Останавливает все активные звуки
    /// </summary>
    public void StopAllSounds()
    {
        foreach (GameObject obj in _activeSoundObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        
        _activeSoundObjects.Clear();
        
        // Останавливаем все зацикленные звуки
        foreach (var targetSounds in _loopedSounds)
        {
            foreach (GameObject soundObj in targetSounds.Value.Values)
            {
                if (soundObj != null)
                {
                    Destroy(soundObj);
                }
            }
        }
        
        _loopedSounds.Clear();
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Удаляет объект после окончания воспроизведения звука
    /// </summary>
    private async UniTask DestroyAfterPlay(GameObject soundObj, float delay)
    {
        // Ожидаем завершения звука
        await UniTask.Delay((int)(delay * 1000));
        
        // Удаляем объект, если он еще существует
        if (soundObj != null)
        {
            _activeSoundObjects.Remove(soundObj);
            Destroy(soundObj);
        }
    }
    
    /// <summary>
    /// Очищает список от несуществующих звуковых объектов
    /// </summary>
    private void CleanupFinishedSounds()
    {
        // Удаляем несуществующие объекты из списка
        _activeSoundObjects.RemoveAll(obj => obj == null);
        
        // Очищаем зацикленные звуки от несуществующих объектов или целей
        List<Transform> targetsToRemove = new List<Transform>();
        
        foreach (var targetEntry in _loopedSounds)
        {
            Transform target = targetEntry.Key;
            
            // Если цель уничтожена, помечаем для удаления
            if (target == null)
            {
                targetsToRemove.Add(target);
                continue;
            }
            
            // Проверяем звуки для этой цели
            List<AudioClip> clipsToRemove = new List<AudioClip>();
            
            foreach (var clipEntry in targetEntry.Value)
            {
                AudioClip clip = clipEntry.Key;
                GameObject soundObj = clipEntry.Value;
                
                // Если звуковой объект уничтожен, помечаем для удаления
                if (soundObj == null)
                {
                    clipsToRemove.Add(clip);
                }
            }
            
            // Удаляем помеченные звуки
            foreach (AudioClip clip in clipsToRemove)
            {
                targetEntry.Value.Remove(clip);
            }
            
            // Если не осталось звуков, помечаем цель для удаления
            if (targetEntry.Value.Count == 0)
            {
                targetsToRemove.Add(target);
            }
        }
        
        // Удаляем помеченные цели
        foreach (Transform target in targetsToRemove)
        {
            _loopedSounds.Remove(target);
        }
    }
    
    /// <summary>
    /// Обновляет позиции зацикленных звуков
    /// </summary>
    private void UpdateLoopedSoundPositions()
    {
        foreach (var targetEntry in _loopedSounds)
        {
            Transform target = targetEntry.Key;
            
            // Пропускаем, если цель уничтожена
            if (target == null) continue;
            
            foreach (var clipEntry in targetEntry.Value)
            {
                GameObject soundObj = clipEntry.Value;
                
                // Пропускаем, если звуковой объект уничтожен
                if (soundObj == null) continue;
                
                // Обновляем позицию звука, если он не привязан к родителю
                if (soundObj.transform.parent == null)
                {
                    soundObj.transform.position = target.position;
                }
            }
        }
    }
    #endregion
} 