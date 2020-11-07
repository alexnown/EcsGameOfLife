﻿using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GameOfLife
{

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class DrawCellsWorldToTexture : SystemBase
    {
        [BurstCompile]
        private struct UpdateTexture : IJobParallelFor
        {
            [NativeDisableContainerSafetyRestriction]
            public DynamicBuffer<DrawCellsOnGuiColor> Colors;
            public int2 Size;
            public int WidthInAreas;
            [ReadOnly]
            public NativeArray<int> CellStates;
            [WriteOnly]
            [NativeDisableParallelForRestriction]
            public NativeArray<byte> TargetTextureArray;

            public void Execute(int index)
            {
                var area = CellStates[index];
                var pos = new int2(index % WidthInAreas, index / WidthInAreas) * new int2(4, 3);
                var states = ConwaysWorldUtils.rightShift(area, ConwaysWorldUtils.CellShifts) & 1;
                //var textureIndex = pos.x + (Size.y - pos.y - 1) * Size.x;
                var textureIndex = pos.x + pos.y * Size.x;
                for (int j = 0; j < 3; j++)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var state = states[j][i];
                        var colorData = Colors[state].Color;
                        var texturePixel = textureIndex + (2 - j) * Size.x + i;
                        //UnityEngine.Debug.Log($"Set pos={pos} to textureIndex={textureIndex}, pixelPos = {texturePixel}");
                        TargetTextureArray[3 * texturePixel] = colorData.r;
                        TargetTextureArray[3 * texturePixel + 1] = colorData.g;
                        TargetTextureArray[3 * texturePixel + 2] = colorData.b;
                    }
                }
            }
        }

        private readonly Stopwatch _timer = new Stopwatch();

        protected override void OnUpdate()
        {
            Entities.ForEach((DrawTextureOnGui drawer) =>
            {
                var texture = drawer.Texture;
                Entities.ForEach((ref CellsInAreas cells, in DynamicBuffer<DrawCellsOnGuiColor> colors) =>
                {
                    if (texture == null)
                    {
                        texture = new Texture2D(cells.Size.x, cells.Size.y, TextureFormat.RGB24, false);
                        drawer.Texture = texture;
                        drawer.enabled = true;
                    }
                    else if (texture.width != cells.Size.x || texture.height != cells.Size.y)
                    {
                        texture.Resize(cells.Size.x, cells.Size.y);
                    }
                    var sizeInAreas = cells.Size / new int2(4, 3);
                    var job = new UpdateTexture
                    {
                        TargetTextureArray = texture.GetRawTextureData<byte>(),
                        CellStates = cells.Areas.Value.ArrayPtr.Value,
                        Colors = colors,
                        Size = cells.Size,
                        WidthInAreas = sizeInAreas.x
                    }.Schedule(sizeInAreas.x * sizeInAreas.y, 1024);
                    job.Complete();
                    texture.Apply(false);
                }).WithoutBurst().Run();
            }).WithoutBurst().Run();
        }
    }
}
