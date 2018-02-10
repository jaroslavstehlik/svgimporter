// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;

using System.Collections.Generic;
using System.Collections;
using System;

namespace SVGImporter.Rendering
{
    [System.Serializable]
    public struct CCGradientColorKey {
        public float time;
        public Color32 color;
        
        public CCGradientColorKey (Color32 color, float time)
        {
            this.time = time;
            this.color = color;
        }

        public override string ToString()
        {
            return string.Format("[CCGradientColorKey: time={0}, color={1}", time, color);
        }
    }

    [System.Serializable]
    public struct CCGradientAlphaKey {
        public float time;
        public float alpha;

    	public CCGradientAlphaKey (float alpha, float time)
    	{
    		this.time = time;
            this.alpha = alpha;
    	}
    	
        public override string ToString()
        {
            return string.Format("[CCGradientAlphaKey: time={0}, alpha={1}]", time, alpha);
        }
    }

    [System.Serializable]
    public class CCGradient : System.Object {
        public const string DEFAULT_GRADIENT_HASH = "GC999FFFFFFC000FFFFFFA999999A000999";
    	public CCGradientColorKey[] colorKeys;
    	public CCGradientAlphaKey[] alphaKeys;
    	
    	public string hash {
    		get {
                string hash = "G";
                
                if (colorKeys != null && colorKeys.Length > 0) {
                    for (int i = 0; i < colorKeys.Length; i++) {
                        hash += "C"+(Mathf.RoundToInt(colorKeys[i].time * 999)).ToString("000") + ColorToHex (colorKeys[i].color);
                    }
                }
                
                if (alphaKeys != null && alphaKeys.Length > 0) {
                    for (int i = 0; i < alphaKeys.Length; i++) {
                        //                    Debug.Log(alphaKeys[i]);
                        hash += "A"+(Mathf.RoundToInt(alphaKeys [i].time * 999)).ToString("000") + (Mathf.RoundToInt(alphaKeys [i].alpha * 999)).ToString("000");
                    }
                }
                return hash;
    		}
    	}
    	
    	public int index;

        [HideInInspector]
        [System.NonSerialized]
    	public int atlasIndex;

        [HideInInspector]
        [System.NonSerialized]
        protected List<ISVGReference> _references;
        public List<ISVGReference> references
        {
            get {
                return _references;
            }
        }
        
        [System.NonSerialized]
        public System.Action<ISVGReference> onReferenceAdded;
        [System.NonSerialized]
        public System.Action<ISVGReference> onReferenceRemoved;
        
        public bool AddReference(ISVGReference reference)
        {
            if(_references == null)
            {
                _references = new List<ISVGReference>{ reference };
                if(onReferenceAdded != null) onReferenceAdded(reference);
                return true;
            } else {
                if(!_references.Contains(reference))
                {
                    _references.Add(reference);
                    if(onReferenceAdded != null) onReferenceAdded(reference);
                    return true;
                } else {
                    return false;
                }
            }
        }
        
        public bool RemoveReference(ISVGReference reference)
        {
            if(_references != null)
            {
                bool removed = _references.Remove(reference);
                if(removed)
                {
                    if(onReferenceRemoved != null) onReferenceRemoved(reference);
                }
                return removed;
            } else {
                return false;
            }
        }

        public int CountReferences(ISVGReference reference)
        {
            int count = 0;
            if(_references != null)
            {
                for(int i = 0; i < _references.Count; i++)
                {
                    if(_references[i] == reference) count++;
                }
            }

            return count;
        }
        
        public int referenceCount
        {
            get {
                if(_references == null) return 0;
                return _references.Count;
            }
        }

        public CCGradient (CCGradientColorKey[] colorKeys, CCGradientAlphaKey[] alphaKeys, bool sort = true)
    	{
    		SetKeys(colorKeys, alphaKeys, sort);
    	}
    	
