using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Reflection;

namespace SVGImporter
{
    public class EditorCoroutine
    {
        class Coroutine
        {
            public ICoroutineYield CurrentYield = new CoroutineDefault();
            public IEnumerator Routine;
            public string RoutineUniqueHash;
            public string OwnerUniqueHash;
            public string MethodName = "";
            public int OwnerHash;
            public string OwnerType;

            public Coroutine(IEnumerator routine, int ownerHash, string ownerType)
            {
                this.Routine = routine;
                this.OwnerHash = ownerHash;
                this.OwnerType = ownerType;
                this.OwnerUniqueHash = ownerHash + "_" + ownerType;

                if (routine != null)
                {
                    string[] split = routine.ToString().Split('<', '>');
                    if (split.Length == 3)
                    {
                        this.MethodName = split [1];
                    }
                }

                this.RoutineUniqueHash = ownerHash + "_" + ownerType + "_" + MethodName;
            }

            public Coroutine(string methodName, int ownerHash, string ownerType)
            {
                this.MethodName = methodName;
                this.OwnerHash = ownerHash;
                this.OwnerType = ownerType;
                this.OwnerUniqueHash = ownerHash + "_" + ownerType;
                this.RoutineUniqueHash = ownerHash + "_" + ownerType + "_" + MethodName;
            }
        }

        abstract class ICoroutineYield
        {
            public abstract bool IsDone(float deltaTime);
        }

        class CoroutineDefault : ICoroutineYield
        {
            public override bool IsDone(float deltaTime)
            {
                return true;
            }
        }

        class CoroutineWaitForSeconds : ICoroutineYield
        {
            public float TimeLeft;

            public override bool IsDone(float deltaTime)
            {
                TimeLeft -= deltaTime;
                return TimeLeft < 0; 
            }
        }

        class CoroutineWWW : ICoroutineYield
        {
            public WWW Www;

            public override bool IsDone(float deltaTime)
            {
                return Www.isDone;
            }
        }

        class CoroutineAsync : ICoroutineYield
        {
            public AsyncOperation asyncOperation;

            public override bool IsDone(float deltaTime)
            {
                return asyncOperation.isDone;
            }

        }

        static EditorCoroutine instance = null;
        Dictionary<string, List<Coroutine>> coroutineDict = new Dictionary<string, List<Coroutine>>();
        Dictionary<string, Dictionary<string, Coroutine>> coroutineOwnerDict = new Dictionary<string, Dictionary<string, Coroutine>>();
        DateTime previousTimeSinceStartup;

        /// <summary>Starts a coroutine.</summary>
        /// <param name="routine">The coroutine to start.</param>
        /// <param name="thisReference">Reference to the instance of the class containing the method.</param>
        public static void StartCoroutine(IEnumerator routine, object thisReference)
        {
            CreateInstanceIfNeeded();
            instance.GoStartCoroutine(routine, thisReference);
        }

        /// <summary>Starts a coroutine.</summary>
        /// <param name="methodName">The name of the coroutine method to start.</param>
        /// <param name="thisReference">Reference to the instance of the class containing the method.</param>
        public static void StartCoroutine(string methodName, object thisReference)
        {
            StartCoroutine(methodName, null, thisReference);
        }

        /// <summary>Starts a coroutine.</summary>
        /// <param name="methodName">The name of the coroutine method to start.</param>
        /// <param name="value">The parameter to pass to the coroutine.</param>
        /// <param name="thisReference">Reference to the instance of the class containing the method.</param>
        public static void StartCoroutine(string methodName, object value, object thisReference)
        {
            MethodInfo methodInfo = thisReference.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
            {
                Debug.LogError("Coroutine '" + methodName + "' couldn't be started, the method doesn't exist!");
                return;
            }
            object returnValue;

            if (value == null)
            {
                returnValue = methodInfo.Invoke(thisReference, null);
            } else
            {
                returnValue = methodInfo.Invoke(thisReference, new object[] { value });
            }

            if (returnValue is IEnumerator)
            {
                CreateInstanceIfNeeded();
                instance.GoStartCoroutine((IEnumerator)returnValue, thisReference);
            } else
            {
                Debug.LogError("Coroutine '" + methodName + "' couldn't be started, the method doesn't return an IEnumerator!");
            }

        }

        /// <summary>Stops all coroutines being the routine running on the passed instance.</summary>
        /// <param name="routine"> The coroutine to stop.</param>
        /// <param name="thisReference">Reference to the instance of the class containing the method.</param>
        public static void StopCoroutine(IEnumerator routine, object thisReference)
        {
            CreateInstanceIfNeeded();
            instance.GoStopCoroutine(routine, thisReference);
        }

        /// <summary>
        /// Stops all coroutines named methodName running on the passed instance.</summary>
        /// <param name="methodName"> The name of the coroutine method to stop.</param>
        /// <param name="thisReference">Reference to the instance of the class containing the method.</param>
        public static void StopCoroutine(string methodName, object thisReference)
        {
            CreateInstanceIfNeeded();
            instance.GoStopCoroutine(methodName, thisReference);
        }

        /// <summary>
        /// Stops all coroutines running on the passed instance.</summary>
        /// <param name="thisReference">Reference to the instance of the class containing the method.</param>
        public static void StopAllCoroutines(object thisReference)
        {
            CreateInstanceIfNeeded();
            instance.GoStopAllCoroutines(thisReference);
        }

        static void CreateInstanceIfNeeded()
        {
            if (instance == null)
            {
                instance = new EditorCoroutine();
                instance.Initialize();
            }
        }

