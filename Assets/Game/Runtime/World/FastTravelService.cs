using System;
using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>Why a journey did or did not begin.</summary>
    public enum TravelResult
    {
        Started,
        /// <summary>Fast travel is not open yet in this run.</summary>
        Locked,
        /// <summary>Travel starts at a lit altar the player is standing at, nowhere else.</summary>
        NotAtShrine,
        UnknownDestination,
        /// <summary>An altar the player never lit. Never offered, never accepted.</summary>
        NotActivated,
        NotEligible,
        AlreadyHere,
        InCombat,
        BossFight,
        PlayerDead,
        /// <summary>A death, a rest or another journey is already running.</summary>
        Busy,
        /// <summary>The altar lives in another scene; scene streaming is not built yet.</summary>
        NotInThisScene,
        UnbuiltZone
    }

    /// <summary>One line of the travel list, generated from the checkpoint database.</summary>
    public readonly struct TravelDestination
    {
        public TravelDestination(CheckpointDefinition checkpoint, ZoneDefinition zone,
            WorldAreaDefinition area, bool isCurrent, TravelResult availability)
        {
            Checkpoint = checkpoint;
            Zone = zone;
            Area = area;
            IsCurrent = isCurrent;
            Availability = availability;
        }

        public CheckpointDefinition Checkpoint { get; }
        public ZoneDefinition Zone { get; }
        public WorldAreaDefinition Area { get; }
        public bool IsCurrent { get; }
        public TravelResult Availability { get; }
        public bool IsSelectable => Availability == TravelResult.Started;
        public string CheckpointId => Checkpoint.CheckpointId;
        public Vector2 MapPosition => Area != null ? Area.MapPosition : Vector2.zero;
    }

    /// <summary>
    /// Checkpoint-to-checkpoint travel, offered at a lit altar.
    ///
    /// It is a convenience over routes already walked, not a replacement for walking them: the
    /// list is every altar the player lit — the same list the save has kept since PHASE 9 — minus
    /// any the database marks as not a destination. There is no second list to fall out of step.
    ///
    /// Every check lives in <see cref="Check"/>, so the menu and a debug command cannot disagree
    /// about what is allowed. The journey itself is the respawner's, which owns every transition
    /// and keeps them in one order.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FastTravelService : MonoBehaviour
    {
        [SerializeField] private GameProgress _progress;
        [SerializeField] private PlayerRespawner _respawner;
        [SerializeField] private PlayerCharacter _player;
        [Tooltip("How close to the altar the player must stand for it to count as the origin.")]
        [SerializeField, Min(0.5f)] private float _shrineReach = 3f;

        private readonly List<TravelDestination> _destinations = new List<TravelDestination>();
        private BossEncounter[] _encounters = Array.Empty<BossEncounter>();
        private PlayerCombatStatus _combatStatus;

        public event Action<string> TravelStarted;
        public event Action<string> TravelCompleted;
        public event Action<TravelResult> TravelRefused;

        public bool IsTraveling { get; private set; }
        public string LastMessage { get; private set; } = string.Empty;

        private WorldGraph Graph => _progress != null ? _progress.World : null;
        private IWorldQuery World => (IWorldQuery)WorldState.Current ?? _progress;

        public bool IsUnlocked => WorldRules.IsFastTravelUnlocked(Graph, World);

        private void Awake()
        {
            if (_progress == null) { _progress = FindAnyObjectByType<GameProgress>(); }
            if (_respawner == null) { _respawner = FindAnyObjectByType<PlayerRespawner>(); }
            if (_player == null) { _player = FindAnyObjectByType<PlayerCharacter>(); }
            _combatStatus = _player != null ? _player.GetComponent<PlayerCombatStatus>() : null;
            // Arenas are fixed parts of the scene; found once, not per question.
            _encounters = FindObjectsByType<BossEncounter>(FindObjectsInactive.Include);
        }

        /// <summary>
        /// Every altar that could appear in the list, in database order, each with whether it can
        /// be chosen from <paramref name="originCheckpointId"/> right now.
        /// </summary>
        public IReadOnlyList<TravelDestination> Destinations(string originCheckpointId)
        {
            _destinations.Clear();
            var graph = Graph;
            if (graph == null)
            {
                return _destinations;
            }

            foreach (var checkpoint in graph.Checkpoints)
            {
                // An altar never lit is not merely greyed out; it is not on the list at all.
                if (checkpoint == null || !checkpoint.CanFastTravel
                    || !World.IsCheckpointActivated(checkpoint.CheckpointId))
                {
                    continue;
                }

                _destinations.Add(new TravelDestination(checkpoint, graph.Zone(checkpoint.ZoneId),
                    graph.Area(checkpoint.AreaId),
                    checkpoint.CheckpointId == originCheckpointId,
                    Check(originCheckpointId, checkpoint.CheckpointId)));
            }

            return _destinations;
        }

        /// <summary>Whether a journey from one altar to another may begin, and if not, why.</summary>
        public TravelResult Check(string originCheckpointId, string destinationId)
        {
            var graph = Graph;
            if (graph == null || !IsUnlocked)
            {
                return TravelResult.Locked;
            }

            var origin = _respawner != null ? _respawner.FindCheckpoint(originCheckpointId) : null;
            if (origin == null || !World.IsCheckpointActivated(originCheckpointId)
                || _player == null
                || Vector2.Distance(_player.transform.position, origin.transform.position)
                    > _shrineReach)
            {
                return TravelResult.NotAtShrine;
            }

            var destination = graph.Checkpoint(destinationId);
            if (destination == null)
            {
                return TravelResult.UnknownDestination;
            }

            if (!World.IsCheckpointActivated(destinationId))
            {
                return TravelResult.NotActivated;
            }

            if (!destination.CanFastTravel)
            {
                return TravelResult.NotEligible;
            }

            if (destinationId == originCheckpointId)
            {
                return TravelResult.AlreadyHere;
            }

            var zone = graph.Zone(destination.ZoneId);
            if (zone == null || !zone.IsImplemented)
            {
                return TravelResult.UnbuiltZone;
            }

            if (!_player.IsAlive)
            {
                return TravelResult.PlayerDead;
            }

            if (IsTraveling || _respawner.IsSequenceRunning)
            {
                return TravelResult.Busy;
            }

            if (IsBossFightActive)
            {
                return TravelResult.BossFight;
            }

            if (_combatStatus != null && _combatStatus.IsInCombat)
            {
                return TravelResult.InCombat;
            }

            // Resolved before anything moves: a destination this scene cannot place the player
            // at is refused here, so the last checkpoint is never overwritten with it.
            if (_respawner.FindCheckpoint(destinationId) == null)
            {
                return TravelResult.NotInThisScene;
            }

            return TravelResult.Started;
        }

        /// <summary>Starts the journey, or explains why not. Nothing changes on a refusal.</summary>
        public TravelResult TryTravel(string originCheckpointId, string destinationId)
        {
            var result = Check(originCheckpointId, destinationId);
            if (result != TravelResult.Started)
            {
                Refuse(result);
                return result;
            }

            var destination = _respawner.FindCheckpoint(destinationId);
            IsTraveling = true;
            if (!_respawner.BeginTravel(destination, arrived => Finish(destinationId, arrived)))
            {
                IsTraveling = false;
                Refuse(TravelResult.Busy);
                return TravelResult.Busy;
            }

            LastMessage = string.Empty;
            TravelStarted?.Invoke(destinationId);
            return TravelResult.Started;
        }

        private void Finish(string destinationId, bool arrived)
        {
            IsTraveling = false;
            if (arrived)
            {
                TravelCompleted?.Invoke(destinationId);
            }
            else
            {
                Refuse(TravelResult.NotInThisScene);
            }
        }

        private void Refuse(TravelResult result)
        {
            LastMessage = Describe(result);
            Debug.Log($"Fast travel refused: {result}.");
            TravelRefused?.Invoke(result);
        }

        private bool IsBossFightActive
        {
            get
            {
                foreach (var encounter in _encounters)
                {
                    if (encounter != null && (encounter.IsFightActive || encounter.IsSettling))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>What the menu says when a destination cannot be chosen.</summary>
        public static string Describe(TravelResult result)
        {
            switch (result)
            {
                case TravelResult.Started: return string.Empty;
                case TravelResult.Locked: return "Шляхи між вівтарями ще не відкриті";
                case TravelResult.NotAtShrine: return "Мандрувати можна лише від вівтаря";
                case TravelResult.NotActivated: return "Цей вівтар ще не запалено";
                case TravelResult.AlreadyHere: return "Ви вже тут";
                case TravelResult.InCombat: return "Не можна мандрувати в бою";
                case TravelResult.BossFight: return "Не можна залишити бій";
                case TravelResult.PlayerDead: return string.Empty;
                case TravelResult.Busy: return "Зачекайте";
                default: return "Шлях недоступний";
            }
        }
    }
}
