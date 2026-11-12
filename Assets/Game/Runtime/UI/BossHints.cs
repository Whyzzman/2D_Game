using System.Collections.Generic;

namespace ForgottenKingdom
{
    /// <summary>
    /// One line under each boss in the bestiary, once the hero has been to its zone: what is said about how to
    /// meet its blows, in the voice of a rumour, not a manual (2026-11-12; decided by Claude under the standing
    /// consent, gamedesign/BOSSES_SIGNATURES.md, KING_FINALE.md).
    /// </summary>
    public static class BossHints
    {
        private static readonly Dictionary<string, string> Lines = new Dictionary<string, string>
        {
            ["guardian-of-the-tomb"] = "Кажуть, удар його меча зверху можна відбити. Коли падає щит — він стає швидшим.",
            ["hollow-warden"] = "Коріння слухає Вартового. Не стій там, де земля здулася, і бережись довгого древка.",
            ["bellringer"] = "Коли Дзвонар б'є в підлогу, камінь падає через крок. Між каменями — місце для одного.",
            ["ash-commander"] = "Його алебарда ламає підлогу рядом, як хвилю. Хвилю перестрибують — або проскакують.",
            ["plague-stag"] = "Де Олень приземлився — там отрута. Веди його на чисте й не стій, коли дзвонять роги.",
            ["ashen-king"] = "Коли згасне світло, дивись на корону. Вогонь його клятви йде від стін — до нього."
        };

        /// <summary>The line for a boss, or empty for one nobody talks about.</summary>
        public static string For(string bossId) =>
            bossId != null && Lines.TryGetValue(bossId, out var line) ? line : string.Empty;
    }
}
