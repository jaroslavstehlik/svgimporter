// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace SVGImporter
{
    [RequireComponent(typeof(SVGRenderer))]
    public class SVGLegacyAnimator : MonoBehaviour
    {
        public SVGAsset[] frames;

        [Serializable]
        public class OnCompleteEvent : UnityEvent<SVGLegacyAnimator>
        {
        }

        public enum WrapMode
        {
            ONCE,
            LOOP,
            PING_PONG
        }

        public WrapMode wrapMode = WrapMode.ONCE;
        public bool playOnAwake = true;
        public float duration = 1f;
        public float timeScale = 1f;
        public bool direction = true;
        public int loops = -1;
        public int currentLoop = 0;
        public bool rewind = false;
        public float progress = 0f;

        // Event delegates triggered on Min Angle.
        [FormerlySerializedAs("onComplete")]
        [SerializeField]
        protected OnCompleteEvent
            m_onComplete = new OnCompleteEvent();

        public OnCompleteEvent onComplete
        {
            get { return m_onComplete; }
            set { m_onComplete = value; }
        }

        protected bool _isPlaying;
        protected SVGRenderer svgRenderer;

        public void Play()
        {
            _isPlaying = true;
        }

        public void Stop()
        {
            currentLoop = 0;
            progress = 0f;
            _isPlaying = false;
        }

        public void Pause()
        {
            _isPlaying = false;
        }

        public void Restart()
        {
            Stop();
            Play();
        }

        public bool isPlaying
        {
            get
            {
                return _isPlaying;
            }
        }

        protected virtual void Awake()
        {
            svgRenderer = GetComponent<SVGRenderer>();
        }

        // Use this for initialization
        protected virtual void Start()
        {
            if (playOnAwake)
                Play();
        }
        
        // Update is called once per frame
        protected virtual void LateUpdate()
        {
            if (!_isPlaying)
                return;

            if (progress >= 0f && direction)
            {
                progress += Time.deltaTime * timeScale;
                
                if (progress >= duration)
                {
                    AnimationEnded();
                }
            } else if (progress <= duration && !direction)
            {
                progress -= Time.deltaTime * timeScale;
                
                if (progress <= 0)
                {
                    AnimationEnded();
                }
            }
            switch (wrapMode)
            {
                case WrapMode.ONCE:
                    progress = Mathf.Clamp(progress, 0f, duration);
                    break;
                case WrapMode.LOOP:
                    progress = Mathf.Repeat(progress, duration);
                    break;
                case WrapMode.PING_PONG:
                    progress = Mathf.Clamp(progress, 0f, duration);
                    break;
            }

            UpdateMesh();
        }

        public void UpdateMesh()
        {
            int vectorGraphicsIndex = Mathf.Clamp(Mathf.RoundToInt(normalizedProgress * frames.Length - 0.5f), 0, frames.Length -1);
            if (svgRenderer.vectorGraphics != frames [vectorGraphicsIndex])
            {
                svgRenderer.vectorGraphics = frames [vectorGraphicsIndex];
            }
        }

        void AnimationEnded()
        {
            switch (wrapMode)
            {
                case WrapMode.ONCE:
                    if (rewind)
                    {
                        Stop();
                    } else
                    {
                        _isPlaying = false;
                    }
                    m_onComplete.Invoke(this);
                    break;
                case WrapMode.LOOP:
                    if (loops >= 0 && currentLoop >= loops)
                    {
                        if (rewind)
                        {
                            Stop();
                        } else
                        {
                            currentLoop = loops;
                            _isPlaying = false;
                        }
                        m_onComplete.Invoke(this);
                    } else
                    {
                        currentLoop++;
                    }
                    break;
                case WrapMode.PING_PONG:
                    if (loops >= 0 && currentLoop >= loops)
                    {
                        if (rewind)
                        {
                            Stop();
                        } else
                        {
                            currentLoop = loops;
                            _isPlaying = false;
                        }
                        m_onComplete.Invoke(this);
                    } else
                    {
                        direction = !direction;
                        currentLoop++;
                    }
                    break;
            }
        }

        public float normalizedProgress
        {
            get
            {
                if (duration == 0f)
                    return 0f;
                return Mathf.Clamp01(progress / duration);
            }
        }
    }
}
