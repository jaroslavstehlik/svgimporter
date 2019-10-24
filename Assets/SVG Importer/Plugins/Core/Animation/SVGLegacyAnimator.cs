// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
