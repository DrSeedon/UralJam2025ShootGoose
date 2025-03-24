using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class CannonGameManager : MonoBehaviour
{
    [Inject] private GameStateManager _gameState;
    [Inject] private CannonShooter _cannonShooter;
    [Inject] private TrajectoryVisualizer _trajectoryVisualizer;
    [Inject] private CannonController _cannonController;
    [Inject] private PowderController _powderController;
    
    private void Start()
    {
        // Инициализируем игру
        _gameState.onRestartGame.AddListener(OnGameRestart);
    }
    
    private void OnGameRestart()
    {
        // Возвращаем все системы в начальное состояние
        _cannonController.ResetToInitialAngles();
        _powderController.ResetPowderToDefault();
    }
}