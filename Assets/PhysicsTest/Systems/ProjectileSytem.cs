using System;
using System.Linq;
using HotRoof.PhysicsTest.Components;
using Latios.Psyshock.Anna;
using Latios.Systems;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using Collider = Latios.Psyshock.Collider;
using Physics = Latios.Psyshock.Physics;

namespace HotRoof.PhysicsTest.Systems
{
    public struct ComponentProjectileSpawnRequest : IComponentData
    {
        public int CollisionLayer;
        public float3 Position;
        public quaternion Rotation;
    }
    
    public struct ComponentProjectile : IComponentData
    {
        public float Speed;
        public float TimeTillDestroy;
    }

    [BurstCompile]
    public partial struct ProjectileSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.TryGetSingleton<ComponentDatabase>(out var db))
                throw new Exception($"An entity-component of type '{nameof(ComponentDatabase)}' is required in the subscene.");
            
            var qAllCollidersExceptProjectiles = SystemAPI
                .QueryBuilder()
                .WithAll<Collider, WorldTransform>()
                .WithNone<ComponentProjectile, ComponentPlayer>()
                .Build();

            var allColliders = qAllCollidersExceptProjectiles.ToComponentDataArray<Collider>(Allocator.TempJob);
            var allPositions = qAllCollidersExceptProjectiles.ToComponentDataArray<WorldTransform>(Allocator.TempJob);
            
            
            // Spawn projectiles from requests
            var dt = SystemAPI.Time.DeltaTime;
            var cb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            foreach (var (req, e) in SystemAPI.Query<ComponentProjectileSpawnRequest>().WithEntityAccess())
            {
                var transform = SystemAPI.GetComponent<WorldTransform>(db.ProjectilePrefab);
                
                var entity = cb.Instantiate(db.ProjectilePrefab);
                transform.worldTransform.position = req.Position;
                transform.worldTransform.rotation = req.Rotation;
                cb.AddComponent(entity, transform);
                
                cb.DestroyEntity(e);
            }
            cb.Playback(state.EntityManager);

            state.Dependency = new MoveProjectilesJob
            {
                DeltaTime = dt
            }.ScheduleParallel(state.Dependency);
            
            state.Dependency = new CheckCollisionJob
            {
                AllColliders = allColliders,
                EnemyPositions = allPositions
            }.ScheduleParallel(state.Dependency);

            cb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            state.Dependency = new DestroyProjectilesJob
            {
                CommandBuffer = cb.AsParallelWriter(),
                DeltaTime = dt
            }.ScheduleParallel(state.Dependency);
         
            state.Dependency.Complete(); // Wait here to ensure the projectiles are destroyed before the next frame
           
            cb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    partial struct MoveProjectilesJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(in ComponentProjectile projectile, ref WorldTransform position)
        {
            position.worldTransform.position += projectile.Speed * DeltaTime * position.forwardDirection;
        }
    }

    [BurstCompile]
    partial struct DestroyProjectilesJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public float DeltaTime;

        public void Execute([EntityIndexInQuery] int entityInQueryIndex, ref ComponentProjectile projectile, Entity entity)
        {
            projectile.TimeTillDestroy -= DeltaTime;
            if (projectile.TimeTillDestroy <= 0)
            {
                CommandBuffer.DestroyEntity(entityInQueryIndex, entity);
            }
        }
    }

    [BurstCompile]
    partial struct CheckCollisionJob : IJobEntity
    {
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Collider> AllColliders;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<WorldTransform> EnemyPositions;
        
        public void Execute(ref ComponentProjectile projectile, in Collider collider, in WorldTransform transform)
        {
            for (int i = 0; i < AllColliders.Length; i++)
            {
                var enemyCollider = AllColliders[i];
                var enemyPos = EnemyPositions[i];
                if (Physics.DistanceBetween(
                        in collider, 
                        in transform.worldTransform, 
                        enemyCollider, 
                        enemyPos.worldTransform, 
                        0f, 
                        out _))
                {
                    projectile.TimeTillDestroy = 0;
                    break;
                }
            }
        }
    }
}