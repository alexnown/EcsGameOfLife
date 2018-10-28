﻿using System.Linq;
using alexnown.EcsLife.Systems;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace alexnown.EcsLife
{
    public static class Bootstrap
    {


        public static BootstrapSettings Settings { get; private set; }
        public static int Width;
        public static int Height;
        public static int TotalCells;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize()
        {
            Settings = Resources.Load<BootstrapSettings>("BootstrapSettings");
            Width = (int)(Screen.width * Settings.ResolutionMultiplier);
            Height = (int)(Screen.height * Settings.ResolutionMultiplier);
            TotalCells = Width * Height;

            World.DisposeAllWorlds();
            InitializeInputWorld();
            var cellsWorld = InitializeCellsWorld();
            var em = World.Active.GetOrCreateManager<EntityManager>();
            em.AddSharedComponentData(em.CreateEntity(), cellsWorld);

            if (Settings.InitializeManualUpdate)
            {
                var autoUpdate = World.Active.GetOrCreateManager<UpdateCellWorldsSystem>();
                autoUpdate.MaxTimeLimitSec = 1f / Settings.PreferedFps;
                autoUpdate.MaxUpdatesForFrame = Settings.MaxWorldsUpdatesLimit;
                ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.Active);
            }
            else ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.AllWorlds.ToArray());
        }

        private static void InitializeInputWorld()
        {
            var inputWorld = new World("Input");
            World.Active = inputWorld;
            inputWorld.CreateManager<CreateSpraySourceFromInput>();
            var drawSystem = inputWorld.GetOrCreateManager<DrawCellsTextureSystem>();
            drawSystem.InitializeTexture(Width, Height);

            var drawer = new GameObject("DrawerOnGUI").AddComponent<DrawTextureOnGui>();
            drawer.RecieveTexture = () => drawSystem.GeneratedTexture;
        }

        private static CellsWorld InitializeCellsWorld()
        {
            var world = new World("CellWorld");
            world.CreateManager<ApplyFutureStatesSystem>();
            world.CreateManager<UpdateCellsLifeRulesSystem>();
            world.CreateManager<DisposeCellsArrayOnDestroyWorld>();
            var paintTexture = world.CreateManager<UpdateTextureColorsJobSystem>();
            world.CreateManager<ApplySprayPointsToCells>();
            var em = world.GetOrCreateManager<EntityManager>();

            var colors = new NativeArray<Color32>(3, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            colors[0] = new Color32();
            colors[1] = new Color32(0, Settings.GreenColor, 0, 0);
            colors[2] = new Color32(0, (byte)(Settings.GreenColor / 2), 0, 0);
            paintTexture.CellColorsByState = colors;

            var futureCellsState = new NativeArray<CellState>(TotalCells, Allocator.Persistent);
            var activeCellState = new NativeArray<CellState>(TotalCells, Allocator.Persistent);
            //var cellAlive = new CellState { State = 1 };
            //activeCellState[0] = activeCellState[1] = activeCellState[2] = activeCellState[2 + Width] = activeCellState[1 + 2 * Width] = cellAlive;

            var activeCellsDb = em.CreateEntity(ComponentType.Create<CellsDb>(), ComponentType.Create<CellsDbState>());
            var cellsDb = new CellsDb
            {
                UpdateRules = Settings.UpdateRules,
                Width = Width,
                Height = Height,
                CellsState0 = activeCellState,
                CellsState1 = futureCellsState
            };
            em.SetSharedComponentData(activeCellsDb, cellsDb);

            return new CellsWorld
            {
                Width = Width,
                Height = Height,
                World = world
            };
        }

    }
}
