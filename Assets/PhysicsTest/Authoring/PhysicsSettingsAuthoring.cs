using Latios.Psyshock;
using Latios.Psyshock.Anna;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HotRoof.PhysicsTest.Authoring
{
    public class PhysicsSettingsAuthoring : MonoBehaviour
    {
        public Aabb Bounds;
        public int3 SubDivisions;
        
        public float3 Gravity = new float3(0f, -9.81f, 0f);
        public float LinearDamping = 0.3f;
        public float AngularDamping = 0.3f;

        public class PhysicsSettingsBaker : Baker<PhysicsSettingsAuthoring>
        {
            public override void Bake(PhysicsSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity,
                    new PhysicsSettings
                    {
                        collisionLayerSettings = new CollisionLayerSettings
                        {
                            worldSubdivisionsPerAxis = authoring.SubDivisions,
                            worldAabb = authoring.Bounds
                        },
                        gravity = authoring.Gravity,
                        linearDamping = (half)authoring.LinearDamping,
                        angularDamping = (half)authoring.AngularDamping
                    }
                );
            }
        }
    }
}