    	public void SetKeys (CCGradientColorKey[] colorKeys, CCGradientAlphaKey[] alphaKeys, bool sort = true)
    	{
            this.colorKeys = (CCGradientColorKey[])colorKeys.Clone();
            this.alphaKeys = (CCGradientAlphaKey[])alphaKeys.Clone();

            if(sort)
            {
                Array.Sort<CCGradientColorKey>(this.colorKeys, (x, y) => y.time.CompareTo(x.time));
                Array.Sort<CCGradientAlphaKey>(this.alphaKeys, (x, y) => y.time.CompareTo(x.time));
            }

            if (this.alphaKeys == null || this.alphaKeys.Length == 0) {
                this.alphaKeys = new CCGradientAlphaKey[]{
                    new CCGradientAlphaKey (1f, 0f),
                    new CCGradientAlphaKey (1f, 1f)
                };			
    		}	    		
    	}
    	
    	public Color32 Evaluate (float time)
    	{
    		time = Mathf.Clamp01 (time);
            Color32 output;
    		
            if(colorKeys == null || colorKeys.Length == 0)
            {
                output = new Color32(0, 0, 0, 255);
            } else if (colorKeys.Length == 1) {
                output = colorKeys [0].color;
    		} else {
                int colorKeysCount = colorKeys.Length;
    			float minDistance = float.MaxValue;
    			float distance = 0f;
    			int tempIndex = 0;
    			
    			for (int i = 0; i < colorKeysCount; i++) {
    				distance = Mathf.Abs (colorKeys [i].time - time);
    				if (distance < minDistance) {
    					minDistance = distance;
                        tempIndex = i;
    				} else if (distance > minDistance) {
    					break;
    				} else {
    					minDistance = distance;
                        tempIndex = i;					
    					break;
    				}
    			}

                if (colorKeys [tempIndex].time > time) {
    				int minIndex = tempIndex, maxIndex = Mathf.Clamp (tempIndex + 1, 0, colorKeysCount - 1);
    				output = Color32.Lerp (colorKeys [minIndex].color, colorKeys [maxIndex].color, Mathf.InverseLerp (colorKeys [minIndex].time, colorKeys [maxIndex].time, time));
    			} else if (colorKeys [tempIndex].time < time) {
    				int minIndex = Mathf.Clamp (tempIndex - 1, 0, colorKeysCount - 1), maxIndex = tempIndex;
    				output = Color32.Lerp (colorKeys [minIndex].color, colorKeys [maxIndex].color, Mathf.InverseLerp (colorKeys [minIndex].time, colorKeys [maxIndex].time, time));				
    			} else {
    				output = colorKeys [tempIndex].color;
    			}		
    		}
    		
            if (alphaKeys == null || alphaKeys.Length == 0) 
            {
                output.a = 255;
            } else if (alphaKeys.Length == 1) {
    			output.a = (byte)(Mathf.RoundToInt (alphaKeys [0].alpha * 255));
    		} else {
                int alphaKeysCount = alphaKeys.Length;
    			float minDistance = float.MaxValue;
    			float distance = 0f;
    			int tempIndex = 0;
    			
    			for (int i = 0; i < alphaKeysCount; i++) {
    				distance = Mathf.Abs (alphaKeys [i].time - time);
    				if (distance < minDistance) {
    					minDistance = distance;
    					tempIndex = i;
    				} else if (distance > minDistance) {
    					break;
    				} else {
    					minDistance = distance;
    					tempIndex = i;
    					break;
    				}
    			}
    			
                if (alphaKeys [tempIndex].time > time) {				
    				int minIndex = tempIndex, maxIndex = Mathf.Clamp (tempIndex + 1, 0, alphaKeysCount - 1);				
    				output.a = (byte)(Mathf.RoundToInt (Mathf.Lerp ((float)alphaKeys [minIndex].alpha, (float)alphaKeys [maxIndex].alpha, Mathf.InverseLerp (alphaKeys [minIndex].time, alphaKeys [maxIndex].time, time)) * 255));
    			} else if (alphaKeys [tempIndex].time < time) {
    				int minIndex = Mathf.Clamp (tempIndex - 1, 0, alphaKeysCount - 1), maxIndex = tempIndex;
    				output.a = (byte)(Mathf.RoundToInt (Mathf.Lerp ((float)alphaKeys [minIndex].alpha, (float)alphaKeys [maxIndex].alpha, Mathf.InverseLerp (alphaKeys [minIndex].time, alphaKeys [maxIndex].time, time)) * 255));
    			} else {
    				output.a = (byte)Mathf.RoundToInt (alphaKeys [tempIndex].alpha * 255);
    			}
    		}
    		
    		return output;
    	}
    	
