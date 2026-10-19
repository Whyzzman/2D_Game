using System.IO;
using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Procedural placeholder score (item 9 of the 2026-09-27 list, the user's choices of
    /// 2026-09-28): gloomy ambient with a rare melody — a drone and slow minor chords, now and then
    /// a few notes on a piano or a bell. Each region has its own mode and timbre: the Tomb an organ
    /// and bells in D minor, the forest plucked strings and a breathy pipe in E dorian. The menu,
    /// one fight track both bosses share, and a quiet victory complete it.
    ///
    /// Every exploration track has a pulse layer — drums and bass of the same length and tempo —
    /// that the director fades in during a fight; the boss track has a heavier one for phase two.
    /// A layer only lines up because it is rendered on the same grid as its track and started on
    /// the same audio clock tick.
    ///
    /// Not a score: it exists so the states, the crossfades and the layers can be heard and judged.
    /// Each loop is written into a ring buffer, so a note ringing past the end continues at the
    /// start and the seam is silent.
    ///
    /// Deeper since 2026-10-19 (MUSIC_SYSTEM.md §4б, decided by Claude under the standing
    /// approval): the region loops are twice as long with the second half voiced higher, a low
    /// counter-voice answers the chords, a breath of filtered air sits under everything, and
    /// every track and layer is put in its own room (<see cref="Space"/>) — a crypt rings longer
    /// than a forest. The fight layers carry a string tremolo over the drums.
    /// </summary>
    public static class PlaceholderMusicArt
    {
        public const string Folder = "Assets/Game/Audio/Music";
        public const int Rate = 22050;

        /// <summary>What the content build hands the scene: every track and layer, by name.</summary>
        public sealed class Score
        {
            public AudioClip Menu, Tomb, TombPulse, Forest, ForestPulse, Capital, CapitalPulse, Village, VillagePulse, Boss, BossDrums, Victory;
        }

        public static Score Generate()
        {
            EnsureFolder(Folder);
            return new Score
            {
                Menu = Write("music_menu", MenuTrack()),
                Tomb = Write("music_tomb", TombTrack()),
                TombPulse = Write("music_tomb_pulse", PulseLayer(TombSong, 11)),
                Forest = Write("music_forest", ForestTrack()),
                ForestPulse = Write("music_forest_pulse", PulseLayer(ForestSong, 12)),
                Capital = Write("music_capital", CapitalTrack()),
                CapitalPulse = Write("music_capital_pulse", PulseLayer(CapitalSong, 14)),
                Village = Write("music_village", VillageTrack()),
                VillagePulse = Write("music_village_pulse", PulseLayer(VillageSong, 15)),
                Boss = Write("music_boss", BossTrack()),
                BossDrums = Write("music_boss_drums", BossDrums()),
                Victory = Write("music_victory", VictoryTrack())
            };
        }

        // ---------- songs: tempo, length and harmony shared by a track and its layer ----------

        private readonly struct Song
        {
            public readonly float Bpm;
            public readonly int Bars;
            /// <summary>Chord roots and qualities, one chord per <see cref="BarsPerChord"/> bars.</summary>
            public readonly int[][] Chords;
            public readonly int BarsPerChord;

            public Song(float bpm, int bars, int barsPerChord, params int[][] chords)
            {
                Bpm = bpm;
                Bars = bars;
                BarsPerChord = barsPerChord;
                Chords = chords;
            }

            public float Beat => 60f / Bpm;
            public float Bar => Beat * 4f;
            public int Length => Mathf.RoundToInt(Bar * Bars * Rate);
            public int[] ChordAt(int bar) => Chords[(bar / BarsPerChord) % Chords.Length];
        }

        // MIDI notes. Minor triads voiced low and open, the fifth doubled below for weight.
        private static readonly Song MenuSong = new Song(60f, 12, 2,
            new[] { 57, 60, 64 }, new[] { 53, 57, 60 }, new[] { 48, 55, 64 },
            new[] { 55, 59, 62 }, new[] { 57, 60, 64 }, new[] { 52, 55, 59 });

        private static readonly Song TombSong = new Song(60f, 24, 2,
            new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 48, 52, 55 },
            new[] { 50, 53, 57 }, new[] { 45, 49, 52 }, new[] { 50, 53, 57 });

        private static readonly Song ForestSong = new Song(72f, 32, 2,
            new[] { 52, 55, 59 }, new[] { 50, 54, 57 }, new[] { 48, 52, 55 },
            new[] { 52, 55, 59 }, new[] { 45, 49, 52 }, new[] { 47, 50, 54 },
            new[] { 48, 52, 55 }, new[] { 50, 54, 57 });

        // The capital: slower than the forest, lower than the tomb, and in a darker key.
        private static readonly Song CapitalSong = new Song(56f, 24, 2,
            new[] { 46, 49, 53 }, new[] { 42, 46, 49 }, new[] { 44, 48, 51 },
            new[] { 46, 49, 53 }, new[] { 41, 45, 48 }, new[] { 44, 47, 51 });

        // The village: a folk lament, between the forest's pace and the capital's, in a minor key.
        private static readonly Song VillageSong = new Song(64f, 24, 2,
            new[] { 50, 53, 57 }, new[] { 46, 50, 53 }, new[] { 48, 52, 55 },
            new[] { 45, 48, 52 }, new[] { 46, 50, 53 }, new[] { 43, 46, 50 });

        private static readonly Song BossSong = new Song(104f, 16, 2,
            new[] { 48, 51, 55 }, new[] { 44, 48, 51 }, new[] { 41, 44, 48 },
            new[] { 43, 47, 50 }, new[] { 48, 51, 55 }, new[] { 44, 48, 51 },
            new[] { 46, 50, 53 }, new[] { 43, 47, 50 });

        private static readonly Song VictorySong = new Song(60f, 6, 2,
            new[] { 48, 52, 55 }, new[] { 53, 57, 60 }, new[] { 55, 59, 62 });

        // ---------- tracks ----------

        private static float[] MenuTrack()
        {
            var song = MenuSong;
            var buffer = new float[song.Length];
            Drone(buffer, song, 0.16f);
            Pads(buffer, song, PadTone.Soft, 0.2f);
            // The melody is rare: one short phrase every other chord, and not always.
            Melody(buffer, song, new System.Random(3), new[] { 69, 72, 74, 76, 79, 81 },
                Voice.Piano, 0.24f, phraseEvery: 4, notesPerPhrase: 4);
            Air(buffer, 31, 0.035f, 900f);
            Space(buffer, 0.28f, 1.15f);
            return Finish(buffer);
        }

        private static float[] TombTrack()
        {
            var song = TombSong;
            var buffer = new float[song.Length];
            Drone(buffer, song, 0.2f);
            Pads(buffer, song, PadTone.Organ, 0.2f);
            Melody(buffer, song, new System.Random(5), new[] { 62, 65, 67, 69, 72, 74 },
                Voice.Bell, 0.2f, phraseEvery: 4, notesPerPhrase: 3);
            Counter(buffer, song, 0.12f);
            Air(buffer, 37, 0.04f, 520f);
            // A crypt: the longest, darkest room of all.
            Space(buffer, 0.36f, 1.35f);
            return Finish(buffer);
        }

        private static float[] ForestTrack()
        {
            var song = ForestSong;
            var buffer = new float[song.Length];
            Drone(buffer, song, 0.12f);
            Pads(buffer, song, PadTone.Air, 0.14f);
            Arpeggio(buffer, song, new System.Random(8), 0.13f);
            Melody(buffer, song, new System.Random(9), new[] { 64, 66, 67, 69, 71, 74, 76 },
                Voice.Pipe, 0.18f, phraseEvery: 4, notesPerPhrase: 5);
            Counter(buffer, song, 0.09f);
            // Leaves: brighter air, a smaller, softer room.
            Air(buffer, 41, 0.045f, 1800f);
            Space(buffer, 0.2f, 0.9f);
            return Finish(buffer);
        }

        /// <summary>A burnt city: organ under everything, a bell tolling now and then, little else.</summary>
        private static float[] CapitalTrack()
        {
            var song = CapitalSong;
            var buffer = new float[song.Length];
            Drone(buffer, song, 0.2f);
            Pads(buffer, song, PadTone.Organ, 0.16f);
            Melody(buffer, song, new System.Random(17), new[] { 58, 61, 63, 65, 68, 70 },
                Voice.Bell, 0.18f, phraseEvery: 4, notesPerPhrase: 2);
            Counter(buffer, song, 0.12f);
            Air(buffer, 43, 0.04f, 650f);
            // Burnt halls: as large as the crypt, a shade brighter.
            Space(buffer, 0.38f, 1.4f);
            return Finish(buffer);
        }

        /// <summary>An empty village at dusk: a drone, a pipe singing a few notes, the odd far bell.</summary>
        private static float[] VillageTrack()
        {
            var song = VillageSong;
            var buffer = new float[song.Length];
            Drone(buffer, song, 0.18f);
            Pads(buffer, song, PadTone.Soft, 0.15f);
            Melody(buffer, song, new System.Random(23), new[] { 62, 65, 67, 69, 70, 72 },
                Voice.Pipe, 0.17f, phraseEvery: 3, notesPerPhrase: 4);
            for (var bar = 0; bar < song.Bars; bar += song.BarsPerChord * 3)
            {
                Note(buffer, bar * song.Bar, 3f, Midi(song.ChordAt(bar)[0] + 12), 0.1f, Voice.Bell);
            }

            Counter(buffer, song, 0.1f);
            Air(buffer, 47, 0.04f, 1100f);
            Space(buffer, 0.24f, 1f);
            return Finish(buffer);
        }

        private static float[] BossTrack()
        {
            var song = BossSong;
            var buffer = new float[song.Length];
            Drone(buffer, song, 0.18f);
            Pads(buffer, song, PadTone.Organ, 0.16f);
            Ostinato(buffer, song, 0.24f);
            // A bell on every chord change: the arena's own clock.
            for (var bar = 0; bar < song.Bars; bar += song.BarsPerChord)
            {
                Note(buffer, bar * song.Bar, 3f, Midi(song.ChordAt(bar)[0] + 24), 0.16f, Voice.Bell);
            }

            Space(buffer, 0.2f, 1.1f);
            return Finish(buffer);
        }

        private static float[] VictoryTrack()
        {
            var song = VictorySong;
            var buffer = new float[song.Length];
            Pads(buffer, song, PadTone.Soft, 0.22f);
            Melody(buffer, song, new System.Random(13), new[] { 72, 76, 79, 84 },
                Voice.Bell, 0.2f, phraseEvery: 2, notesPerPhrase: 4);
            Space(buffer, 0.32f, 1.2f);
            return Finish(buffer);
        }

        /// <summary>
        /// The fight under exploration: a low drum on one and three, a ghost stroke before each,
        /// a soft tick on the eighths and the chord root pulsing below. Same grid as the track.
        /// </summary>
        private static float[] PulseLayer(Song song, int seed)
        {
            var random = new System.Random(seed);
            var buffer = new float[song.Length];
            var eighth = song.Beat * 0.5f;
            for (var bar = 0; bar < song.Bars; bar++)
            {
                var start = bar * song.Bar;
                var root = song.ChordAt(bar)[0] - 12;
                for (var e = 0; e < 8; e++)
                {
                    var t = start + e * eighth;
                    if (e == 0 || e == 4) { Drum(buffer, t, 0.9f, 110f, 48f); }
                    if (e == 3 || e == 7) { Drum(buffer, t, 0.35f, 140f, 70f); }
                    Tick(buffer, t, e % 2 == 0 ? 0.12f : 0.07f, random);
                    Note(buffer, t, eighth * 0.9f, Midi(root), e % 2 == 0 ? 0.3f : 0.2f, Voice.Bass);
                }
            }

            // Tension over the drums: the chord's third and fifth bowed in a quiet tremolo, so the
            // fight is a rising of the same music, not a different song.
            Tremolo(buffer, song, 0.07f, 12);
            Space(buffer, 0.14f, 0.9f);
            return Finish(buffer);
        }

        /// <summary>Phase two: war drums, a fill every fourth bar, sixteenth ticks.</summary>
        private static float[] BossDrums()
        {
            var song = BossSong;
            var random = new System.Random(17);
            var buffer = new float[song.Length];
            var sixteenth = song.Beat * 0.25f;
            for (var bar = 0; bar < song.Bars; bar++)
            {
                var start = bar * song.Bar;
                var fill = bar % 4 == 3;
                for (var s = 0; s < 16; s++)
                {
                    var t = start + s * sixteenth;
                    if (s % 4 == 0) { Drum(buffer, t, 1f, 100f, 42f); }
                    if (s == 6 || s == 10) { Drum(buffer, t, 0.55f, 100f, 42f); }
                    if (fill && s >= 12) { Drum(buffer, t, 0.6f, 190f - (s - 12) * 25f, 90f); }
                    Tick(buffer, t, s % 4 == 2 ? 0.14f : 0.06f, random);
                }
            }

            Tremolo(buffer, song, 0.09f, 24);
            Space(buffer, 0.14f, 1.1f);
            return Finish(buffer);
        }

        // ---------- parts ----------

        /// <summary>The ground under everything: the key's root and fifth, breathing slowly.</summary>
        private static void Drone(float[] buffer, Song song, float amp)
        {
            var length = buffer.Length;
            // Tuned to a whole number of cycles per loop (a cent or two off at most): a sustained
            // tone is the one sound that would otherwise click at the seam.
            var loopSeconds = length / (float)Rate;
            // An even number of cycles, so the fifth above (×1.5) closes on the seam as well.
            var root = 2f * Mathf.Round(Midi(song.Chords[0][0] - 24) * loopSeconds / 2f) / loopSeconds;
            for (var i = 0; i < length; i++)
            {
                var t = i / (float)Rate;
                // The breath completes a whole number of cycles per loop, so the seam is smooth.
                var breath = 0.75f + 0.25f * Mathf.Sin(2f * Mathf.PI * 3f * i / length);
                buffer[i] += amp * breath * (Mathf.Sin(2f * Mathf.PI * root * t)
                    + 0.5f * Mathf.Sin(2f * Mathf.PI * root * 1.5f * t)
                    + 0.2f * Mathf.Sin(2f * Mathf.PI * root * 2f * t));
            }
        }

        private enum PadTone { Soft, Organ, Air }

        private static void Pads(float[] buffer, Song song, PadTone tone, float amp)
        {
            var span = song.Bar * song.BarsPerChord;
            // A long loop's second half voices the same chords higher (the root an octave up), so the
            // repeat is heard as an answer rather than as the tape starting over.
            var answerFrom = song.Bars >= 24 ? song.Bars / 2 : int.MaxValue;
            for (var bar = 0; bar < song.Bars; bar += song.BarsPerChord)
            {
                var chord = song.ChordAt(bar);
                for (var n = 0; n < chord.Length; n++)
                {
                    var note = chord[n] + (bar >= answerFrom && n == 0 ? 12 : 0);
                    var pitch = tone == PadTone.Air ? note + 12 : note;
                    Pad(buffer, bar * song.Bar, span + 1.5f, Midi(pitch), amp, tone);
                }
            }
        }

        private static void Pad(float[] buffer, float start, float seconds, float freq, float amp,
            PadTone tone)
        {
            var from = Mathf.RoundToInt(start * Rate);
            var count = Mathf.RoundToInt(seconds * Rate);
            var attack = 1.4f;
            var release = 1.8f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)Rate;
                var env = Mathf.Min(1f, t / attack) * Mathf.Clamp01((seconds - t) / release);
                var phase = 2f * Mathf.PI * freq * t;
                float v;
                switch (tone)
                {
                    case PadTone.Organ:
                        v = Mathf.Sin(phase) + 0.45f * Mathf.Sin(phase * 2f) + 0.25f * Mathf.Sin(phase * 3f)
                            + 0.12f * Mathf.Sin(phase * 4f);
                        break;
                    case PadTone.Air:
                        v = Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 1.003f + 1f);
                        break;
                    default:
                        // Two voices a few cents apart: slow beating, the pad's movement.
                        v = Mathf.Sin(phase) + 0.8f * Mathf.Sin(phase * 1.004f) + 0.2f * Mathf.Sin(phase * 2f);
                        break;
                }

                Add(buffer, from + i, amp * env * v * 0.5f);
            }
        }

        private enum Voice { Piano, Bell, Pipe, Pluck, Bass, Low, Cello, Bow }

        private static void Melody(float[] buffer, Song song, System.Random random, int[] scale,
            Voice voice, float amp, int phraseEvery, int notesPerPhrase)
        {
            for (var bar = 1; bar < song.Bars; bar += phraseEvery)
            {
                // Sometimes the phrase is left out: rare, not scheduled.
                if (random.NextDouble() < 0.3)
                {
                    continue;
                }

                var t = bar * song.Bar + song.Beat * random.Next(0, 2);
                var index = random.Next(0, scale.Length);
                for (var n = 0; n < notesPerPhrase; n++)
                {
                    var length = song.Beat * (random.NextDouble() < 0.4 ? 2f : 1f);
                    Note(buffer, t, length * 1.6f, Midi(scale[index]), amp, voice);
                    t += length;
                    index = Mathf.Clamp(index + random.Next(-2, 3), 0, scale.Length - 1);
                }
            }
        }

        /// <summary>The forest's plucked strings: the chord broken upwards, with gaps.</summary>
        private static void Arpeggio(float[] buffer, Song song, System.Random random, float amp)
        {
            var eighth = song.Beat * 0.5f;
            for (var bar = 0; bar < song.Bars; bar++)
            {
                var chord = song.ChordAt(bar);
                for (var e = 0; e < 8; e++)
                {
                    if (random.NextDouble() < 0.35)
                    {
                        continue;
                    }

                    var note = chord[e % chord.Length] + (e >= 4 ? 12 : 0);
                    Note(buffer, bar * song.Bar + e * eighth, 1.2f, Midi(note), amp, Voice.Pluck);
                }
            }
        }

        /// <summary>The boss's driving low strings: root and fifth on the eighths.</summary>
        private static void Ostinato(float[] buffer, Song song, float amp)
        {
            var eighth = song.Beat * 0.5f;
            for (var bar = 0; bar < song.Bars; bar++)
            {
                var root = song.ChordAt(bar)[0] - 12;
                for (var e = 0; e < 8; e++)
                {
                    var note = e % 4 == 3 ? root + 7 : root;
                    Note(buffer, bar * song.Bar + e * eighth, eighth * 0.85f, Midi(note),
                        e % 2 == 0 ? amp : amp * 0.7f, Voice.Low);
                }
            }
        }

        /// <summary>
        /// A low bowed voice answering the chords: the third of every other chord an octave down,
        /// held through both its bars and swelling in, so the harmony moves under the melody.
        /// </summary>
        private static void Counter(float[] buffer, Song song, float amp)
        {
            var span = song.Bar * song.BarsPerChord;
            for (var bar = song.BarsPerChord; bar < song.Bars; bar += song.BarsPerChord * 2)
            {
                var chord = song.ChordAt(bar);
                Note(buffer, bar * song.Bar, span * 0.95f, Midi(chord[1] - 12), amp, Voice.Cello);
            }
        }

        /// <summary>
        /// Bowed tremolo on the chord's third and fifth, re-struck every sixteenth (or twelfth),
        /// at a whisper: the strings' nervous edge under a fight.
        /// </summary>
        private static void Tremolo(float[] buffer, Song song, float amp, int strokesPerBar)
        {
            var stroke = song.Bar / strokesPerBar;
            for (var bar = 0; bar < song.Bars; bar++)
            {
                var chord = song.ChordAt(bar);
                for (var s = 0; s < strokesPerBar; s++)
                {
                    var swell = 0.6f + 0.4f * Mathf.Sin(Mathf.PI * s / strokesPerBar);
                    foreach (var note in new[] { chord[1] + 12, chord[2] + 12 })
                    {
                        Note(buffer, bar * song.Bar + s * stroke, stroke * 1.1f, Midi(note),
                            amp * swell, Voice.Bow);
                    }
                }
            }
        }

        /// <summary>
        /// A breath of air under the music: noise through a slowly opening and closing low-pass.
        /// Filtered over two passes of the loop, so the filter enters the seam already settled.
        /// </summary>
        private static void Air(float[] buffer, int seed, float amp, float cutoff)
        {
            var random = new System.Random(seed);
            var length = buffer.Length;
            var noise = new float[length];
            for (var i = 0; i < length; i++)
            {
                noise[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            }

            var z1 = 0f;
            var z2 = 0f;
            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < length; i++)
                {
                    // Two whole cycles of the sweep per loop: the seam meets itself.
                    var sweep = cutoff * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 2f * i / length));
                    var a = Mathf.Clamp01(1f - Mathf.Exp(-2f * Mathf.PI * sweep / Rate));
                    z1 += a * (noise[i] - z1);
                    z2 += a * (z1 - z2);
                    if (pass == 1)
                    {
                        buffer[i] += z2 * amp * 6f;
                    }
                }
            }
        }

        /// <summary>
        /// A room: four damped comb filters and two all-passes (Schroeder), mixed in at
        /// <paramref name="wet"/>. <paramref name="size"/> stretches the delays — a crypt rings
        /// longer than a glade. Run over the loop twice and kept from the second pass, so the tail
        /// of the end is already ringing at the start and the seam stays silent.
        /// </summary>
        private static void Space(float[] buffer, float wet, float size)
        {
            var length = buffer.Length;
            var combs = new[] { 0.0297f, 0.0371f, 0.0411f, 0.0437f };
            var lines = new float[combs.Length][];
            var heads = new int[combs.Length];
            var damp = new float[combs.Length];
            for (var c = 0; c < combs.Length; c++)
            {
                lines[c] = new float[Mathf.Max(1, Mathf.RoundToInt(combs[c] * size * Rate))];
            }

            var allpass = new[] { new float[Mathf.RoundToInt(0.005f * Rate)], new float[Mathf.RoundToInt(0.0017f * Rate)] };
            var allHeads = new int[2];
            var feedback = Mathf.Clamp(0.74f + 0.06f * size, 0.7f, 0.86f);
            var output = new float[length];
            for (var pass = 0; pass < 2; pass++)
            {
                for (var i = 0; i < length; i++)
                {
                    var input = buffer[i];
                    var sum = 0f;
                    for (var c = 0; c < combs.Length; c++)
                    {
                        var line = lines[c];
                        var delayed = line[heads[c]];
                        damp[c] += 0.35f * (delayed - damp[c]);
                        line[heads[c]] = input + damp[c] * feedback;
                        heads[c] = (heads[c] + 1) % line.Length;
                        sum += delayed;
                    }

                    var value = sum * 0.25f;
                    for (var a = 0; a < allpass.Length; a++)
                    {
                        var line = allpass[a];
                        var delayed = line[allHeads[a]];
                        var next = value + delayed * 0.5f;
                        line[allHeads[a]] = next;
                        allHeads[a] = (allHeads[a] + 1) % line.Length;
                        value = delayed - next * 0.5f;
                    }

                    if (pass == 1)
                    {
                        output[i] = value;
                    }
                }
            }

            for (var i = 0; i < length; i++)
            {
                buffer[i] = buffer[i] * (1f - wet * 0.5f) + output[i] * wet;
            }
        }

        // ---------- instruments ----------

        private static void Note(float[] buffer, float start, float seconds, float freq, float amp,
            Voice voice)
        {
            var from = Mathf.RoundToInt(start * Rate);
            var count = Mathf.RoundToInt(seconds * Rate);
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)Rate;
                var phase = 2f * Mathf.PI * freq * t;
                var tail = Mathf.Clamp01((seconds - t) / 0.08f);
                float v;
                switch (voice)
                {
                    case Voice.Piano:
                        v = Mathf.Exp(-t * 1.6f) * (Mathf.Sin(phase) + 0.4f * Mathf.Sin(phase * 2f) * Mathf.Exp(-t * 3f)
                            + 0.15f * Mathf.Sin(phase * 3f) * Mathf.Exp(-t * 5f));
                        v *= Mathf.Min(1f, t / 0.005f);
                        break;
                    case Voice.Bell:
                        // Inharmonic partials: the ring of cast metal rather than a string.
                        v = Mathf.Exp(-t * 0.9f) * (Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2.76f) * Mathf.Exp(-t * 2f)
                            + 0.25f * Mathf.Sin(phase * 5.4f) * Mathf.Exp(-t * 4f));
                        v *= Mathf.Min(1f, t / 0.003f);
                        break;
                    case Voice.Pipe:
                        var vibrato = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 5f * t) * Mathf.Min(1f, t / 0.4f);
                        v = Mathf.Sin(phase * vibrato) + 0.15f * Mathf.Sin(phase * 2f * vibrato);
                        v *= Mathf.Min(1f, t / 0.12f);
                        break;
                    case Voice.Pluck:
                        v = Mathf.Exp(-t * 4f) * (Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2f) * Mathf.Exp(-t * 6f)
                            + 0.3f * Mathf.Sin(phase * 3f) * Mathf.Exp(-t * 9f));
                        v *= Mathf.Min(1f, t / 0.002f);
                        break;
                    case Voice.Bass:
                        v = Mathf.Exp(-t * 3f) * (Mathf.Sin(phase) + 0.3f * Mathf.Sin(phase * 2f));
                        v *= Mathf.Min(1f, t / 0.004f);
                        break;
                    case Voice.Cello:
                    {
                        // A held bow: a slow swell in, a gentle vibrato that arrives late, warm harmonics.
                        var wobble = 1f + 0.003f * Mathf.Sin(2f * Mathf.PI * 4.6f * t) * Mathf.Clamp01((t - 0.5f) / 0.8f);
                        var p = phase * wobble;
                        v = Mathf.Sin(p) + 0.42f * Mathf.Sin(p * 2f) + 0.2f * Mathf.Sin(p * 3f) + 0.08f * Mathf.Sin(p * 5f);
                        v *= Mathf.Min(1f, t / 0.9f) * Mathf.Clamp01((seconds - t) / 0.9f);
                        break;
                    }
                    case Voice.Bow:
                        // One short tremolo stroke: soft in and out so the strokes blur together.
                        v = (Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 2f) + 0.15f * Mathf.Sin(phase * 3f))
                            * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / Mathf.Max(0.001f, seconds)));
                        break;
                    default:
                        // Low bowed strings: a few harmonics, a short bow attack.
                        v = Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2f) + 0.3f * Mathf.Sin(phase * 3f)
                            + 0.15f * Mathf.Sin(phase * 4f);
                        v *= Mathf.Min(1f, t / 0.02f) * Mathf.Exp(-t * 1.5f);
                        break;
                }

                Add(buffer, from + i, amp * v * tail);
            }
        }

        /// <summary>A tuned drum: a sine falling from one pitch to another, quick to die.</summary>
        private static void Drum(float[] buffer, float start, float amp, float fromHz, float toHz)
        {
            var from = Mathf.RoundToInt(start * Rate);
            var count = Mathf.RoundToInt(0.45f * Rate);
            var phase = 0f;
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)Rate;
                var freq = Mathf.Lerp(toHz, fromHz, Mathf.Exp(-t * 18f));
                phase += 2f * Mathf.PI * freq / Rate;
                Add(buffer, from + i, 0.6f * amp * Mathf.Sin(phase) * Mathf.Exp(-t * 7f));
            }
        }

        /// <summary>A soft noise tick: the rattle over the drums.</summary>
        private static void Tick(float[] buffer, float start, float amp, System.Random random)
        {
            var from = Mathf.RoundToInt(start * Rate);
            var count = Mathf.RoundToInt(0.05f * Rate);
            var last = 0f;
            for (var i = 0; i < count; i++)
            {
                var noise = (float)(random.NextDouble() * 2.0 - 1.0);
                // The difference of two noise samples: a crude high-pass, so it hisses, not thumps.
                var bright = noise - last;
                last = noise;
                // Two milliseconds of attack: struck, not switched on.
                var onset = Mathf.Min(1f, i / (0.002f * Rate));
                Add(buffer, from + i, amp * 0.5f * bright * onset * Mathf.Exp(-i / (float)Rate * 60f));
            }
        }

        // ---------- helpers ----------

        private static float Midi(int note) => 440f * Mathf.Pow(2f, (note - 69) / 12f);

        /// <summary>Writes into the loop as a ring: whatever rings past the end lands at the start.</summary>
        private static void Add(float[] buffer, int index, float value)
        {
            buffer[((index % buffer.Length) + buffer.Length) % buffer.Length] += value;
        }

        private static float[] Finish(float[] buffer)
        {
            var peak = 0f;
            foreach (var sample in buffer)
            {
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            }

            if (peak > 0f)
            {
                var gain = 0.7f / peak;
                for (var i = 0; i < buffer.Length; i++)
                {
                    buffer[i] *= gain;
                }
            }

            return buffer;
        }

        private static AudioClip Write(string name, float[] samples)
        {
            var path = $"{Folder}/{name}.wav";
            var bytes = Wav(samples);
            var full = Path.GetFullPath(path);
            if (!File.Exists(full) || !BytesEqual(File.ReadAllBytes(full), bytes))
            {
                File.WriteAllBytes(full, bytes);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            Configure(path);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogError($"Placeholder music failed to import: {path}");
            }

            return clip;
        }

        /// <summary>
        /// Compressed in memory rather than streamed: a layer is started on the same audio clock
        /// tick as its track, and a streamed clip cannot promise to begin on that tick.
        /// </summary>
        private static void Configure(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                return;
            }

            var settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.CompressedInMemory
                && settings.compressionFormat == AudioCompressionFormat.Vorbis
                && importer.forceToMono)
            {
                return;
            }

            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.6f;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        private static byte[] Wav(float[] samples)
        {
            var dataBytes = samples.Length * 2;
            using var stream = new MemoryStream(44 + dataBytes);
            using var writer = new BinaryWriter(stream);
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataBytes);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(Rate);
            writer.Write(Rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataBytes);
            foreach (var sample in samples)
            {
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * 32000f));
            }

            writer.Flush();
            return stream.ToArray();
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
