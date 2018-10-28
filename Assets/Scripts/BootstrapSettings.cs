﻿using UnityEngine;

namespace alexnown.EcsLife
{
    [CreateAssetMenu(fileName = nameof(BootstrapSettings))]
    public class BootstrapSettings : ScriptableObject
    {
       
        public float ResolutionMultiplier => _resolutionMultiplier;
        public byte GreenColor => (byte)_greenColor;

        public bool InitializeManualUpdate => _initializeManualUpdate;
        public int MaxWorldsUpdatesLimit => _maxWorldsUpdatesLimit;
        public int PreferedFps => _preferedFps;

        [SerializeField] [Range(0.125f, 4)]
        private float _resolutionMultiplier = 1;
        [Range(10, 255)]
        [SerializeField]
        private int _greenColor = 255;

        [Header("ManualWorldsUpdate")]
        [SerializeField]
        private bool _initializeManualUpdate;
        [SerializeField]
        private int _maxWorldsUpdatesLimit = 100;
        [SerializeField]
        [Range(1, 60)]
        private int _preferedFps = 20;
    }
}

