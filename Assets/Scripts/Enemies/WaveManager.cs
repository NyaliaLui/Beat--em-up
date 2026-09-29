using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ironhold
{
    /// <summary>
    /// Endless escalating waves (section 8). Spawns over time up to the concurrent cap, grows
    /// count / shrinks interval / scales stats per wave, mixes enemy types by wave band, spawns
    /// from one side early and both edges from wave 3, fires announcer lines, and runs a breather
    /// between cleared waves. A wave is cleared when nothing is left to spawn and none are alive.
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        public int CurrentWave { get; private set; }
        public bool Running { get; private set; }
        public int AliveCount => _alive.Count;  // drives the music intensity layer
        public bool IsBossWave => GameConfig.IsBossWave(CurrentWave);
        public bool IsFrenzyWave => !IsBossWave && CurrentWave > 0 && CurrentWave % GameConfig.FrenzyWaveEvery == 0;
        public EnemyBase ActiveBoss { get; private set; }   // HUD boss bar reads this
        public event Action<int> WaveStarted;   // HUD shows the "WAVE n" banner
        public event Action<float> BossDefeated; // seconds from the boss's first hit to its death

        private Transform _player;
        private readonly List<EnemyBase> _alive = new List<EnemyBase>();
        private int _remainingToSpawn;
        private float _spawnInterval;
        private float _spawnTimer;
        private bool _inBreather;
        private float _breatherTimer;

        public void Configure(Transform player) => _player = player;

        public void BeginRun()
        {
            ClearAllEnemies();
            Running = true;
            _inBreather = false;
            CurrentWave = 0;
            StartWave(1);
        }

        public void StopRun()
        {
            Running = false;
            _inBreather = false;
        }

        public void ClearAllEnemies()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
                if (_alive[i] != null) Destroy(_alive[i].gameObject);
            _alive.Clear();
            ActiveBoss = null;
        }

        private void StartWave(int w)
        {
            CurrentWave = w;
            // Boss waves are a lone duel so the speed-kill clock measures the player, not the crowd.
            _remainingToSpawn = GameConfig.IsBossWave(w) ? 1 : GameConfig.EnemiesInWave(w);
            _spawnInterval = GameConfig.SpawnInterval(w);
            _spawnTimer = 0f;
            _inBreather = false;

            var gm = GameManager.Instance;
            gm?.Sfx?.Play(SfxManager.WaveStart);
            if (w == 2) gm?.Announcer?.Play(AnnouncerVO.Line.Wave2);
            else if (w == GameConfig.BruteFirstWave) gm?.Announcer?.Play(AnnouncerVO.Line.WaveBrute);
            else if (w % 5 == 0) gm?.Announcer?.Play(AnnouncerVO.Line.WaveMilestone);

            WaveStarted?.Invoke(w);
        }

        private void Update()
        {
            if (!Running) return;

            if (_inBreather)
            {
                _breatherTimer -= Time.deltaTime;
                if (_breatherTimer <= 0f) StartWave(CurrentWave + 1);
                return;
            }

            _spawnTimer -= Time.deltaTime;
            int cap = GameConfig.ConcurrentAlive(CurrentWave);
            while (_remainingToSpawn > 0 && _alive.Count < cap && _spawnTimer <= 0f)
            {
                SpawnOne();
                _spawnTimer = _spawnInterval;
                _remainingToSpawn--;
            }

            // Prune any null (destroyed) references defensively, then test for wave clear.
            for (int i = _alive.Count - 1; i >= 0; i--)
                if (_alive[i] == null) _alive.RemoveAt(i);

            if (_remainingToSpawn == 0 && _alive.Count == 0)
                WaveCleared();
        }

        private void WaveCleared()
        {
            var gm = GameManager.Instance;
            gm?.Score?.AddWaveBonus(CurrentWave);
            gm?.Sfx?.Play(SfxManager.WaveClear);
            gm?.Announcer?.Play(AnnouncerVO.Line.WaveCleared);
            _inBreather = true;
            _breatherTimer = GameConfig.WaveBreather;
        }

        private void SpawnOne()
        {
            EnemyType type = IsBossWave ? EnemyType.Boss : PickType(CurrentWave);
            float scale = GameConfig.WaveStatScale(CurrentWave);
            EnemyStats stats = EnemyStats.For(type, scale);
            if (IsFrenzyWave) stats.ApplyFrenzy();
            if (!stats.IsBoss && CurrentWave >= GameConfig.EliteFirstWave && UnityEngine.Random.value < GameConfig.EliteChance)
                stats.MakeElite();

            float spawnX;
            if (CurrentWave < 3)
                spawnX = GameConfig.SpawnLeftX;
            else
                spawnX = (UnityEngine.Random.value < 0.5f) ? GameConfig.SpawnLeftX : GameConfig.SpawnRightX;

            EnemyBase e = Spawner.Spawn(stats, _player, spawnX, this);
            if (e != null)
            {
                _alive.Add(e);
                if (stats.IsBoss) ActiveBoss = e;
            }
        }

        public void NotifyEnemyDead(EnemyBase e)
        {
            _alive.Remove(e);
            if (e != null && e == ActiveBoss)
            {
                ActiveBoss = null;
                float first = e.Health != null ? e.Health.FirstHitTime : -1f;
                if (first >= 0f) BossDefeated?.Invoke(Time.time - first);
            }
        }

        private static EnemyType PickType(int w)
        {
            float r = UnityEngine.Random.value;
            if (w == 1) return EnemyType.Grunt;
            if (w <= 3) return r < 0.6f ? EnemyType.Grunt : EnemyType.Skeleton;
            if (w <= 6)
            {
                if (r < 0.45f) return EnemyType.Grunt;
                if (r < 0.80f) return EnemyType.Skeleton;
                return EnemyType.Brute;
            }
            if (r < 0.35f) return EnemyType.Grunt;
            if (r < 0.70f) return EnemyType.Skeleton;
            return EnemyType.Brute;
        }
    }
}
