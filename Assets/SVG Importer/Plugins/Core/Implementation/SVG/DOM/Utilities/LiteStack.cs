// Copyright (C) 2019 Jaroslav Stehlik
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
