﻿using NUnit.Framework;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static GameOfLife.ConwaysWorldUtils;

namespace GameOfLife
{
    public class ConwaysWorldCorrectnessTests
    {
        private Tuple<int, int>[] ConwaysUpdateResults = new[]
        {
            Tuple.Create(0b011000001000010100000000,0b011000000100001000000000),
            Tuple.Create(0b011000000100001000000000,0b001000010100000000000000),
            Tuple.Create(0b001000010100000000000000,0b001000001000000000000000),
            Tuple.Create(0b110000110000000,0b110000110000000), //Square
            Tuple.Create(0b1110000000000000,0b100000100000100000000), // Horizontal 3-cells line
            Tuple.Create(0b100010100001100000000,0b1000000110001100000000), // Glider0
            Tuple.Create(0b1000000110001100000000,0b100000010001110000000), // Glider1
        };

        private int4x3[] OscillatorSequence = new[]
        {
            new int4x3(new int4(0, 2637952, 2162688, 0), new int4(1089792, 2130432, 2138624, 4228096), new int4(0, 541312, 66048, 0)),
            new int4x3(new int4(0, 2637952, 2162688, 0), new int4(1089792, 2146816, 2204160, 4228096), new int4(0, 541312, 66048, 0)),
            new int4x3(new int4(0, 2637952, 2162688, 0), new int4(1089792, 2671232, 2204160, 4228096), new int4(0, 541312, 66048, 0)),
            new int4x3(new int4(0, 2637952, 2162688, 0), new int4(1089792, 2654848, 2138624, 4228096), new int4(0, 541312, 66048, 0))
        };

        private NativeArray<int> InitializeSourceStatesArray()
        {
            var states = new NativeArray<int>(ConwaysUpdateResults.Length, Allocator.TempJob);
            for (int i = 0; i < ConwaysUpdateResults.Length; i++)
                states[i] = ConwaysUpdateResults[i].Item1;
            return states;
        }

        private void CheckAreaUpdatedStatesAndDispose(NativeArray<int> results)
        {
            for (int i = 0; i < results.Length; i++)
                Assert.AreEqual(ConwaysUpdateResults[i].Item2, results[i]);
            results.Dispose();
        }

        [Test]
        public void UpdateConwaysArea_WithoutShiftToFirstBit()
        {
            var states = InitializeSourceStatesArray();
            new UpdateAreaCells_WithoutShiftToFirstBitJob
            {
                CellStates = states
            }.Schedule(states.Length, 1).Complete();
            CheckAreaUpdatedStatesAndDispose(states);
        }

        [Test]
        public void UpdateConwaysArea_ManualShiftJob()
        {
            var states = InitializeSourceStatesArray();
            new UpdateAreaCells_ManualShiftJob
            {
                CellStates = states
            }.Schedule(states.Length, 1).Complete();
            CheckAreaUpdatedStatesAndDispose(states);
        }

        [Test]
        public void UpdateConwaysArea_ShiftByDivisionJob()
        {
            var states = InitializeSourceStatesArray();
            new UpdateAreaCells_ShiftByDivisionJob
            {
                CellStates = states
            }.Schedule(states.Length, 1).Complete();
            CheckAreaUpdatedStatesAndDispose(states);
        }

        [Test]
        public void UpdateConwaysArea_ShiftByMathJob()
        {
            var states = InitializeSourceStatesArray();
            new UpdateAreaCells_ShiftByMathJob
            {
                CellStates = states
            }.Schedule(states.Length, 1).Complete();
            CheckAreaUpdatedStatesAndDispose(states);
        }

        [Test]
        public void UpdateConwaysArea_ShiftNeighborsCountJob()
        {
            var states = InitializeSourceStatesArray();
            new UpdateAreaCells_ShiftNeighborsCountJob
            {
                CellStates = states
            }.Schedule(states.Length, 1).Complete();
            CheckAreaUpdatedStatesAndDispose(states);
        }
        [TestCase(4)]
        [TestCase(8)]
        [TestCase(12)]
        public void SetHorizontalSidesJob(int areasCount)
        {
            var states = new NativeArray<int4>(areasCount / 4, Allocator.TempJob);
            for (int i = 0; i < states.Length; i++) states[i] = 0b10010000000010000000;
            new SetHorizontalSidesInAreasJob { Width = states.Length, CellStates = states }.Schedule(1, 1).Complete();
            var cellStates = states.Reinterpret<int>(UnsafeUtility.SizeOf<int4>());
            Assert.AreEqual(594048, cellStates[0]);
            for (int i = 1; i < cellStates.Length - 1; i++)
                Assert.AreEqual(8984704, cellStates[i]);
            Assert.AreEqual(8980608, cellStates[cellStates.Length - 1]);
        }

        [TestCase(4)]
        [TestCase(8)]
        [TestCase(12)]
        public void SetVerticalSidesJob(int areasCount)
        {
            var states = new NativeArray<int4>(areasCount / 4, Allocator.TempJob);
            for (int i = 0; i < states.Length; i++) states[i] = 0b101001000000010110000000;
            new SetVerticalSidesInAreasJob { Width = 1, CellStates = states }.Schedule(1, 1).Complete();
            if (states.Length == 1) Assert.AreEqual((int4)0b101001000000010110000000, states[0]);
            else
            {
                Assert.AreEqual((int4)379848064, states[0]);
                for (int i = 1; i < states.Length - 1; i++)
                    Assert.AreEqual((int4)379848105, states[i]);
                Assert.AreEqual((int4)10749353, states[states.Length - 1]);
            }
        }

