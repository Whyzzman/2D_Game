using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Things to break (GAME_FEEL_IDEAS item 14): the level text's <c>urn id gold</c> becomes an urn,
    /// a heap of skulls or a crate (by the room's zone, <see cref="PlaceholderPropArt.ForRoom"/>) that
    /// one blow from the hero bursts (<see cref="BreakableProp"/>). Neutral, with a hurtbox on the
    /// enemy layer as the breakable walls have; no collider stops anyone.
    /// </summary>
    public static partial class ContentBuilder
    {
        private static Dictionary<string, Sprite> _breakables;

        private static string PlaceBreakable(Transform parent, LevelMarker marker, Vector2 feet)
        {
            var id = marker.Arg(0);
            if (string.IsNullOrEmpty(id))
            {
                return "an urn needs an id";
            }

            var gold = int.TryParse(marker.Arg(1), out var parsed) ? Mathf.Max(0, parsed) : 0;
            if (_breakables == null) { _breakables = PlaceholderPropArt.Generate(); }
            var kind = PlaceholderPropArt.ForRoom(marker.RoomId);
            if (!_breakables.TryGetValue(kind, out var sprite) || sprite == null)
            {
                return $"no art for «{kind}»";
            }

            var root = new GameObject($"Breakable_{id}");
            root.transform.SetParent(parent, false);
            root.transform.position = feet;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            // In front of the walls, behind the hero: it stands in the room, not on a backdrop.
            SetSorting(renderer, "GameplayEnvironment", 3);

            var actor = Ensure<CombatActor>(root);
            SetEnum(actor, "_faction", (int)Faction.Neutral);
            SetFloat(Ensure<Health>(root), "_maxHealth", 100000f);
            SetFloat(actor, "_hitInvulnerability", 0.05f);

            var hurt = new GameObject("Hurtbox") { layer = EnemyLayer };
            hurt.transform.SetParent(root.transform, false);
            var size = sprite.bounds.size;
            hurt.transform.localPosition = new Vector3(0f, size.y * 0.5f, 0f);
            var box = hurt.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(size.x + 0.2f, size.y);
            SetReference(hurt.AddComponent<Hurtbox>(), "_owner", actor);

            var prop = Ensure<BreakableProp>(root);
            SetString(prop, "_propId", "break-" + id);
            SetInt(prop, "_gold", gold);
            SetReference(prop, "_actor", actor);
            SetReference(prop, "_hurtbox", box);
            SetReference(prop, "_visual", renderer);
            SetColor(prop, "_dustTint", kind == PlaceholderPropArt.Crate
                ? new Color(1f, 0.84f, 0.62f, 1f)
                : kind == PlaceholderPropArt.Skulls ? new Color(0.95f, 0.92f, 0.84f, 1f) : new Color(0.86f, 0.7f, 0.56f, 1f));
            return null;
        }
    }
}
