using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

// Отвечает за стрельбу из пушки
public class CannonShooter : MonoBehaviour
{
    [Inject] private GameStateManager _gameStateManager;
    [Inject] private PowderController _powderController;
    [Inject] private BallisticsCalculator _ballisticsCalculator;
    [Inject] private TrajectoryVisualizer _trajectoryVisualizer;
    [Inject] private CannonEffects _cannonEffects;
    [Inject] private Target _target;
    
    [SerializeField] private Transform _projectileSpawnPoint;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private float _fireDelay = 0.5f;
    
    // Публичное свойство для доступа к точке спауна снаружи
    public Transform ProjectileSpawnPoint => _projectileSpawnPoint;
    
    /// <summary>
    /// Обрабатывает нажатие на кнопку выстрела
    /// </summary>
    public async UniTask FireCannon()
    {
        if (!_gameStateManager.CanFire)
            return;

        _gameStateManager.IsFiring = true;
        await FireProjectileAsync();
        _gameStateManager.IsFiring = false;
    }
    
    private async UniTask FireProjectileAsync()
    {
        if (_gameStateManager.ProjectilesRemaining <= 0)
        {
            return;
        }

        // Уменьшаем количество снарядов
        _gameStateManager.ProjectilesRemaining--;
        
        // Воспроизводим эффекты выстрела
        _cannonEffects.PlayMuzzleFlash();
        await _cannonEffects.PlayFireSound();

        // Получаем текущие параметры стрельбы
        float powderPercentage = _powderController.CurrentPowderPercentage;
        
        // Проверяем безопасность заряда пороха
        bool isSafeCharge = _ballisticsCalculator.IsPowderSafe(powderPercentage);
        
        // Если заряд небезопасен, есть шанс взрыва пушки
        if (!isSafeCharge && Random.value > 0.5f)
        {
            // Взрыв пушки из-за перезаряда
            await _cannonEffects.PlayExplosionAsync();
            _gameStateManager.ShowResults("Перезаряд привел к взрыву пушки!");
            return;
        }
        
        // Рассчитываем траекторию для передачи снаряду
        List<Vector3> trajectory = _ballisticsCalculator.CalculateTrajectory(
            _projectileSpawnPoint.position,
            _projectileSpawnPoint.forward,
            powderPercentage
        );
        
        // Если траектория пустая (например, слишком мало пороха)
        if (trajectory.Count < 2)
        {
            return;
        }
        
        // Задержка после выстрела
        await UniTask.Delay((int)(_fireDelay * 1000));
        
        // Отключаем предпросмотр траектории на время полета снаряда
        _trajectoryVisualizer.EnableTrajectoryPreview(false);
        
        // Создаем снаряд и передаем ему траекторию
        GameObject projectile = Instantiate(_projectilePrefab, _projectileSpawnPoint.position, _projectileSpawnPoint.rotation);
        Projectile projectileComponent = projectile.GetComponent<Projectile>();
        
        if (projectileComponent != null)
        {
            // Запускаем движение снаряда по траектории и передаем Target
            await projectileComponent.MoveAlongTrajectoryAsync(trajectory, _target);
        }
        
        // Включаем предпросмотр траектории обратно
        _trajectoryVisualizer.EnableTrajectoryPreview(true);
        
        // Проверяем, не закончилась ли игра
        _gameStateManager.CheckGameOver();
    }
}