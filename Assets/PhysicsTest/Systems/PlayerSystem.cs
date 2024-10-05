using HotRoof.PhysicsTest.Components;
using Latios.Psyshock.Anna;
using Latios.Transforms;
using Unity.Entities;
using Unity.Mathematics;

namespace HotRoof.PhysicsTest.Systems
{
    public partial class PlayerSystem : SystemBase
    {
        private float reloadTimer = 0.2f;
        private float screenShakeMagnitude = 0.01f;
        
        
        protected override void OnUpdate()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;

            var wPressed = keyboard.wKey.isPressed;
            var aPressed = keyboard.aKey.isPressed;
            var sPressed = keyboard.sKey.isPressed;
            var dPressed = keyboard.dKey.isPressed;

            
            var cb = new EntityCommandBuffer(WorldUpdateAllocator);
            foreach (var (player, transform, rigidBody, addImpulseBuffer) in SystemAPI
                         .Query<ComponentPlayer, TransformAspect, RefRW<RigidBody>, DynamicBuffer<AddImpulse>>())
            {
                var change = new float3(
                    (aPressed ? -1 : 0) + (dPressed ? 1 : 0),
                    0,
                    (sPressed ? -1 : 0) + (wPressed ? 1 : 0)
                );
                
                //apply damping if no input is given
                if (math.all(change == 0))
                {
                    var damping = 0.02f;
                    var dampingForce = -rigidBody.ValueRW.velocity.linear.xz * damping;
                    addImpulseBuffer.Add(
                        new AddImpulse(new float3(dampingForce.x, 0, dampingForce.y))
                    );
                }
                
                rigidBody.ValueRW.velocity.angular = float3.zero;
                
                var totalSpeed = math.length(rigidBody.ValueRW.velocity.linear);
                if (totalSpeed > 30 || float.IsNaN(totalSpeed))
                {
                    rigidBody.ValueRW.velocity.linear = math.normalize(rigidBody.ValueRW.velocity.linear);
                }

                if (math.any(change))
                {
                    var newVelocity = rigidBody.ValueRW.velocity.linear + math.normalize(change) * player.Speed;
                    var speed = math.length(newVelocity);
                    speed = math.clamp(speed, 0, 10);
                    rigidBody.ValueRW.velocity.linear = math.normalize(newVelocity) * speed;
                }
                
                //
                // if (math.any(change))
                // {
                //     addImpulseBuffer.Add(
                //         new AddImpulse(math.normalize(change) * player.Speed * rigidBody.ValueRW.inverseMass)
                //     );
                // }
                //
                // transform.worldPosition += player.Speed * SystemAPI.Time.DeltaTime * change;

                // Look at mouse
                var mousePosition = mouse.position.ReadValue();
                var screenPoint = UnityEngine.Camera.main.WorldToScreenPoint(transform.worldPosition);
                var direction = new float3(mousePosition.x - screenPoint.x, mousePosition.y - screenPoint.y, 0);
                var angle = math.atan2(direction.x, direction.y);
                transform.worldRotation = quaternion.RotateY(angle);
                
                reloadTimer -= SystemAPI.Time.DeltaTime;
                if (mouse.leftButton.isPressed && reloadTimer <= 0)
                {
                    reloadTimer = 0.2f;
                    var entity = cb.CreateEntity();
                    cb.AddComponent(entity, new ComponentProjectileSpawnRequest
                    {
                        CollisionLayer = 1 << 1,
                        Position = transform.worldPosition,
                        Rotation = quaternion.RotateY(angle)
                    });
                    
                    PlayerCameraSystem.i.StartLookingDirectionShake(0.1f, screenShakeMagnitude);
                }
            }
            
            cb.Playback(EntityManager);
        }
    } 
}

