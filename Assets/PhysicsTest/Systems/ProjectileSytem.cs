using System;
using System.Linq;
using HotRoof.PhysicsTest.Components;
using Latios.Psyshock;
using Latios.Psyshock.Anna;
using Latios.Systems;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.Rendering;
using UnityEngine;
using BoxCollider = Latios.Psyshock.BoxCollider;
using Collider = Latios.Psyshock.Collider;
using Physics = Latios.Psyshock.Physics;
using SphereCollider = Latios.Psyshock.SphereCollider;

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
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ComponentDatabase>();
        }

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
            
            
            var qProjectiles = SystemAPI
                .QueryBuilder()
                .WithAll<ComponentProjectile, WorldTransform, Collider>()
                .Build();

            var projectiles = qProjectiles.ToComponentDataArray<ComponentProjectile>(Allocator.TempJob);
            var projectilesColliders = qProjectiles.ToComponentDataArray<Collider>(Allocator.TempJob);
            var projectileTransforms = qProjectiles.ToComponentDataArray<WorldTransform>(Allocator.TempJob);

            // m_handle.Update(ref state);
            // var settings = new CollisionLayerSettings
            // {
            //     worldAabb = new Aabb(-db.MapSize, db.MapSize), 
            //     worldSubdivisionsPerAxis = new int3((int) db.MapSize / 8, 1, (int)db.MapSize / 8),
            // };
            //
            // state.Dependency = Physics
            //     .BuildCollisionLayer(m_allColliders, in m_handle)
            //     .WithSettings(settings)
            //     .ScheduleParallel(out var layer, Allocator.TempJob, state.Dependency);
            
            
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
            
            // Debug.Log("All:" + allColliders.Length);
            // Debug.Log("Proj:" + SystemAPI.QueryBuilder().WithAll<ComponentProjectile>().Build().CalculateEntityCount());
            state.Dependency = new CheckCollisionJob
            {
                AllColliders = allColliders,
                AllColliderPositions = allPositions
            }.ScheduleParallel(state.Dependency);
            
            // state.Dependency = new CheckCollisionWithEnemyJob()
            // {
            //     Projectiles = projectiles,
            //     ProjectileColliders = projectilesColliders,
            //     ProjectileTransforms = projectileTransforms
            // }.ScheduleParallel(state.Dependency);

            
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
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<WorldTransform> AllColliderPositions;
        
        public void Execute(ref ComponentProjectile projectile, in Collider projCollider, in WorldTransform projTransform)
        {
            for (int i = 0; i < AllColliders.Length; i++)
            {
                var col = AllColliders[i];
                var pos = AllColliderPositions[i];
                var projectilePos = GetColliderCenterWorld(in projCollider, in projTransform);
                // if (Physics.DistanceBetween(
                //         in projCollider, 
                //         in projTransform.worldTransform, 
                //         in col, 
                //         in pos.worldTransform, 
                //         0f, 
                //         out _))
                // {
                //     projectile.TimeTillDestroy = 0;
                //     break;
                // }
                if (Overlaps(in projectilePos, in col, in pos))
                {
                    projectile.TimeTillDestroy = 0;
                    break;
                }
            }
        }

        public float3 GetColliderCenterWorld(in Collider collider, in WorldTransform colTransform)
        {
            switch (collider.type)
            {
                case ColliderType.Sphere:
                    var sphere = (SphereCollider)collider;
                    return colTransform.worldTransform.position + sphere.center * colTransform.scale;
                case ColliderType.Box:
                    var box = (BoxCollider)collider;
                    return colTransform.worldTransform.position + box.center * colTransform.scale;
                default:
                    throw new NotImplementedException();
            }
        }
        
        public bool Overlaps(in float3 point, in Collider collider, in WorldTransform colTransform)
        {
            switch (collider.type)
            {
                case ColliderType.Sphere:
                    var sphere = (SphereCollider)collider;
                    var distance = math.distance(point, colTransform.worldTransform.position);
                    return distance <= sphere.radius * colTransform.scale;
                case ColliderType.Box:
                    var box = (BoxCollider)collider;
                    var localPoint = math.mul(math.inverse(colTransform.worldTransform.rotation), point - colTransform.worldTransform.position);
                    var collides = math.all(math.abs(localPoint) <= (box.halfSize + box.center) * colTransform.scale);
                    // if (collides)
                    //     Debug.Log("Collides: " + point + " " + colTransform.worldTransform.position);
                    // return false;
                    return collides;
                default:
                    throw new NotImplementedException();
            }
        }
    }

    [BurstCompile]
    partial struct CheckCollisionWithEnemyJob : IJobEntity
    {
        [DeallocateOnJobCompletion, WriteOnly, NativeDisableParallelForRestriction] 
        public NativeArray<ComponentProjectile> Projectiles;
        [DeallocateOnJobCompletion, ReadOnly] 
        public NativeArray<WorldTransform> ProjectileTransforms;
        [DeallocateOnJobCompletion, ReadOnly] 
        public NativeArray<Collider> ProjectileColliders;
        
        public void Execute(ref ComponentEnemy enemy, in Collider collider, in WorldTransform transform)
        {
            for (int i = 0; i < Projectiles.Length; i++)
            {
                ref var proj = ref Projectiles.AsSpan()[i];
                var col = ProjectileColliders[i];
                var pos = ProjectileTransforms[i];
                if (Physics.DistanceBetween(
                        in collider, 
                        in transform.worldTransform, 
                        col, 
                        pos.worldTransform, 
                        0f, 
                        out _))
                {
                    proj.TimeTillDestroy = 0;
                    break;
                }
            }
        }
    }

    [BurstCompile]
    partial struct CheckCollisionWithEnvironmentJob : IJobEntity
    {
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<Collider> AllColliders;
        [DeallocateOnJobCompletion, ReadOnly] public NativeArray<WorldTransform> AllColliderPositions;

        public void Execute(ref ComponentProjectile projectile, in Collider collider, in WorldTransform transform)
        {
            for (int i = 0; i < AllColliders.Length; i++)
            {
                var col = AllColliders[i];
                var pos = AllColliderPositions[i];
                if (Physics.DistanceBetween(
                        in collider,
                        in transform.worldTransform,
                        col,
                        pos.worldTransform,
                        0f,
                        out _
                    ))
                {
                    projectile.TimeTillDestroy = 0;
                    break;
                }
            }
        }
    }
}