// Кадр гри з живого Editor (не Play Mode): тимчасова камера в точці, 32 × 18 юнітів, PNG у Artifacts/Shots.
// unity command eval_file tools/level-art/capture.cs — точки нижче, правити під потрібну кімнату.
var shots = new (string name, float x, float y)[] { SHOTS };
var scenes = new[] { "Assets/Game/Scenes/TombTrial.unity", "Assets/Game/Scenes/Zones/Zone_Tomb.unity", "Assets/Game/Scenes/Zones/Zone_Forest.unity" };
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenes[0], UnityEditor.SceneManagement.OpenSceneMode.Single);
for (int i = 1; i < scenes.Length; i++) UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenes[i], UnityEditor.SceneManagement.OpenSceneMode.Additive);
System.IO.Directory.CreateDirectory("Artifacts/Shots");
var main = UnityEngine.Camera.main;
var go = UnityEngine.Object.Instantiate(main.gameObject);
var cam = go.GetComponent<UnityEngine.Camera>();
foreach (var b in go.GetComponents<UnityEngine.MonoBehaviour>()) if (!(b is UnityEngine.Rendering.Universal.UniversalAdditionalCameraData)) b.enabled = false;
cam.orthographic = true; cam.orthographicSize = 9;
var rt = new UnityEngine.RenderTexture(1600, 900, 24);
cam.targetTexture = rt;
foreach (var s in shots) {
  go.transform.position = new UnityEngine.Vector3(s.x, s.y, -10);
  cam.Render();
  UnityEngine.RenderTexture.active = rt;
  var tex = new UnityEngine.Texture2D(1600, 900, UnityEngine.TextureFormat.RGB24, false);
  tex.ReadPixels(new UnityEngine.Rect(0, 0, 1600, 900), 0, 0); tex.Apply();
  System.IO.File.WriteAllBytes("Artifacts/Shots/" + s.name + ".png", tex.EncodeToPNG());
}
UnityEngine.RenderTexture.active = null; cam.targetTexture = null;
UnityEngine.Object.DestroyImmediate(go);
return "shot";
