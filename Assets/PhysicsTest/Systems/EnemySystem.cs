using System;
using HotRoof.PhysicsTest.Components;
using Latios;
using Latios.Psyshock;
using Latios.Psyshock.Anna;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Collider = Latios.Psyshock.Collider;
using Physics = Latios.Psyshock.Physics;
using Random = Unity.Mathematics.Random;
using Ray = Latios.Psyshock.Ray;

namespace HotRoof.PhysicsTest.Systems
{
    public partial struct EnemySystem : ISystem
    {
        private Random m_rnd;
        private BuildCollisionLayerTypeHandles m_handle;
        private EntityQuery m_allCollidersExceptEnemyQuery;

        public void OnCreate(ref SystemState state)
        {
            m_handle = new BuildCollisionLayerTypeHandles(ref state);
            m_rnd = new Random(123);
            m_allCollidersExceptEnemyQuery = state
                .Fluent()
                .With<Collider>(true)
                .Without<ComponentEnemy>(true)
                .PatchQueryForBuildingCollisionLayer()
                .Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ComponentDatabase>(out var db))
                throw new Exception($"An entity-component of type '{nameof(ComponentDatabase)}' is required in the subscene.");

            int activeEnemies = SystemAPI.QueryBuilder().WithAll<ComponentEnemy>().Build().CalculateEntityCount();
            for (int i = activeEnemies; i < db.MaxEnemies; i++)
            {
                var e = state.EntityManager.Instantiate(db.EnemyPrefab);
                var transform = SystemAPI.GetComponent<WorldTransform>(db.EnemyPrefab);
                transform.worldTransform.position = m_rnd.NextFloat3(-db.MapSize / 2, db.MapSize / 2);
                transform.worldTransform.position.y = 0;
                transform.worldTransform.rotation = quaternion.EulerXYZ(0, m_rnd.NextFloat(0, math.PI2), 0);
                
                state.EntityManager.AddComponentData(e, transform);
            }
            
            m_handle.Update(ref state);
            var settings = new CollisionLayerSettings
            {
                worldAabb = new Aabb(-db.MapSize, db.MapSize), 
                worldSubdivisionsPerAxis = new int3((int) db.MapSize / 8, 1, (int)db.MapSize / 8),
            };
            
            state.Dependency = Physics
                .BuildCollisionLayer(m_allCollidersExceptEnemyQuery, in m_handle)
                .WithSettings(settings)
                .ScheduleParallel(out var layer, Allocator.TempJob, state.Dependency);
            
            state.Dependency = new MoveEnemiesJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new AvoidContactsJob
            {
                Random = new Random((uint)(SystemAPI.Time.DeltaTime * 1000)),
                Layer = layer
            }.ScheduleParallel(state.Dependency);
            layer.Dispose(state.Dependency);
        }
    }
    
    [BurstCompile]
    partial struct MoveEnemiesJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref ComponentEnemy enemy, ref RigidBody rb, in WorldTransform transform)
        {
            rb.velocity.linear = transform.forwardDirection * enemy.Speed;
        }
    }
    
    [BurstCompile]
    partial struct AvoidContactsJob : IJobEntity
    {
        [ReadOnly] public CollisionLayer Layer;
        public Random Random;

        public void Execute(ref ComponentEnemy enemy, TransformAspect transform)
        {
            var ray = new Ray(transform.worldPosition + new float3(0, .5f, 0), transform.forwardDirection, 2.0f);
            if (Physics.Raycast(ray, Layer, out var hit, out var info))
            {
                var sign = Random.NextInt(0, 2) * 2 - 1;
                transform.worldRotation = math.mul(transform.worldRotation, quaternion.RotateY(sign * math.PI / 2));
            }
        }
    }
}