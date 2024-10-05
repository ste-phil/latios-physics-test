using Unity.Entities;
using UnityEngine;

namespace HotRoof.PhysicsTest.Components
{
    public struct ComponentDatabase : IComponentData
    {
        public Entity ProjectilePrefab;
        public Entity EnemyPrefab;
        public int MaxEnemies;
        public float MapSize;
    }
}