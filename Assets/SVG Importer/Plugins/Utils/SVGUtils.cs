// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using UnityEngine;
using System.Collections;

namespace SVGImporter.Utils
{        
    public static class SVGPropertyUtility
    {
        public static bool SetColor(ref Color currentValue, Color newValue)
        {
            if (currentValue.r == newValue.r && currentValue.g == newValue.g && currentValue.b == newValue.b && currentValue.a == newValue.a)
                return false;
            
            currentValue = newValue;
            return true;
        }
        
        public static bool SetStruct<T>(ref T currentValue, T newValue) where T: struct
        {
            if (currentValue.Equals(newValue))
            return false;
            
            currentValue = newValue;
            return true;
        }
        
        public static bool SetClass<T>(ref T currentValue, T newValue) where T: class
        {
            if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue)))
                return false;
            
            currentValue = newValue;
            return true;
        }
    }

    public static class SVGDeleagate {
        public static bool IsRegistered(System.Delegate source, System.Action compare)
        {   
            if (source == null || compare == null)
                return false;
            
            System.Delegate[] delegates = source.GetInvocationList();
            if (delegates == null || delegates.Length == 0)
                return false;
            
            for (int i = 0; i < delegates.Length; i++)
            {
                if(delegates[i].Equals(compare))
                    return true;
            }
            
            return false;
        }
        
        public static bool IsRegistered<T>(System.Delegate source, System.Action<T> compare)
        {   
            if (source == null || compare == null)
                return false;
            
            System.Delegate[] delegates = source.GetInvocationList();
            if (delegates == null || delegates.Length == 0)
                return false;
            
            for (int i = 0; i < delegates.Length; i++)
            {
                if(delegates[i].Equals(compare))
                    return true;
            }
            
            return false;
        }

        public static bool IsRegistered<T1, T2>(System.Delegate source, System.Action<T1, T2> compare)
        {   
            if (source == null || compare == null)
                return false;
            
            System.Delegate[] delegates = source.GetInvocationList();
            if (delegates == null || delegates.Length == 0)
                return false;
            
            for (int i = 0; i < delegates.Length; i++)
            {
                if(delegates[i].Equals(compare))
                    return true;
            }
            
            return false;
        }

        public static bool IsRegistered<T1, T2, T3>(System.Delegate source, System.Action<T1, T2, T3> compare)
        {   
            if (source == null || compare == null)
                return false;
            
            System.Delegate[] delegates = source.GetInvocationList();
            if (delegates == null || delegates.Length == 0)
                return false;
            
            for (int i = 0; i < delegates.Length; i++)
            {
                if(delegates[i].Equals(compare))
                    return true;
            }
            
            return false;
        }
        
        public static bool IsRegistered<T1, T2, T3, T4>(System.Delegate source, System.Action<T1, T2, T3, T4> compare)
        {   
            if (source == null || compare == null)
                return false;
            
            System.Delegate[] delegates = source.GetInvocationList();
            if (delegates == null || delegates.Length == 0)
                return false;
            
            for (int i = 0; i < delegates.Length; i++)
            {
                if(delegates[i].Equals(compare))
                    return true;
            }
            
            return false;
        }
    }
}
