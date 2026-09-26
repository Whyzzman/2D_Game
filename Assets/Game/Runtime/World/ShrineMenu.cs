using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// The altar's menu: rest, travel, or leave. This is its behaviour only; drawing it waits for
    /// the HTML to be approved, exactly as the inventory's did.
    ///
    /// Until something presents it, a lit altar behaves as it always has — interacting rests at
    /// once — because a menu nobody can see would pause the game in front of the player and leave
    /// them stuck. A presenter (the interface, later; the developer overlay, today) registers
    /// itself, and from then on the altar opens this instead.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShrineMenu : MonoBehaviour
    {
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private FastTravelService _travel;
        [SerializeField] private PlayerCharacter _player;
        [SerializeField] private bool _pausesGame = true;

        private readonly HashSet<object> _presenters = new HashSet<object>();
        private PlayerInputReader _input;

        public event Action Opened;
        public event Action Closed;

        public bool IsOpen { get; private set; }
        public Checkpoint Origin { get; private set; }
        public bool HasPresenter => _presenters.Count > 0;
        public FastTravelService Travel => _travel;

        /// <summary>True when the travel entry leads anywhere: unlocked, and a second lit altar.</summary>
        public bool CanOfferTravel
        {
            get
            {
                if (_travel == null || Origin == null || !_travel.IsUnlocked)
                {
                    return false;
                }

                foreach (var destination in _travel.Destinations(Origin.CheckpointId))
                {
                    if (!destination.IsCurrent)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void Awake()
        {
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
            if (_travel == null) { _travel = FindAnyObjectByType<FastTravelService>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            _input = _player != null ? _player.GetComponent<PlayerInputReader>() : null;
        }

        private void OnDisable()
        {
            // Leaving with the menu up would strand the world paused.
            Close();
        }

        public void AddPresenter(object presenter)
        {
            if (presenter != null) { _presenters.Add(presenter); }
        }

        public void RemovePresenter(object presenter)
        {
            _presenters.Remove(presenter);
            if (!HasPresenter)
            {
                Close();
            }
        }

        public void Open(Checkpoint origin)
        {
            if (IsOpen || origin == null || _player == null || !_player.IsAlive)
            {
                return;
            }

            IsOpen = true;
            Origin = origin;
            _input?.SetGameplayEnabled(false);
            if (_pausesGame)
            {
                GamePause.Push();
            }

            Opened?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            if (_pausesGame)
            {
                GamePause.Pop();
            }

            _input?.SetGameplayEnabled(true);
            Closed?.Invoke();
        }

        /// <summary>Rests at the origin: the same as interacting with a lit altar always did.</summary>
        public void Rest()
        {
            var origin = Origin;
            Close();
            if (origin == null || _respawner == null)
            {
                return;
            }

            _respawner.SetRespawnPoint(origin.RespawnPosition, origin.CheckpointId);
            _respawner.RestAt(origin);
        }

        /// <summary>Leaves for another altar. A refusal keeps the menu open with the reason.</summary>
        public TravelResult TravelTo(string destinationId)
        {
            if (_travel == null || Origin == null)
            {
                return TravelResult.Locked;
            }

            var result = _travel.Check(Origin.CheckpointId, destinationId);
            if (result != TravelResult.Started)
            {
                _travel.TryTravel(Origin.CheckpointId, destinationId);
                return result;
            }

            // Closed first so the pause is released before the fade begins.
            var origin = Origin.CheckpointId;
            Close();
            return _travel.TryTravel(origin, destinationId);
        }
    }
}
