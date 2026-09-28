using UnityEditor;
using UnityEngine;

namespace ForgottenKingdom.EditorTools
{
    /// <summary>
    /// Enemy animation 2.0 on the prefabs (visual item 4, the user's choices of 2026-09-28): the
    /// loose piece hung on the frame's anchor, the squash of a landed blow and the body crumbling
    /// to ash after death. The windup itself is in the frames (<see cref="PlaceholderEnemyArt"/>).
    /// </summary>
    public static partial class ContentBuilder
    {
        private const string AshMaterialPath = "Assets/Game/Art/Shapes/EmberParticles.mat";

        /// <summary>Seconds from death to the first crack: the death clip, then a still moment.</summary>
        private const float EnemyCrumbleDelay = 2.4f;
        private const float BossCrumbleDelay = 3.8f;

        private static System.Collections.Generic.Dictionary<string, Texture2D> _trails;

        private static void BuildEnemyLife(GameObject root, GameObject visual, MonoBehaviour animation,
            SpriteRenderer body, bool isBoss)
        {
            _trails ??= PlaceholderEnemyArt.GenerateTrails();
            if (PlaceholderEnemyArt.Trails.TryGetValue(root.name, out var spec)
                && _trails.TryGetValue(root.name, out var texture))
            {
                BuildLooseTrail(visual, animation, body, texture, spec);
            }

            var recoil = Ensure<HitRecoil>(root);
            SetReference(recoil, "_actor", root.GetComponent<CombatActor>());
            SetReference(recoil, "_body", body.transform);
            // A boss is too heavy to squat under every blow; it only gives a little.
            SetFloat(recoil, "_squash", isBoss ? 0.06f : 0.14f);
            SetFloat(recoil, "_tilt", isBoss ? 3f : 9f);

            var crumble = Ensure<CorpseCrumble>(root);
            SetReference(crumble, "_health", root.GetComponent<Health>());
            SetReference(crumble, "_visualRoot", visual);
            SetReference(crumble, "_body", body);
            SetReference(crumble, "_ash", BuildAsh(root, isBoss));
            SetFloat(crumble, "_delay", isBoss ? BossCrumbleDelay : EnemyCrumbleDelay);
            SetFloat(crumble, "_crumbleTime", isBoss ? 1.2f : 0.7f);
        }

        /// <summary>The hero's cloak chain, reused: hangs off the body on each frame's anchor.</summary>
        private static void BuildLooseTrail(GameObject visual, MonoBehaviour animation, SpriteRenderer body,
            Texture2D texture, PlaceholderEnemyArt.TrailSpec spec)
        {
            var tailObject = FindOrCreateChild(visual, "LooseTrail");
            tailObject.transform.localPosition = Vector3.zero;
            var line = Ensure<LineRenderer>(tailObject);
            line.useWorldSpace = true;
            line.positionCount = CloakTail.Links + 1;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCornerVertices = 2;
            line.numCapVertices = 0;
            line.widthMultiplier = spec.Width;
            line.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.75f));
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = body.sharedMaterial != null ? body.sharedMaterial : _material;
            line.sortingLayerName = "Characters";
            line.sortingOrder = 2;

            var tail = Ensure<CloakTail>(tailObject);
            SetReference(tail, "_animation", animation);
            SetReference(tail, "_body", body);
            SetReference(tail, "_texture", texture);
            SetFloat(tail, "_length", spec.Length);
            SetFloat(tail, "_stiffness", spec.Stiffness);
            SetFloat(tail, "_damping", spec.Damping);
        }

        /// <summary>
        /// The ash a body leaves: dark flakes that lift and drift, on the root rather than the
        /// visual, so they keep falling after the body is gone.
        /// </summary>
        private static ParticleSystem BuildAsh(GameObject root, bool isBoss)
        {
            var host = FindOrCreateChild(root, "Ash");
            host.transform.localPosition = Vector3.zero;
            var ash = Ensure<ParticleSystem>(host);
            ash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ash.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, isBoss ? 0.12f : 0.09f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.19f, 0.18f, 0.95f),
                new Color(0.42f, 0.4f, 0.37f, 0.85f));
            main.gravityModifier = -0.06f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = isBoss ? 220 : 90;

            var emission = ash.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)(isBoss ? 90 : 36)),
                new ParticleSystem.Burst(0.25f, (short)(isBoss ? 70 : 26))
            });

            var shape = ash.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(0.8f, 1.4f, 0.1f);

            var noise = ash.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.8f;

            var fade = ash.colorOverLifetime;
            fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;

            var renderer = host.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(AshMaterialPath);
            renderer.sortingLayerName = "VFXFront";
            renderer.sortingOrder = 0;
            return ash;
        }
    }
}
