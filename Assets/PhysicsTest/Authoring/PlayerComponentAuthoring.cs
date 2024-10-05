using Unity.Entities;
using UnityEngine;

namespace HotRoof.PhysicsTest.Components
{
    public class ComponentPlayerAuthoring : MonoBehaviour
    {
        public float Speed;

        public class ComponentPlayerBaker : Baker<ComponentPlayerAuthoring>
        {
            public override void Bake(ComponentPlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ComponentPlayer { Speed = authoring.Speed });
            }
        }
    }
}