using Zenject;

/// <summary>
/// Инсталлер для регистрации зависимостей в Zenject.
/// Связывает сервисы и компоненты в проекте.
/// </summary>
public class ProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Регистрируем аудио сервисы
        Container.Bind<AudioManager>().FromComponentInHierarchy().AsSingle();
        Container.Bind<MusicManager>().FromComponentInHierarchy().AsSingle();
        
        // Регистрируем компоненты пушки
        Container.Bind<CannonController>().FromComponentInHierarchy().AsSingle();
        Container.Bind<PowderController>().FromComponentInHierarchy().AsSingle();
        Container.Bind<BallisticsCalculator>().FromComponentInHierarchy().AsSingle();
        Container.Bind<CannonGameManager>().FromComponentInHierarchy().AsSingle();
        Container.Bind<Target>().FromComponentInHierarchy().AsSingle();
        Container.Bind<ResultsPanel>().FromComponentInHierarchy().AsSingle();
        
        // Регистрируем UI-контроллер
        Container.Bind<GameUIController>().FromComponentInHierarchy().AsSingle();
    }
}
