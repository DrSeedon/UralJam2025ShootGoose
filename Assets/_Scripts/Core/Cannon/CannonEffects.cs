using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

// Отвечает за эффекты пушки
public class CannonEffects : MonoBehaviour
{
    [Inject] private AudioManager _audioManager;
    [Inject] private CannonShooter _cannonShooter;
    
    [SerializeField] private ParticleSystem _muzzleFlashEffect;
    [SerializeField] private ParticleSystem _explosionEffect;
    [SerializeField] private Transform _cannonBarrel;
    
    [Header("Audio")]
    [SerializeField] private AudioClip _fireSound;
    [SerializeField] private AudioClip _explosionSound;
    
    public void PlayMuzzleFlash()
    {
        if (_muzzleFlashEffect != null)
        {
            if (_cannonShooter.ProjectileSpawnPoint != null)
            {
                _muzzleFlashEffect.transform.position = _cannonShooter.ProjectileSpawnPoint.position;
            }
            
            _muzzleFlashEffect.Stop();
            _muzzleFlashEffect.Play();
        }
    }
    
    public async UniTask PlayFireSound()
    {
        if (_fireSound != null)
        {
            Vector3 soundPosition = _cannonShooter.ProjectileSpawnPoint != null ? 
                _cannonShooter.ProjectileSpawnPoint.position : transform.position;
                
            _audioManager.PlaySound(_fireSound, soundPosition);
        }
    }
    
    public async UniTask PlayExplosionAsync()
    {
        if (_cannonBarrel != null)
        {
            _cannonBarrel.gameObject.SetActive(false);
        }
        
        if (_explosionEffect != null)
        {
            Vector3 explosionPosition = _cannonShooter.ProjectileSpawnPoint != null ? 
                _cannonShooter.ProjectileSpawnPoint.position : transform.position;
                
            _explosionEffect.transform.position = explosionPosition;
            _explosionEffect.Play();
        }
        
        if (_explosionSound != null)
        {
            Vector3 soundPosition = _cannonShooter.ProjectileSpawnPoint != null ? 
                _cannonShooter.ProjectileSpawnPoint.position : transform.position;
                
            _audioManager.PlaySound(_explosionSound, soundPosition);
        }
        
        await UniTask.Delay(1000);
    }
}