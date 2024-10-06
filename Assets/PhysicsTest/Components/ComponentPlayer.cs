using Unity.Entities;

namespace HotRoof.PhysicsTest.Components
{
    public struct ComponentPlayer : IComponentData
    {
        public float Speed;
        public float BreakingSpeed;
    }
}