    	public Color32 ApproximateColor (int samples)
    	{
    		int r = 0, g = 0, b = 0, a = 0;
			float totalTime = samples - 1;//, progress = 0f;
    		Color32 color;
    		for (float i = 0; i < samples; i++) {
    			color = Evaluate (i / totalTime);
    			r += color.r;
    			g += color.g;
    			b += color.b;
    			a += color.a;
    		}
    		
    		r = Mathf.Clamp(Mathf.RoundToInt ((float)r / (float)samples), 0, 255);
    		g = Mathf.Clamp(Mathf.RoundToInt ((float)g / (float)samples), 0, 255);
    		b = Mathf.Clamp(Mathf.RoundToInt ((float)b / (float)samples), 0, 255);
    		a = Mathf.Clamp (Mathf.RoundToInt ((float)a / (float)samples), 0, 255);
    		
    		return new Color32 ((byte)r, (byte)g, (byte)b, (byte)a);
    	}
    	
    	public bool initialised
    	{
    		get {
    			return (colorKeys != null && alphaKeys != null && colorKeys.Length != 0 && alphaKeys.Length != 0);
    		}
    	}
    	
        public CCGradient Clone()
        {
            if(this.colorKeys == null || this.alphaKeys == null)
                return null;

            CCGradient gradient = new CCGradient((CCGradientColorKey[])this.colorKeys.Clone(), 
                                                 (CCGradientAlphaKey[])this.alphaKeys.Clone(), false);
            gradient.index = this.index;
            gradient.atlasIndex = this.atlasIndex;
            return gradient;
        }

        public override string ToString()
        {
            string output = string.Format("[CCGradient: initialised={0}, index={1}, atlasIndex={2}]",  hash, initialised, index, atlasIndex);

            if(colorKeys != null && colorKeys.Length > 0)
            {
                output += "\nColorKeys:\n";
                for(int i = 0; i < colorKeys.Length; i++)
                {
                    output += colorKeys[i].ToString()+"\n";
                }
            }

            if(alphaKeys != null && alphaKeys.Length > 0)
            {
                output += "\nAlphaKeys:\n";
                for(int i = 0; i < alphaKeys.Length; i++)
                {
                    output += alphaKeys[i].ToString()+"\n";
                }
            }

            return output;
        }

        // Note that Color32 and Color implictly convert to each other. You may pass a Color object to this method without first casting it.
        public static string ColorToHex (Color32 color)
        {
            string hex = color.r.ToString ("X2") + color.g.ToString ("X2") + color.b.ToString ("X2");
            return hex;
        }
        
        public static Color HexToColor (string hex)
        {
            byte r, g, b;
            byte.TryParse (hex.Substring (0, 2), System.Globalization.NumberStyles.HexNumber, null as IFormatProvider, out r);
            byte.TryParse (hex.Substring (2, 2), System.Globalization.NumberStyles.HexNumber, null as IFormatProvider, out g);
            byte.TryParse (hex.Substring (4, 2), System.Globalization.NumberStyles.HexNumber, null as IFormatProvider, out b);
            return new Color32 (r, g, b, 255);
        }
    }
}
