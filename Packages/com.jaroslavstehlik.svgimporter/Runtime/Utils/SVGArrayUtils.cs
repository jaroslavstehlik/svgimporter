

using UnityEngine;
using System.Collections;

namespace SVGImporter.Utils
{
    public class SVGArrayUtils {

        public static T[] CreatePreinitializedArray<T>(T value, int length)
        {
            T[] array = new T[length];
            for (int i = 0; i < length; i++)
            {
                array[i] = value;
            }
            return array;
        }
    }
}