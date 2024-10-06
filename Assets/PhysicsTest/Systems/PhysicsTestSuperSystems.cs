using Latios;
using Latios.Transforms.Systems;
using Unity.Entities;

namespace HotRoof.PhysicsTest.Systems
{
    [DisableAutoCreation]
    [UpdateBefore(typeof(TransformSuperSystem))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PhysicsTestSimulationSuperSystem : SuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddUnmanagedSystem<EnemySpawnSystem>();
            GetOrCreateAndAddUnmanagedSystem<EnemySystem>();
            
            GetOrCreateAndAddManagedSystem<PlayerSystem>();
            GetOrCreateAndAddUnmanagedSystem<ProjectileSystem>();
        }
    }
    
    [DisableAutoCreation]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class PhysicsTestPresentationSuperSystems : SuperSystem
    {
        protected override void CreateSystems()
        {
            GetOrCreateAndAddManagedSystem<PlayerCameraSystem>();
        }
    }
    
    public class PhysicsTestBootstrap
    {
        public static void Install(World world)
        {
            BootstrapTools.InjectSystem(TypeManager.GetSystemTypeIndex<PhysicsTestPresentationSuperSystems>(), world);
            BootstrapTools.InjectSystem(TypeManager.GetSystemTypeIndex<PhysicsTestSimulationSuperSystem>(), world);
        }
    }
    
  
}