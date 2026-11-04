// LevelCheck of every zone text from the live Editor; the result goes to Artifacts/levelcheck.txt.
var names = new[]{"tomb","forest","village","capital"};
var sb = new System.Text.StringBuilder();
foreach (var n in names) {
  var z = ForgottenKingdom.LevelZone.Parse(System.IO.File.ReadAllText("Assets/Game/Levels/"+n+".txt"));
  var p = ForgottenKingdom.LevelCheck.Problems(z);
  sb.Append(n+": "+(p.Count==0?"OK":string.Join(" | ",p))+"\n");
}
System.IO.File.WriteAllText("Artifacts/levelcheck.txt", sb.ToString()); return "ok";