        void Initialize()
        {
            previousTimeSinceStartup = DateTime.Now;
            EditorApplication.update += OnUpdate;
        }

        void GoStopCoroutine(IEnumerator routine, object thisReference)
        {
            GoStopActualRoutine(CreateCoroutine(routine, thisReference));
            
        }

        void GoStopCoroutine(string methodName, object thisReference)
        {
            GoStopActualRoutine(CreateCoroutineFromString(methodName, thisReference));
        }

        void GoStopActualRoutine(Coroutine routine)
        {
            if (coroutineDict.ContainsKey(routine.RoutineUniqueHash))
            {
                coroutineOwnerDict [routine.OwnerUniqueHash].Remove(routine.RoutineUniqueHash);
                coroutineDict.Remove(routine.RoutineUniqueHash);
            }
        }

        void GoStopAllCoroutines(object thisReference)
        {
            Coroutine coroutine = CreateCoroutine(null, thisReference);
            if (coroutineOwnerDict.ContainsKey(coroutine.OwnerUniqueHash))
            {
                foreach (var couple in coroutineOwnerDict[coroutine.OwnerUniqueHash])
                {
                    coroutineDict.Remove(couple.Value.RoutineUniqueHash);
                }
                coroutineOwnerDict.Remove(coroutine.OwnerUniqueHash);
            }
        }

        void GoStartCoroutine(IEnumerator routine, object thisReference)
        {
            if (routine == null)
            {
                Debug.LogException(new Exception("IEnumerator is null!"), null);
            }
            Coroutine coroutine = CreateCoroutine(routine, thisReference);

            if (!coroutineDict.ContainsKey(coroutine.RoutineUniqueHash))
            {
                List<Coroutine> newCoroutineList = new List<Coroutine>();
                coroutineDict.Add(coroutine.RoutineUniqueHash, newCoroutineList);
            }
            coroutineDict [coroutine.RoutineUniqueHash].Add(coroutine);

            if (!coroutineOwnerDict.ContainsKey(coroutine.OwnerUniqueHash))
            {
                Dictionary<string, Coroutine> newCoroutineDict = new Dictionary<string, Coroutine>();
                coroutineOwnerDict.Add(coroutine.OwnerUniqueHash, newCoroutineDict);
            }

            // If the method from the same owner has been stored before, it doesn't have to be stored anymore,
            // One reference is enough in order for "StopAllCoroutines" to work
            if (!coroutineOwnerDict [coroutine.OwnerUniqueHash].ContainsKey(coroutine.RoutineUniqueHash))
            {
                coroutineOwnerDict [coroutine.OwnerUniqueHash].Add(coroutine.RoutineUniqueHash, coroutine);
            }

            MoveNext(coroutine);
        }

        Coroutine CreateCoroutine(IEnumerator routine, object thisReference)
        {
            return new Coroutine(routine, thisReference.GetHashCode(), thisReference.GetType().ToString());
        }

        Coroutine CreateCoroutineFromString(string methodName, object thisReference)
        {
            return new Coroutine(methodName, thisReference.GetHashCode(), thisReference.GetType().ToString());
        }

        void OnUpdate()
        {
            float deltaTime = (float)(DateTime.Now.Subtract(previousTimeSinceStartup).TotalMilliseconds / 1000.0f);

            previousTimeSinceStartup = DateTime.Now;
            if (coroutineDict.Count == 0)
            {
                return;
            }

            List<string> removals = new List<string>();

            foreach (var pair in coroutineDict)
            {
                List<Coroutine> coroutines = pair.Value;
                
                for (int i = coroutines.Count-1; i >= 0; i--)
                {
                    Coroutine coroutine = coroutines [i];

                    if (!coroutine.CurrentYield.IsDone(deltaTime))
                    {
                        continue;
                    }

                    if (!MoveNext(coroutine))
                    {
                        coroutines.RemoveAt(i);
                    }
                }

                if (coroutines.Count == 0)
                {
                    removals.Add(pair.Key);
                }
            }
            foreach (string toRemove in removals)
            {
                coroutineDict.Remove(toRemove);
            }
        }

        bool MoveNext(Coroutine coroutine)
        {
            if (coroutine.Routine.MoveNext())
            {
                return Process(coroutine);
            }

            return false;
        }
        
        // returns false if no next, returns true if OK
        bool Process(Coroutine coroutine)
        {
            object current = coroutine.Routine.Current;
            if (current == null)
            {
                return false;
            } else if (current is WaitForSeconds)
            {
                float seconds = float.Parse(GetInstanceField(typeof(WaitForSeconds), current, "m_Seconds").ToString());
                coroutine.CurrentYield = new CoroutineWaitForSeconds() { TimeLeft = (float)seconds };
            } else if (current is WWW)
            {
                coroutine.CurrentYield = new CoroutineWWW() { Www = (WWW) current };
            } else if (current is WaitForFixedUpdate)
            {
                coroutine.CurrentYield = new CoroutineDefault();
            } else if (current is AsyncOperation)
            {
                coroutine.CurrentYield = new CoroutineAsync() { asyncOperation = (AsyncOperation)current };
            } else
            {
                Debug.LogException(new Exception("<" + coroutine.MethodName + "> yielded an unknown or unsupported type! (" + current.GetType() + ")"), null);
                coroutine.CurrentYield = new CoroutineDefault();
            }
            return true;
        }

        static object GetInstanceField(Type type, object instance, string fieldName)
        {
            BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            FieldInfo field = type.GetField(fieldName, bindFlags);
            return field.GetValue(instance);
        }
    }
}
