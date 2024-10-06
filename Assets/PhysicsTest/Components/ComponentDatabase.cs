using System;
using Unity.Entities;

namespace HotRoof.PhysicsTest.Components
{
    public enum Example
    {
        Moving, Static
    }
    
    public struct ComponentDatabase : IComponentData
    {
        public Entity ProjectilePrefab;
        public Entity EnemyPrefab;
        public int MaxEnemies;
        public float MapSize;

        public Example Scenario;
    }
}