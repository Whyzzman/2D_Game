// Which sprites the open Tomb and Forest scenes draw, and how often (to find placeholders still on screen).
var counts = new System.Collections.Generic.Dictionary<string, int>();
foreach (var r in UnityEngine.Object.FindObjectsByType<UnityEngine.SpriteRenderer>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
{
  var scene = r.gameObject.scene.name;
  if (scene != "Zone_Tomb" && scene != "Zone_Forest" && scene != "TombTrial") continue;
  var path = r.sprite == null ? "(none)" : UnityEditor.AssetDatabase.GetAssetPath(r.sprite);
  if (path.Contains("/Art/Levels/")) continue;
  var key = path + " <- " + r.gameObject.name.Split('(')[0].Split('_')[0];
  counts[key] = counts.TryGetValue(key, out var n) ? n + 1 : 1;
}
var sb = new System.Text.StringBuilder();
foreach (var kv in System.Linq.Enumerable.OrderByDescending(counts, k => k.Value)) sb.AppendLine(kv.Value + " " + kv.Key);
System.IO.File.WriteAllText("Artifacts/sprites-in-zones.txt", sb.ToString());
return counts.Count;
