using System;
using HotRoof.PhysicsTest.Components;
using Latios.Transforms;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace HotRoof.PhysicsTest
{
    public partial struct EnemySpawnSystem : ISystem
    {
        private Random m_rnd;

        public void OnCreate(ref SystemState state)
        {
            m_rnd = new Random(123);
            
            state.RequireForUpdate<ComponentDatabase>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ComponentDatabase>(out var db))
                throw new Exception(
                    $"An entity-component of type '{nameof(ComponentDatabase)}' is required in the subscene."
                );


            int activeEnemies = SystemAPI.QueryBuilder().WithAll<ComponentEnemy>().Build().CalculateEntityCount();
            switch (db.Scenario)
            {
                case Example.Static:
                {
                    var length = (int) math.ceil(math.sqrt(math.max(0, db.MaxEnemies - activeEnemies)));
                    
                    for (int i = -length / 2; i < length / 2; i++)
                    {
                        for (int j = 0; j < length; j++)
                        {
                            var e = state.EntityManager.Instantiate(db.EnemyPrefab);
                            var transform = SystemAPI.GetComponent<WorldTransform>(db.EnemyPrefab);
                            transform.worldTransform.position = new float3(i, 0, j);
                            transform.worldTransform.rotation = quaternion.EulerXYZ(0, m_rnd.NextFloat(0, math.PI2), 0);

                            state.EntityManager.AddComponentData(e, transform);
                        }
                    }
                    break;
                }
                case Example.Moving:
                {
                    for (int i = activeEnemies; i < db.MaxEnemies; i++)
                    {
                        var e = state.EntityManager.Instantiate(db.EnemyPrefab);
                        var transform = SystemAPI.GetComponent<WorldTransform>(db.EnemyPrefab);
                        transform.worldTransform.position = m_rnd.NextFloat3(-db.MapSize / 2, db.MapSize / 2);
                        transform.worldTransform.position.y = 0;
                        transform.worldTransform.rotation = quaternion.EulerXYZ(0, m_rnd.NextFloat(0, math.PI2), 0);

                        state.EntityManager.AddComponentData(e, transform);
                    }

                    break;
                }
            }
        }
    }
}
