//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using Unity.Entities;
//using Unity.Transforms;
//using Unity.Mathematics;
//using Unity.Collections;

//partial class FluidSystem : SystemBase
//{
//    private EndSimulationEntityCommandBufferSystem commandBufferSystem;

//    protected override void OnCreate()
//    {
//        base.OnCreate();
//        commandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
//    }
//    protected override void OnUpdate()
//    {
//        //NativeArray<VoxDenisty> voxDensit = GetEntityQuery(typeof(VoxDenisty)).ToComponentDataArray<VoxDenisty>(Allocator.TempJob);

//        // DIFFUSE
//        //
//        Entities.ForEach
//        ((  ref VoxDenisty translation,
//            in VoxVector velocity) =>
//        {
           
//        })
//        .Schedule();
//    }
//}

//public struct Voxel : IComponentData
//{
//    float3 pos;
//}

//public struct VoxDenisty : IComponentData
//{
//    float density;
//}

//public struct VoxVector : IComponentData
//{
//    float3 vec;
//}
