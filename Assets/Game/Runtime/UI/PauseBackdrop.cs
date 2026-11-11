using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace ForgottenKingdom
{
    /// <summary>
    /// The world behind the pause menu, out of focus (GAME_FEEL_IDEAS item 18). The game stands still
    /// under a pause, so one picture is enough: the camera is rendered once into a small texture, that
    /// is softened by passing it down and back up through smaller ones, and the menu's overlay wears it
    /// darkened. No renderer feature and no shader of its own; without a camera the plain dimming stays.
    /// </summary>
    public sealed class PauseBackdrop
    {
        /// <summary>How dark the picture is under the menu: the text has to stay the brightest thing.</summary>
        private static readonly Color Tint = new Color(0.36f, 0.34f, 0.34f, 1f);

        private RenderTexture _picture;

        public bool IsShown => _picture != null;

        public void Show(VisualElement overlay)
        {
            var camera = Camera.main;
            if (overlay == null || camera == null || Screen.width < 16 || Screen.height < 16)
            {
                return;
            }

            Release();
            var width = Mathf.Max(8, Screen.width / 2);
            var height = Mathf.Max(8, Screen.height / 2);
            var sharp = RenderTexture.GetTemporary(width, height, 24);
            if (!Render(camera, sharp))
            {
                RenderTexture.ReleaseTemporary(sharp);
                return;
            }

            _picture = Soften(sharp);
            RenderTexture.ReleaseTemporary(sharp);
            overlay.style.backgroundImage = Background.FromRenderTexture(_picture);
            overlay.style.unityBackgroundImageTintColor = Tint;
        }

        public void Hide(VisualElement overlay)
        {
            if (overlay != null)
            {
                overlay.style.backgroundImage = StyleKeyword.Null;
                overlay.style.unityBackgroundImageTintColor = StyleKeyword.Null;
            }

            Release();
        }

        private static bool Render(Camera camera, RenderTexture target)
        {
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
            {
                return false;
            }

            RenderPipeline.SubmitRenderRequest(camera, request);
            return true;
        }

        /// <summary>Down to a sixteenth and back to a quarter: each step is a small blur of its own.</summary>
        private static RenderTexture Soften(RenderTexture source)
        {
            var current = source;
            RenderTexture previous = null;
            foreach (var divisor in new[] { 2, 4, 8, 4, 2 })
            {
                var next = RenderTexture.GetTemporary(Mathf.Max(4, source.width / divisor),
                    Mathf.Max(4, source.height / divisor), 0);
                next.filterMode = FilterMode.Bilinear;
                Graphics.Blit(current, next);
                if (previous != null)
                {
                    RenderTexture.ReleaseTemporary(previous);
                }

                previous = next;
                current = next;
            }

            var result = new RenderTexture(current.width, current.height, 0)
            {
                name = "PauseBackdrop",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Graphics.Blit(current, result);
            RenderTexture.ReleaseTemporary(current);
            return result;
        }

        private void Release()
        {
            if (_picture != null)
            {
                _picture.Release();
                Object.Destroy(_picture);
                _picture = null;
            }
        }
    }
}
