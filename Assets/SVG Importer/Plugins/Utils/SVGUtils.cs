// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

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
