using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Отвечает только за визуализацию траектории
public class TrajectoryVisualizer : MonoBehaviour
{
    [Inject] private BallisticsCalculator _ballisticsCalculator;
    [Inject] private PowderController _powderController;
    [Inject] private CannonShooter _cannonShooter;
    
    [SerializeField] private LineRenderer _trajectoryLineRenderer;
    [SerializeField] private bool _showTrajectoryPreview = true;
    [SerializeField] private int _trajectoryPreviewResolution = 20;
    [SerializeField] private float _trajectoryPreviewUpdateRate = 0.2f;
    
    private float _trajectoryPreviewTimer = 0f;
    
    private void Start()
    {
        SetupTrajectoryPreview();
    }
    
    private void Update()
    {
        if (_showTrajectoryPreview)
        {
            UpdateTrajectoryPreview();
        }
    }
    
    public void SetupTrajectoryPreview()
    {
        if (_trajectoryLineRenderer == null && _showTrajectoryPreview)
        {
            _trajectoryLineRenderer = gameObject.AddComponent<LineRenderer>();
            _trajectoryLineRenderer.startWidth = 0.1f;
            _trajectoryLineRenderer.endWidth = 0.05f;
            _trajectoryLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _trajectoryLineRenderer.startColor = new Color(1f, 1f, 0f, 0.3f);
            _trajectoryLineRenderer.endColor = new Color(1f, 0.5f, 0f, 0.1f);
            _trajectoryLineRenderer.positionCount = 0;
        }

        if (_trajectoryLineRenderer != null)
        {
            _trajectoryLineRenderer.enabled = _showTrajectoryPreview;
        }
    }
    
    public void EnableTrajectoryPreview(bool enable)
    {
        if (_trajectoryLineRenderer != null)
        {
            _trajectoryLineRenderer.enabled = enable;
        }
    }
    
    private void UpdateTrajectoryPreview()
    {
        if (_cannonShooter.ProjectileSpawnPoint == null)
        {
            Debug.LogWarning("ProjectileSpawnPoint is null in CannonShooter");
            return;
        }
        
        _trajectoryPreviewTimer -= Time.deltaTime;

        if (_trajectoryPreviewTimer <= 0)
        {
            _trajectoryPreviewTimer = _trajectoryPreviewUpdateRate;

            if (_trajectoryLineRenderer != null)
            {
                float powderPercentage = _powderController.CurrentPowderPercentage;
                Transform spawnPoint = _cannonShooter.ProjectileSpawnPoint;
                Vector3 direction = spawnPoint.forward;

                if (powderPercentage <= 0.1f)
                {
                    List<Vector3> shortTrajectory = new List<Vector3>();
                    shortTrajectory.Add(spawnPoint.position);
                    shortTrajectory.Add(spawnPoint.position + spawnPoint.forward * 0.5f -
                                        Vector3.up * 2f);

                    _trajectoryLineRenderer.positionCount = shortTrajectory.Count;
                    _trajectoryLineRenderer.SetPositions(shortTrajectory.ToArray());
                    return;
                }

                List<Vector3> trajectory = _ballisticsCalculator.CalculateTrajectory(
                    spawnPoint.position,
                    direction,
                    powderPercentage
                );

                if (trajectory.Count > _trajectoryPreviewResolution)
                {
                    float step = (float)trajectory.Count / _trajectoryPreviewResolution;
                    List<Vector3> simplifiedTrajectory = new List<Vector3>();

                    for (int i = 0; i < _trajectoryPreviewResolution; i++)
                    {
                        int index = Mathf.Min(Mathf.FloorToInt(i * step), trajectory.Count - 1);
                        simplifiedTrajectory.Add(trajectory[index]);
                    }

                    simplifiedTrajectory.Add(trajectory[trajectory.Count - 1]);
                    trajectory = simplifiedTrajectory;
                }

                _trajectoryLineRenderer.positionCount = trajectory.Count;
                _trajectoryLineRenderer.SetPositions(trajectory.ToArray());
            }
        }
    }
}