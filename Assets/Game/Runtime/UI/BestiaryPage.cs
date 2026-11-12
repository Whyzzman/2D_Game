using System.Collections.Generic;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The «Бестіарій» page of the inventory window, from design/ui/bestiary/bestiary.html
    /// (item 4 of the big list, stage D): the zones the hero has been in, in the order of the
    /// world; under each the kinds of enemy it holds, named with a kill count once one is killed
    /// and «???» until then; last the bosses, named once beaten. A dot marks a kind killed since
    /// the page was last looked at.
    /// </summary>
    public sealed class BestiaryPage
    {
        public const string Unknown = "???";
        private readonly ScrollView _list;
        private readonly Label _kinds;
        private readonly Label _total;
        private GameProgress _progress;
        // Kinds killed since the page was last opened; worked out once per visit, so a redraw
        // while the page is open does not wipe the dots it has just shown.
        private HashSet<EnemyDefinition> _fresh;

        internal BestiaryPage(VisualElement page)
        {
            _list = page?.Q<ScrollView>("bst-list");
            _kinds = page?.Q<Label>("bst-kinds");
            _total = page?.Q<Label>("bst-total");
        }

        /// <summary>The rows shown now as «name · count» (count empty for bosses and unknowns), for tests.</summary>
        public List<string> Shown { get; } = new List<string>();

        /// <summary>The bosses' hint lines on the page now, for tests.</summary>
        public List<string> Hints { get; } = new List<string>();

        /// <summary>The zone headers shown now, for tests.</summary>
        public List<string> Zones { get; } = new List<string>();

        public string KindsText => _kinds != null ? _kinds.text : string.Empty;

        public void Attach(GameProgress progress) => _progress = progress;

        /// <summary>The page is turned to: the next draw decides what is new.</summary>
        public void BeginVisit() => _fresh = null;

        public void Draw()
        {
            Shown.Clear();
            Hints.Clear();
            Zones.Clear();
            _list?.Clear();
            var catalog = _progress != null ? _progress.BestiaryCatalog : null;
            var world = _progress != null ? _progress.World : null;
            if (catalog == null || world == null)
            {
                return;
            }

            if (_fresh == null)
            {
                _fresh = new HashSet<EnemyDefinition>();
                foreach (var entry in catalog.Enemies)
                {
                    if (entry.Enemy != null && Bestiary.Kills(_progress, entry.Enemy) > 0
                        && !Bestiary.IsSeen(_progress, entry.Enemy))
                    {
                        _fresh.Add(entry.Enemy);
                        Bestiary.MarkSeen(_progress, entry.Enemy);
                    }
                }
            }

            int known = 0, listed = 0;
            foreach (var zone in world.Zones)
            {
                if (zone == null || !Bestiary.HasVisited(_progress, zone.ZoneId))
                {
                    continue;
                }

                var header = false;
                foreach (var entry in catalog.Enemies)
                {
                    if (entry.Enemy == null || entry.ZoneId != zone.ZoneId)
                    {
                        continue;
                    }

                    if (!header)
                    {
                        AddZone(zone.DisplayName);
                        header = true;
                    }

                    var kills = Bestiary.Kills(_progress, entry.Enemy);
                    listed++;
                    if (kills <= 0)
                    {
                        AddRow(Unknown, string.Empty, "bst__row--unknown");
                        continue;
                    }

                    known++;
                    AddRow(entry.Enemy.DisplayName, kills.ToString(),
                        _fresh.Contains(entry.Enemy) ? "bst__row--new" : null);
                }
            }

            var bossHeader = false;
            foreach (var boss in catalog.Bosses)
            {
                if (!Bestiary.HasVisited(_progress, boss.ZoneId))
                {
                    continue;
                }

                if (!bossHeader)
                {
                    AddZone("Боси");
                    bossHeader = true;
                }

                var beaten = _progress.IsBossDefeated(boss.BossId);
                AddRow(beaten ? boss.DisplayName : Unknown, string.Empty,
                    beaten ? "bst__row--boss" : "bst__row--unknown");
                AddHint(BossHints.For(boss.BossId));
            }

            if (_kinds != null) { _kinds.text = $"{known} з {listed} видів"; }
            if (_total != null) { _total.text = $"Усього вбито: {Bestiary.TotalKills(_progress)}"; }
        }

        private void AddZone(string name)
        {
            Zones.Add(name);
            _list?.Add(UiParts.Text(name.ToUpperInvariant(), "bst__zone"));
        }

        /// <summary>What is said about a boss whose zone the hero has seen, beaten or not: help, not a name.</summary>
        private void AddHint(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            Hints.Add(line);
            _list?.Add(UiParts.Text(line, "bst__hint"));
        }

        private void AddRow(string name, string count, string modifier)
        {
            Shown.Add(string.IsNullOrEmpty(count) ? name : $"{name} · {count}");
            if (_list == null)
            {
                return;
            }

            var row = new VisualElement { pickingMode = PickingMode.Ignore };
            row.AddToClassList("bst__row");
            if (modifier != null) { row.AddToClassList(modifier); }
            row.Add(UiParts.Text(modifier == "bst__row--new" ? $"{name}  •" : name, "bst__name"));
            row.Add(UiParts.Text(count, "bst__count"));
            _list.Add(row);
        }
    }
}
