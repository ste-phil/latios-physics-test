using HotRoof.PhysicsTest.Components;
using Latios.Transforms;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotRoof.PhysicsTest.Systems
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class PlayerCameraSystem : SystemBase
    {
        private static float2 cameraHeightRange = new float2(10, 30);
        private static float cameraHeight;
        private static float scrollSpeed = 3;
        private static float smoothTime = 0.3f;

        public static PlayerCameraSystem i { get; private set; }

        private Camera playerCamera;
        private Vector2 cameraVelocity = Vector2.zero;
        private float cameraVelocityY = 0;        
        private static float smoothTimeCameraY = 0.15f;


        private float2 shakeMagnitude;
        private float shakeDuration;

        private float2 shakeDirectionalMagnitude;
        private float shakeDirectionalDuration;

        protected override void OnCreate()
        {
            i = this;
            
            cameraHeight = cameraHeightRange.y;
        }

        protected override void OnUpdate()
        {
            playerCamera = Camera.main;
            foreach (var (ltw, input) in SystemAPI
                         .Query<RefRO<WorldTransform>, RefRO<ComponentPlayer>>())
            {
                // Smooth scroll wheel camera height adjustment
                var axisInput = UnityEngine.InputSystem.Mouse.current.scroll.ReadUnprocessedValue();
                cameraHeight -= axisInput.y * scrollSpeed;
                cameraHeight = math.clamp(cameraHeight, cameraHeightRange.x, cameraHeightRange.y);
                var newCameraHeight = Mathf.SmoothDamp(playerCamera.transform.position.y, cameraHeight, ref cameraVelocityY, smoothTimeCameraY);
                
                var camPosition = ((float3)playerCamera.transform.position).xz;
                Vector2 smoothedPosition = Vector2.SmoothDamp(
                    camPosition,
                    ltw.ValueRO.position.xz,
                    ref cameraVelocity,
                    smoothTime
                );

                ShakeXZ(
                    out var shakeOffset,
                    ref shakeDuration,
                    shakeMagnitude
                );

                playerCamera.transform.position = shakeOffset + new float3(
                    smoothedPosition.x,
                    newCameraHeight,
                    smoothedPosition.y
                );
            }

            foreach (var (ltw, core) in SystemAPI
                         .Query<RefRO<WorldTransform>, RefRO<ComponentPlayer>>())
            {
                var forward = ltw.ValueRO.forwardDirection;
                var rotWorldY = math.atan2(forward.x, forward.z);
                ShakeXZDirection(
                    out var shakeOffset,
                    ref shakeDirectionalDuration,
                    rotWorldY,
                    shakeDirectionalMagnitude
                );
            
                playerCamera.transform.position += (Vector3)shakeOffset;
            }
        }


        public void StartShake(float duration, float magnitude)
        {
            if (shakeDuration > 0)
                return;

            shakeDuration = duration;
            shakeMagnitude = magnitude;
        }

        public void StartLookingDirectionShake(float duration, float magnitude)
        {
            if (shakeDuration > 0)
                return;

            shakeDirectionalDuration = duration;
            shakeDirectionalMagnitude = new float2(magnitude * 4, magnitude);
        }

        private void ShakeXZ(out float3 shakePosition, ref float duration, float2 magnitude)
        {
            if (duration <= 0)
            {
                shakePosition = Vector3.zero;
                return;
            }

            var x = Random.Range(-1f, 1f) * magnitude.x;
            var z = Random.Range(-1f, 1f) * magnitude.y;

            shakePosition = new float3(x, 0, z);
            duration -= UnityEngine.Time.deltaTime;
        }

        private void ShakeXZDirection(out float3 shakePosition, ref float duration, float angleRad, float2 magnitude)
        {
            if (duration <= 0)
            {
                shakePosition = float3.zero;
                return;
            }

            //half circle x [0, 1] ~ rd(0, 1)
            //            y [-1, 1] ~ exp(8, [-1, 1])
            var position = Random.insideUnitCircle;
            position.x = math.abs(position.x);
            var sign = math.sign(position.y);
            position.y = math.pow(8, position.y * -sign) * sign;

            position.x *= magnitude.x;
            position.y *= magnitude.y;

            // Rotate shake vector by input angle
            var cosTheta = math.cos(angleRad + math.PI * .5f);
            var sinTheta = math.sin(angleRad - math.PI * .5f);

            var rotatedPosX = position.x * cosTheta - position.y * sinTheta;
            var rotatedPosY = position.x * sinTheta + position.y * cosTheta;

            shakePosition = new float3(rotatedPosX, 0, rotatedPosY);
            duration -= UnityEngine.Time.deltaTime;
        }
    }
}