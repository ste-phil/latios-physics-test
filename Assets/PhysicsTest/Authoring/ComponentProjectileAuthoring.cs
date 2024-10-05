using Unity.Entities;
using UnityEngine;

namespace HotRoof.PhysicsTest.Systems
{
    public class ComponentProjectileAuthoring : MonoBehaviour
    {
        public float Speed;
        public float TimeTillDestroy;

        public class ComponentProjectileBaker : Baker<ComponentProjectileAuthoring>
        {
            public override void Bake(ComponentProjectileAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,
                    new ComponentProjectile { Speed = authoring.Speed, TimeTillDestroy = authoring.TimeTillDestroy }
                );
            }
        }
    }
}