        [Test]
        public void CheckNeighborAreasOnConwaysUpdate()
        {
            var areas = new NativeArray<int>(16, Allocator.TempJob);
            areas[5] = 0b100010100001100000000;// glider
            var job = new UpdateAreaCells_ManualShiftJob
            {
                CellStates = areas
            }.Schedule(areas.Length, 256);
            var statesInt4 = areas.Reinterpret<int4>(UnsafeUtility.SizeOf<int>());
            job = new SetHorizontalSidesInAreasJob
            {
                Width = 1,
                CellStates = statesInt4
            }.Schedule(4, 32, job);
            job = new SetVerticalSidesInAreasJob
            {
                Width = 1,
                CellStates = statesInt4
            }.Schedule(1, 32, job);
            job.Complete();
            for (int i = 0; i < areas.Length; i++)
            {
                if (i == 1) Assert.AreEqual(201326592, areas[i]);
                else if (i == 5) Assert.AreEqual(0b1000000110001100000000, areas[i]);
                else if (i == 6) Assert.AreEqual(131072, areas[i]);
                else if (i == 9) Assert.AreEqual(8, areas[i]);
                else Assert.AreEqual(0, areas[i]);
            }
        }

        [Test]
        public void UnpackInConwaysWorldCorrectness()
        {
            var areasInt4 = UnpackToConwaysWorld(OscillatorSequence[0]);
            Assert.AreEqual(new int4(67108864, 136859776, 161548288, 268435456), areasInt4[0]);
            Assert.AreEqual(new int4(1089792, 170033674, 673489512, 4359168), areasInt4[1]);
            Assert.AreEqual(new int4(4, 545416, 8456713, 16), areasInt4[2]);
            areasInt4.Dispose();
        }

        [Test]
        public void OscillatorInConwaysWorld()
        {
            var areasInt4 = UnpackToConwaysWorld(OscillatorSequence[0]);
            var areas = areasInt4.Reinterpret<int>(UnsafeUtility.SizeOf<int4>());
            new UpdateAreaCells_ManualShiftJob { CellStates = areas }.Run(areas.Length);
            new SetHorizontalSidesInAreasJob { Width = 1, CellStates = areasInt4 }.Run(areasInt4.Length);
            new SetVerticalSidesInAreasJob { Width = 1, CellStates = areasInt4 }.Run(1);

            var oscillatorNextStepAreas = UnpackToConwaysWorld(OscillatorSequence[1]);
            Assert.AreEqual(oscillatorNextStepAreas[0], areasInt4[0]);
            Assert.AreEqual(oscillatorNextStepAreas[1], areasInt4[1]);
            Assert.AreEqual(oscillatorNextStepAreas[2], areasInt4[2]);
            oscillatorNextStepAreas.Dispose();

            new UpdateAreaCells_ManualShiftJob { CellStates = areas }.Run(areas.Length);
            new SetHorizontalSidesInAreasJob { Width = 1, CellStates = areasInt4 }.Run(areasInt4.Length);
            new SetVerticalSidesInAreasJob { Width = 1, CellStates = areasInt4 }.Run(1);
            oscillatorNextStepAreas = UnpackToConwaysWorld(OscillatorSequence[2]);
            Assert.AreEqual(oscillatorNextStepAreas[0], areasInt4[0]);
            Assert.AreEqual(oscillatorNextStepAreas[1], areasInt4[1]);
            Assert.AreEqual(oscillatorNextStepAreas[2], areasInt4[2]);
            oscillatorNextStepAreas.Dispose();

            new UpdateAreaCells_ManualShiftJob { CellStates = areas }.Run(areas.Length);
            new SetHorizontalSidesInAreasJob { Width = 1, CellStates = areasInt4 }.Run(areasInt4.Length);
            new SetVerticalSidesInAreasJob { Width = 1, CellStates = areasInt4 }.Run(1);
            oscillatorNextStepAreas = UnpackToConwaysWorld(OscillatorSequence[3]);
            Assert.AreEqual(oscillatorNextStepAreas[0], areasInt4[0]);
            Assert.AreEqual(oscillatorNextStepAreas[1], areasInt4[1]);
            Assert.AreEqual(oscillatorNextStepAreas[2], areasInt4[2]);
            oscillatorNextStepAreas.Dispose();

            new UpdateAreaCells_ManualShiftJob { CellStates = areas }.Run(areas.Length);
            new SetHorizontalSidesInAreasJob { Width = 1, CellStates = areasInt4 }.Run(areasInt4.Length);
            new SetVerticalSidesInAreasJob { Width = 1, CellStates = areasInt4 }.Run(1);
            oscillatorNextStepAreas = UnpackToConwaysWorld(OscillatorSequence[0]);
            Assert.AreEqual(oscillatorNextStepAreas[0], areasInt4[0]);
            Assert.AreEqual(oscillatorNextStepAreas[1], areasInt4[1]);
            Assert.AreEqual(oscillatorNextStepAreas[2], areasInt4[2]);
            oscillatorNextStepAreas.Dispose();
            areasInt4.Dispose();
        }

        private NativeArray<int4> UnpackToConwaysWorld(int4x3 areaForm)
        {
            var areas = new NativeArray<int4>(3, Allocator.TempJob);
            areas[0] = areaForm.c0;
            areas[1] = areaForm.c1;
            areas[2] = areaForm.c2;
            new SetHorizontalSidesInAreasJob { Width = 1, CellStates = areas }.Run(areas.Length);
            new SetVerticalSidesInAreasJob { Width = 1, CellStates = areas }.Run(1);
            return areas;
        }

    }
}