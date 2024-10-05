using Unity.Entities;
using UnityEngine;

namespace HotRoof.PhysicsTest.Components
{
    public class DatabaseAuthoring : MonoBehaviour
    {
        public GameObject ProjectilePrefab;
        public GameObject EnemyPrefab;
        public int MaxEnemies;

        public class DatabaseBaker : Baker<DatabaseAuthoring>
        {
            public override void Bake(DatabaseAuthoring authoring)
            {
                var scale = authoring.transform.lossyScale;
                
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,
                    new ComponentDatabase
                    {
                        ProjectilePrefab = GetEntity(authoring.ProjectilePrefab, TransformUsageFlags.Dynamic),
                        EnemyPrefab = GetEntity(authoring.EnemyPrefab, TransformUsageFlags.Dynamic),
                        MaxEnemies = authoring.MaxEnemies,
                        MapSize = scale.x * 10 // Multiply scale by 10 since we use a plane as base mesh
                    }
                );
            }
        }
    }
}