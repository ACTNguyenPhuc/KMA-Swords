using System;
using UnityEngine;

namespace Oath
{
    /// <summary>Phát chuỗi khung hình sprite (sprite sheet của Tiny Swords đã được cắt sẵn).</summary>
    public class SpriteAnimator : MonoBehaviour
    {
        public SpriteRenderer target;
        public float fps = 10f;
        public bool loop = true;
        public bool destroyOnComplete;
        public bool useUnscaledTime;

        Sprite[] frames;
        float time;
        Action onComplete;
        bool completed;

        public Sprite[] Frames => frames;
        public float Duration => frames == null || frames.Length == 0 ? 0f : frames.Length / fps;

        void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
        }

        public void Play(Sprite[] newFrames, float newFps, bool newLoop, bool restart = false, Action complete = null, bool randomStart = false)
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (!restart && newFrames == frames && loop == newLoop) return;
            frames = newFrames;
            fps = newFps;
            loop = newLoop;
            onComplete = complete;
            completed = false;
            time = randomStart && frames != null && frames.Length > 0 ? UnityEngine.Random.value * Duration : 0f;
            Apply();
        }

        public void Play(string key, float newFps, bool newLoop, bool restart = false, Action complete = null)
        {
            Play(GameArt.I.Set(key), newFps, newLoop, restart, complete);
        }

        void Update()
        {
            if (frames == null || frames.Length == 0 || completed) return;
            time += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            if (frames == null || frames.Length == 0 || target == null) return;
            int i = Mathf.FloorToInt(time * fps);
            if (loop) i %= frames.Length;
            else if (i >= frames.Length)
            {
                i = frames.Length - 1;
                if (!completed)
                {
                    completed = true;
                    target.sprite = frames[i];
                    var cb = onComplete;
                    onComplete = null;
                    cb?.Invoke();
                    if (destroyOnComplete) Destroy(gameObject);
                    return;
                }
            }
            target.sprite = frames[i];
        }
    }
}
