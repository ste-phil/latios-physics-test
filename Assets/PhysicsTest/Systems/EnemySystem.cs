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
        private EntityQuery m_allColliders;

        public void OnCreate(ref SystemState state)
        {
            m_handle = new BuildCollisionLayerTypeHandles(ref state);
            m_rnd = new Random(123);
            m_allColliders = state
                .Fluent()
                .With<Collider>(true)
                .PatchQueryForBuildingCollisionLayer()
                .Build();
            
            state.RequireForUpdate<ComponentDatabase>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ComponentDatabase>(out var db))
                throw new Exception($"An entity-component of type '{nameof(ComponentDatabase)}' is required in the subscene.");
            
            m_handle.Update(ref state);
            var settings = new CollisionLayerSettings
            {
                worldAabb = new Aabb(-db.MapSize, db.MapSize), 
                worldSubdivisionsPerAxis = new int3((int) db.MapSize / 8, 1, (int)db.MapSize / 8),
            };
            
            state.Dependency = Physics
                .BuildCollisionLayer(m_allColliders, in m_handle)
                .WithSettings(settings)
                .ScheduleParallel(out var layer, Allocator.TempJob, state.Dependency);
            
            state.Dependency = new EnemyMoveJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new EnemyAvoidContactsJob
            {
                Random = m_rnd,
                Layer = layer
            }.ScheduleParallel(state.Dependency);
            layer.Dispose(state.Dependency);
        }
    }
    
    [BurstCompile]
    partial struct EnemyMoveJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref ComponentEnemy enemy, ref RigidBody rb, in WorldTransform transform)
        {
            rb.velocity.linear = transform.forwardDirection * enemy.Speed;
        }
    }
    
    [BurstCompile]
    [WithAll(typeof(ComponentEnemyAvoidContacts))]
    partial struct EnemyAvoidContactsJob : IJobEntity
    {
        [ReadOnly] public CollisionLayer Layer;
        public Random Random;

        public void Execute(ref ComponentEnemy enemy, ref WorldTransform transform)
        {
            var ray = new Ray(transform.position + new float3(0, .5f, 0), transform.forwardDirection, 2.0f);
            if (Physics.Raycast(ray, Layer, out var hit, out var info))
            {
                var sign = Random.NextInt(0, 2) * 2 - 1;
                transform.worldTransform.rotation = math.mul(transform.rotation, quaternion.RotateY(sign * math.PI / 2));
            }
        }
    }
}