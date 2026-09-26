using System.Collections.Generic;
using UnityEngine;

namespace ForgottenKingdom
{
    /// <summary>
    /// A small pool of burst objects.
    ///
    /// The cap matters as much as the reuse: past a point, more simultaneous effects do not make a
    /// fight read better, they make it read worse. When the pool is exhausted the oldest burst is
    /// taken over rather than a new object being created, so a spell hitting eight enemies cannot
    /// quietly become eight new allocations and a screen full of noise.
    /// </summary>
    public sealed class VfxPool
    {
        private readonly Transform _root;
        private readonly List<VfxBurst> _all = new List<VfxBurst>();
        private readonly Queue<VfxBurst> _idle = new Queue<VfxBurst>();
        private readonly int _capacity;
        private readonly Material _material;
        private readonly UnityEngine.Rendering.Universal.Light2D _lightTemplate;
        private int _nextToSteal;

        /// <param name="material">For every renderer in the pool; null keeps the default.</param>
        /// <param name="lightTemplate">Cloned into each entry when the pool is for light pulses.</param>
        public VfxPool(Transform root, int capacity, Material material = null,
            UnityEngine.Rendering.Universal.Light2D lightTemplate = null)
        {
            _root = root;
            _capacity = Mathf.Max(1, capacity);
            _material = material;
            _lightTemplate = lightTemplate;
        }

        public int Live
        {
            get
            {
                var count = 0;
                foreach (var burst in _all)
                {
                    if (burst != null && burst.IsPlaying)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public int Allocated => _all.Count;

        public VfxBurst Take()
        {
            while (_idle.Count > 0)
            {
                var pooled = _idle.Dequeue();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            if (_all.Count < _capacity)
            {
                return Create();
            }

            // At capacity: reuse the oldest in a rotation, so no single effect can starve the rest.
            for (var i = 0; i < _all.Count; i++)
            {
                _nextToSteal = (_nextToSteal + 1) % _all.Count;
                var candidate = _all[_nextToSteal];
                if (candidate != null)
                {
                    candidate.Cancel();
                    return candidate;
                }
            }

            return Create();
        }

        public void Return(VfxBurst burst)
        {
            if (burst != null)
            {
                _idle.Enqueue(burst);
            }
        }

        /// <summary>Stops everything at once, for death, respawn and scene changes.</summary>
        public void ClearAll()
        {
            _idle.Clear();
            foreach (var burst in _all)
            {
                if (burst == null)
                {
                    continue;
                }

                burst.Cancel();
                _idle.Enqueue(burst);
            }
        }

        private VfxBurst Create()
        {
            var host = new GameObject("VfxBurst");
            host.transform.SetParent(_root, false);
            var renderer = host.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            if (_material != null)
            {
                renderer.sharedMaterial = _material;
            }

            if (_lightTemplate != null)
            {
                var light = Object.Instantiate(_lightTemplate, host.transform, false);
                light.transform.localPosition = Vector3.zero;
                light.gameObject.SetActive(true);
                light.enabled = false;
            }

            var burst = host.AddComponent<VfxBurst>();
            _all.Add(burst);
            return burst;
        }
    }
}
