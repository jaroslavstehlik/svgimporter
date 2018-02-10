// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using System.Collections.Generic;

namespace SVGImporter.Utils
{
    public class LiteStack<T>
    {
        private int idx = 0;
        private List<T> stack = new List<T>();

        public void Push(T obj)
        {
            idx++;
            if (idx > stack.Count)
                stack.Add(obj);
            else
                stack [idx - 1] = obj;
        }

        public T Pop()
        {
            T tmp = Peek();
            if (idx > 0)
            {
                idx--;
                stack [idx] = default(T);
            }
            return tmp;
        }

        public T Peek()
        {
            if (idx > 0)
                return stack [idx - 1];
            else
                return default(T);
        }

        public int Count
        {
            get
            {
                return idx;
            }
        }

        public void Clear()
        {
            stack.Clear();
            idx = 0;
        }
    }

    public class LiteStack : LiteStack<object>
    {

    }
}
