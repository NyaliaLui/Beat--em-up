using System;
using UnityEngine;

namespace Ironhold
{
    public enum GameState { Menu, Playing, Paused, GameOver, Promo }

    /// <summary>
    /// Central run coordinator: owns the state machine (Menu / Playing / Paused / GameOver),
    /// wires the systems together and drives the start / pause / game-over / restart flow.
    /// Built and populated entirely in code by Bootstrap; there is one instance per session.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;
        public event Action<GameState> StateChanged;

        // Systems (assigned by Bootstrap once everything is built).
        public PlayerController Player;
        public PlayerHealth PlayerHealth;
        public PlayerStamina PlayerStamina;
        public WaveManager Waves;
        public ScoreManager Score;
        public AnnouncerVO Announcer;
        public SfxManager Sfx;
        public CameraFollow Camera;

        // UI controllers.
        public HUDController Hud;
        public MenuController Menu;
        public PauseController Pause;
        public GameOverController GameOverUI;
        public PromoPopupController PromoUI;
        public EasterEggManager EasterEggs;

        private float _deathTimer = -1f;

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (_deathTimer > 0f)
            {
                _deathTimer -= Time.unscaledDeltaTime;
                if (_deathTimer <= 0f)
                {
                    _deathTimer = -1f;
                    EnterGameOver();
                }
            }
        }

        public void GoToMenu()
        {
            SetState(GameState.Menu);
            Time.timeScale = 1f;
        }

        public void StartRun()
        {
            Time.timeScale = 1f;
            _deathTimer = -1f;
            Impact.CancelHitStop();
            AttackDirector.Clear();
            Score.ResetRun();
            PlayerHealth.ResetActor();
            PlayerStamina.ResetActor();
            Player.ResetActor();
            Waves.BeginRun();
            EasterEggs?.ResetRun();
            SetState(GameState.Playing);
            Announcer.Play(AnnouncerVO.Line.RunStart);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) PauseGame();
            else if (State == GameState.Paused) ResumeGame();
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            Impact.CancelHitStop(); // never let a running hit-stop restore timeScale under the menu
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>Freeze the fight under an easter-egg popup. Only from live gameplay.</summary>
        public bool OpenPromo(PromoPopupController.Promo promo)
        {
            if (State != GameState.Playing || _deathTimer > 0f || PromoUI == null) return false;
            Impact.CancelHitStop();
            // The controls hide under the popup, so release any held move/guard buttons now.
            Player?.OnMoveLeftUp();
            Player?.OnMoveRightUp();
            Player?.OnBlockUp();
            Time.timeScale = 0f;
            SetState(GameState.Promo);
            PromoUI.Show(promo);
            return true;
        }

        public void ClosePromo()
        {
            if (State != GameState.Promo) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        /// <summary>Called by PlayerHealth when HP hits 0. Slow-mo beat, then the Game Over screen.</summary>
        public void OnPlayerDied()
        {
            if (State != GameState.Playing) return;
            Impact.CancelHitStop();
            Time.timeScale = 0.25f;      // death reads in slow motion (death timer is unscaled)
            Camera?.KillPunch(1f);       // dolly onto the falling hero
            Announcer.Play(AnnouncerVO.Line.GameOver);
            _deathTimer = GameConfig.PlayerDeathDelay;
        }

        private void EnterGameOver()
        {
            Time.timeScale = 1f;
            Waves.StopRun();
            Score.CommitBest();
            SetState(GameState.GameOver);
        }

        public void Restart()
        {
            Waves.StopRun();
            Waves.ClearAllEnemies();
            StartRun();
        }

        public void QuitToMenu()
        {
            Waves.StopRun();
            Waves.ClearAllEnemies();
            GoToMenu();
        }

        /// <summary>Called by an enemy on death: award score (popped at the kill), combo lines + sfx.</summary>
        public void RegisterEnemyKilled(int basePoints, Vector3 worldPos)
        {
            if (Score == null) return;
            int prevMult = Score.Multiplier;
            int awarded = Score.AddKill(basePoints);
            DamageNumbers.Score(worldPos, awarded);
            int newMult = Score.Multiplier;
            if (prevMult < 3 && newMult >= 3) Announcer?.Play(AnnouncerVO.Line.ComboX3);
            if (prevMult < 5 && newMult >= 5) Announcer?.Play(AnnouncerVO.Line.ComboX5);
            if (newMult > prevMult) Sfx?.Play(SfxManager.ComboUp);
        }

        private void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }
    }
}
