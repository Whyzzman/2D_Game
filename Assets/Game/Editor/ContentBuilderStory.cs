using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// The story told in the world (item 5 of the big list, gamedesign/STORY.md): seven stones
    /// with words cut into them along the main path, stood on whatever floor is under them.
    /// Built with the exploration layer, under the same root.
    /// </summary>
    public static partial class ContentBuilder
    {
        private readonly struct StoneSpot
        {
            public readonly string Name;
            public readonly float X;
            public readonly string Text;

            public StoneSpot(string name, float x, string text)
            {
                Name = name;
                X = x;
                Text = text;
            }
        }

        public static readonly float[] InscriptionXs = { -17.5f, 22.5f, 34f, 86f, 212.5f, 319.5f, 508f };

        private static readonly StoneSpot[] Inscriptions =
        {
            new StoneSpot("InscriptionUnsworn", InscriptionXs[0],
                "Тут лежать ті, хто не присягнув. Їхні імена стерто, щоб королівство пам'ятало лише вірних."),
            new StoneSpot("InscriptionWardensKey", InscriptionXs[1],
                "Ми замурували двері й поклали ключ у серце вартового. Хай тінь, що вийде звідси, знає: її не кликали."),
            new StoneSpot("InscriptionGuardian", InscriptionXs[2],
                "Страж не пропустить нікого назовні. Не тому, що мертві небезпечні, — тому, що вони пам'ятають, як було насправді."),
            new StoneSpot("InscriptionRoots", InscriptionXs[3],
                "Ліс присягав разом із Королем. Коріння досі тримає слово, хоч листя давно його забуло."),
            new StoneSpot("InscriptionHunters", InscriptionXs[4],
                "Мисливці пішли до Столиці по наказ і не повернулися. Сокольничий чекає на них і досі."),
            new StoneSpot("InscriptionFire", InscriptionXs[5],
                "Вогонь не взяв місто одразу. Воно горіло роками, бо Король заборонив йому згасати."),
            new StoneSpot("InscriptionOath", InscriptionXs[6],
                "«Моє слово — стіна. Поки я стою, королівство не забудуть». — Клятва Короля, викарбувана на дверях")
        };

        private static readonly System.Collections.Generic.Dictionary<string, string> TombStones =
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["InscriptionUnsworn"] = "stone-unsworn",
                ["InscriptionWardensKey"] = "stone-wardens-key",
                ["InscriptionGuardian"] = "stone-guardian"
            };

        private static readonly System.Collections.Generic.Dictionary<string, string> ForestStones =
            new System.Collections.Generic.Dictionary<string, string>
            {
                ["InscriptionRoots"] = "stone-roots",
                ["InscriptionHunters"] = "stone-hunters"
            };

        private static void BuildInscriptions(Transform root)
        {
            Physics2D.SyncTransforms();
            var previous = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = false;
            foreach (var spot in Inscriptions)
            {
                var stone = new GameObject(spot.Name);
                stone.transform.SetParent(root);
                // The Tomb's and the Forest's stones stand where their texts put them; the Capital's by x.
                stone.transform.position = TombStones.TryGetValue(spot.Name, out var tombSpot) ? (Vector3)TombSpot(tombSpot)
                    : ForestStones.TryGetValue(spot.Name, out var forestSpot) ? (Vector3)ForestSpot(forestSpot)
                    : (Vector3)FloorNear(spot.X);
                var trigger = stone.AddComponent<BoxCollider2D>();
                trigger.isTrigger = true;
                trigger.size = new Vector2(2.2f, 2.6f);
                trigger.offset = new Vector2(0f, 1.3f);
                StonePart(stone, "Slab", new Vector2(0f, 0.62f), new Vector2(0.86f, 1.24f), new Color(0.26f, 0.23f, 0.2f));
                StonePart(stone, "Cap", new Vector2(0f, 1.28f), new Vector2(1f, 0.12f), new Color(0.33f, 0.29f, 0.24f));
                for (var line = 0; line < 4; line++)
                {
                    StonePart(stone, $"Line{line}", new Vector2(0f, 1.02f - line * 0.16f), new Vector2(0.56f, 0.04f),
                        new Color(0.13f, 0.11f, 0.1f));
                }

                SetString(Ensure<Inscription>(stone), "_text", spot.Text);
            }

            Physics2D.queriesHitTriggers = previous;
        }

        /// <summary>
        /// Where the stone for x stands: the nearest x, within three units, with floor under it at
        /// about a standing hero's height or lower (a ledge overhead or a ceiling is not taken for
        /// it), and that floor's top. A pit under the planned x moves the stone to its rim.
        /// </summary>
        private static Vector2 FloorNear(float x)
        {
            for (var step = 0; step <= 12; step++)
            {
                var dx = (step % 2 == 0 ? 1 : -1) * ((step + 1) / 2) * 0.5f;
                foreach (var from in new[] { 1.5f, 4f, 8f })
                {
                    var hit = Physics2D.Raycast(new Vector2(x + dx, from), Vector2.down, 6f);
                    if (hit.collider != null && hit.distance > 0f && hit.point.y > -2f)
                    {
                        return new Vector2(x + dx, hit.point.y);
                    }
                }
            }

            Debug.LogWarning($"No floor near the inscription at x {x}; stood at 0.");
            return new Vector2(x, 0f);
        }

        /// <summary>The ending lives beside the save it marks, in the world scene.</summary>
        private static void BuildEnding(GameProgress progress)
        {
            var ending = Ensure<EndingSequence>(progress.gameObject);
            SetReference(ending, "_progress", progress);
            SetReference(ending, "_respawner", Object.FindAnyObjectByType<PlayerRespawner>());
            SetReference(ending, "_player", Object.FindAnyObjectByType<PlayerCharacter>());
        }

        private static void StonePart(GameObject stone, string name, Vector2 at, Vector2 size, Color color)
        {
            var part = new GameObject(name);
            part.transform.SetParent(stone.transform, false);
            part.transform.localPosition = at;
            part.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = _sprite;
            renderer.sharedMaterial = _material;
            renderer.color = color;
            SetSorting(renderer, "EnvironmentBack", name == "Slab" ? 6 : 7);
        }
    }
}
