// Where things stand in the open world scenes: name contains one of the words below (for capture.cs points).
var words = new[] { WORDS };
var sb = new System.Text.StringBuilder();
foreach (var t in UnityEngine.Object.FindObjectsByType<UnityEngine.Transform>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None))
  foreach (var w in words)
    if (t.name.IndexOf(w, System.StringComparison.OrdinalIgnoreCase) >= 0) { sb.AppendLine(t.name + " " + t.position + " " + t.gameObject.scene.name); break; }
System.IO.File.WriteAllText("Artifacts/find.txt", sb.ToString());
return "found";
