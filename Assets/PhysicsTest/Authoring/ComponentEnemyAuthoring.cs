using Unity.Entities;
using UnityEngine;

namespace HotRoof.PhysicsTest.Components
{
    public class EnemyAuthoring : MonoBehaviour
    {
        public float Speed;

        public class ComponentEnemyBaker : Baker<EnemyAuthoring>
        {
            public override void Bake(EnemyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ComponentEnemy { Speed = authoring.Speed });
            }
        }
    }
}