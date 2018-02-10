// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEngine;
using UnityEditor;

using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace SVGImporter
{
    using Utils;

	internal class DelayedCall
    {
        public System.Action callback;
        public System.Action<UnityEngine.WWW> wwwCallback;
        public DelayedCall(System.Action callback = null)
        {
            this.callback = callback;
        }

        public IEnumerator Delay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if(this.callback != null)
                this.callback();
        }

		public IEnumerator WWW(string request)
		{
            UnityEngine.WWW www = new UnityEngine.WWW(request);
            yield return www;
            if(this.callback != null)
            {
                this.callback();
            }
            if(this.wwwCallback != null)
            {
                this.wwwCallback(www);
            }
		}
    }

    [InitializeOnLoad]
    public class SVGImporterLaunchEditor {

		public static void OpenAboutWindow()
		{
			//Analytics.TrackEvent("Open About Window", "app/about");
		}

		public static void OpenSettingsWindow()
		{
			//Analytics.TrackEvent("Open Settings Window", "app/settings");
		}
		
		public static void OpenReportBugWindow()
		{
			//Analytics.TrackEvent("Open Report Bug", "app/ReportBug");
		}

        static DelayedCall initCall;

        // Begining
		static bool launched = false;
        static SVGImporterLaunchEditor() {

			if(launched)
			{
				return;
			} else {
				launched = true;
			}

            initCall = new DelayedCall(DelayedInit);

            if (EditorPrefs.HasKey(SVG_IMPORTER_WINDOW_PREF_KEY))
            {
                _active = EditorPrefs.GetBool(SVG_IMPORTER_WINDOW_PREF_KEY);
            } else
            {
                _active = true;
            }

            //HideGizmos();
            SVGPostprocessor.Init();
            UpdateDelegates();

			//Analytics.TrackEvent("Start App, Version: "+SVGImporterSettings.version, "app/start");
            EditorCoroutine.StartCoroutine(initCall.Delay(0.5f), initCall);
        }

        static void DelayedInit()
        {
            SVGImporterEditor.Init();
        }

        static bool repaint;
        const string SVG_IMPORTER_WINDOW_PREF_KEY = "SVG_IMPORTER_WINDOW_PREF_KEY";
        static bool _active = false;
        public static bool active
        {
            get {
                if (EditorPrefs.HasKey(SVG_IMPORTER_WINDOW_PREF_KEY))
                {
                    _active = EditorPrefs.GetBool(SVG_IMPORTER_WINDOW_PREF_KEY);
                } else
                {
                    _active = true;
                }

                return _active;
            }
            set {
                EditorPrefs.SetBool(SVG_IMPORTER_WINDOW_PREF_KEY, value);
                _active = value;
            }
        }

        static void UpdateDelegates()
        {
            if(_active)
            {
                RegisterDelegates();
            } else {
                UnregisterDelegates();
            }
        }

        static void RegisterDelegates()
        {
            if(!SVGDeleagate.IsRegistered(EditorApplication.playmodeStateChanged, PlaymodeStateChanged))
                EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
            if(!SVGDeleagate.IsRegistered<SceneView>(SceneView.onSceneGUIDelegate, OnSceneGUI))
                SceneView.onSceneGUIDelegate += OnSceneGUI;
            if(!SVGDeleagate.IsRegistered<GameObject>(PrefabUtility.prefabInstanceUpdated, OnPrefabInstanceUpdated))
                PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;
            if(!SVGDeleagate.IsRegistered<int, Rect>(EditorApplication.hierarchyWindowItemOnGUI, HierarchyWindowItemOnGUI))
                EditorApplication.hierarchyWindowItemOnGUI += HierarchyWindowItemOnGUI;
			if(!SVGDeleagate.IsRegistered(EditorApplication.update, Update))
                EditorApplication.update += Update;
        }

        static void Update()
        {

			if(!Application.isPlaying)
			{
				if(SVGAtlas.Instance.atlasHasChanged)
				{
					SVGAtlas.Instance.OnAtlasPreRender();
					SceneView.RepaintAll();
				}
			}
        }

        static void UnregisterDelegates()
        {
            if(SVGDeleagate.IsRegistered(EditorApplication.playmodeStateChanged, PlaymodeStateChanged))
                EditorApplication.playmodeStateChanged -= PlaymodeStateChanged;
            if(SVGDeleagate.IsRegistered<SceneView>(SceneView.onSceneGUIDelegate, OnSceneGUI))
                SceneView.onSceneGUIDelegate -= OnSceneGUI;
            if(SVGDeleagate.IsRegistered<GameObject>(PrefabUtility.prefabInstanceUpdated, OnPrefabInstanceUpdated))
                PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
            if(SVGDeleagate.IsRegistered<int, Rect>(EditorApplication.hierarchyWindowItemOnGUI, HierarchyWindowItemOnGUI))
                EditorApplication.hierarchyWindowItemOnGUI -= HierarchyWindowItemOnGUI;
			if(SVGDeleagate.IsRegistered(EditorApplication.update, Update))
                EditorApplication.update -= Update;
        }

        public static void Start() {
            active = true;
            UpdateDelegates();
        }

        public static void Stop() {
            active = false;
            UpdateDelegates();
        }

        /*
        public static void HideGizmos()
        {
            var Annotation = System.Type.GetType("UnityEditor.Annotation, UnityEditor");
            var ClassId = Annotation.GetField("classID");
            var ScriptClass = Annotation.GetField("scriptClass");
            
            System.Type AnnotationUtility = System.Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");
            var GetAnnotations = AnnotationUtility.GetMethod("GetAnnotations", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            //var SetGizmoEnabled = AnnotationUtility.GetMethod("SetGizmoEnabled", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            var SetIconEnabled = AnnotationUtility.GetMethod("SetIconEnabled", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            
            System.Array annotations = (System.Array)GetAnnotations.Invoke(null, null);
            foreach (var a in annotations)
            {
                int classId = (int)ClassId.GetValue(a);
                string scriptClass = (string)ScriptClass.GetValue(a);
                if(string.IsNullOrEmpty(scriptClass)) continue;

                if(scriptClass == typeof(SVGAsset).Name || 
                   scriptClass == typeof(SVGAtlas).Name || 
                   scriptClass == typeof(SVGImage).Name || 
                   scriptClass == typeof(SVGRenderer).Name || 
                   scriptClass == typeof(SVGCollider2D).Name
                   )
                {                    
                    if(SetIconEnabled != null)
                        SetIconEnabled.Invoke(null, new object[] { classId, scriptClass, 0 });
                }
            }
        }
        */

        static void PlaymodeStateChanged()
        {
			SVGAtlas svgAtlas = Object.FindObjectOfType<SVGAtlas>();
			if(svgAtlas != null)
			{
				svgAtlas.ClearAll();
			}
            /*
            SVGImage[] svgImages = Resources.FindObjectsOfTypeAll<SVGImage>();
            if(svgImages != null && svgImages.Length > 0)
            {
                for(int i = 0; i < svgImages.Length; i++)
                {
                    if(svgImages[i] == null)
                        continue;
                    if(svgImages[i].vectorGraphics == null)
                        continue;
                    svgImages[i].vectorGraphics._editor_ClearCache();
                }
            }
            */
            //visible = visible;
        }
        /*
        static void OnSceneGUI(SceneView sceneView)
        {
            if(DragAndDrop.objectReferences == null || DragAndDrop.objectReferences.Length == 0)
                return;

            SVGAsset asset = DragAndDrop.objectReferences[0] as SVGAsset;
            if(asset == null)
                return;

            Event evt = Event.current;
            Rect scenePosition = new Rect(0f, 0f, sceneView.position.width, sceneView.position.height);
            switch(evt.type)
            {
                case EventType.dragUpdated:
                case EventType.dragPerform:
                    Vector2 mousePosition = evt.mousePosition;
                    if(scenePosition.Contains(mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (evt.type == EventType.DragPerform) {
                            DragAndDrop.AcceptDrag ();

                        }
                    }
                break;
            }
        }
        */

        public static void OnSceneGUI(SceneView sceneView)
        {
            Event current = Event.current;
            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform && current.type != EventType.DragExited)
            {
                return;
            }
            SVGAsset[] svgAssetsFromDraggedPathsOrObjects = GetSVGAssetsFromDraggedObjects();
            if (svgAssetsFromDraggedPathsOrObjects.Length == 0)
            {
                return;
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            EventType type = current.type;
            if (type == EventType.DragPerform)
            {
                Vector3 point = HandleUtility.GUIPointToWorldRay(current.mousePosition).GetPoint(10f);
                point.z = 0f;
                GameObject objectToUndo = DropFramesToSceneToCreateGO(svgAssetsFromDraggedPathsOrObjects[0].name, svgAssetsFromDraggedPathsOrObjects, point);
                Undo.RegisterCreatedObjectUndo(objectToUndo, "Create SVG Renderer");
                current.Use();
            }
        }

        private static void HierarchyWindowItemOnGUI(int instancedID, Rect selectionRect)
        {
            Event current = Event.current;
            if (current.type != EventType.DragUpdated && current.type != EventType.DragPerform && current.type != EventType.DragExited)
            {
                return;
            }
            if(!selectionRect.Contains(Event.current.mousePosition))
                return;
            SVGAsset[] svgAssetsFromDraggedPathsOrObjects = GetSVGAssetsFromDraggedObjects();
            if (svgAssetsFromDraggedPathsOrObjects.Length == 0)
            {
                return;
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            EventType type = current.type;
            if (type == EventType.DragPerform)
            {
                GameObject parent = EditorUtility.InstanceIDToObject(instancedID) as GameObject;
                GameObject objectToUndo = DropFramesToSceneToCreateGO(svgAssetsFromDraggedPathsOrObjects[0].name, svgAssetsFromDraggedPathsOrObjects, parent.transform.position);
                Undo.RegisterCreatedObjectUndo(objectToUndo, "Create SVG Renderer");
                objectToUndo.transform.SetParent(parent.transform);
                objectToUndo.transform.localPosition = Vector3.zero;
                objectToUndo.transform.localRotation = Quaternion.identity;
                objectToUndo.transform.localScale = Vector3.one;
                current.Use();
            }
        }

        public static GameObject DropFramesToSceneToCreateGO(string name, SVGAsset[] frames, Vector3 position)
        {
            if (frames.Length > 0)
            {
                SVGAsset asset = frames[0];
                GameObject gameObject = DropSVGAssetToSceneToCreateGO(asset, position);
                if (frames.Length > 1)
                {
                    SVGFrameAnimator svgFrameAnimator = gameObject.AddComponent<SVGFrameAnimator>();
                    svgFrameAnimator.frames = frames.Clone() as SVGAsset[];
                }
                return gameObject;
            }
            return null;
        }

        private static Vector3 GetDefaultInstantiatePosition()
        {
            Vector3 result = Vector3.zero;
            if (SceneView.lastActiveSceneView)
            {
                if (SceneView.lastActiveSceneView.in2DMode)
                {
                    result = SceneView.lastActiveSceneView.camera.transform.position;
                    result.z = 0f;
                }
                else
                {
                    result = (Vector3)typeof(SceneView).GetField("cameraTargetPosition", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SceneView.lastActiveSceneView);
                }
            }
            return result;
        }
        /*
        public static bool HandleSingleDragIntoHierarchy(IHierarchyProperty property, SVGAsset svgAsset, bool perform)
        {
            GameObject gameObject = null;
            if (property != null && property.pptrValue != null)
            {
                UnityEngine.Object pptrValue = property.pptrValue;
                gameObject = (pptrValue as GameObject);
            }
            if (perform)
            {
                Vector3 defaultInstantiatePosition = GetDefaultInstantiatePosition();
                GameObject gameObject2 = DropSVGAssetToSceneToCreateGO(svgAsset, defaultInstantiatePosition);
                if (gameObject != null)
                {
                    gameObject2.transform.parent = gameObject.transform;
                    gameObject2.transform.localPosition = Vector3.zero;
                }
                Selection.activeGameObject = gameObject2;
            }
            return true;
        }
        */

        public static SVGAsset[] GetSVGAssetsFromDraggedObjects()
        {
            List<SVGAsset> list = new List<SVGAsset>();
            UnityEngine.Object[] objectReferences = DragAndDrop.objectReferences;
            for (int i = 0; i < objectReferences.Length; i++)
            {
                UnityEngine.Object @object = objectReferences[i];
                if (@object.GetType() == typeof(SVGAsset))
                {
                    list.Add(@object as SVGAsset);
                }
            }
            return list.ToArray();
        }

        public static GameObject DropSVGAssetToSceneToCreateGO(SVGAsset asset, Vector3 position)
        {
            GameObject go = new GameObject(asset.name);
            Undo.RegisterCreatedObjectUndo(go, "Create SVG Renderer");
            //Vector3 destination = Camera.current.ScreenToWorldPoint(new Vector3(mousePosition.x, scenePosition.height - mousePosition.y, 0f));
            //destination.z = 0f;
            go.transform.position = position;
            SVGRenderer renderer = go.AddComponent<SVGRenderer>();
            renderer.vectorGraphics = asset;
            if(asset.generateCollider) go.AddComponent<SVGCollider2D>();
            Selection.activeGameObject = go;
            return go;
        }

        static void OnPrefabInstanceUpdated(GameObject instance)
        {
            if(instance == null)
                return;

            SVGRenderer[] svgRenderers = instance.GetComponentsInChildren<SVGRenderer>();
            if(svgRenderers != null && svgRenderers.Length > 0)
            {
                for(int i = 0; i < svgRenderers.Length; i++)
                {
                    if(svgRenderers[i] == null)
                        continue;
                    svgRenderers[i].SetAllDirty();
                }
            }

            SVGImage[] svgImages = instance.GetComponentsInChildren<SVGImage>();
            if(svgImages != null && svgImages.Length > 0)
            {            
                for(int i = 0; i < svgImages.Length; i++)
                {
                    if(svgImages[i] == null)
                        continue;

                    typeof(SVGImage).GetMethod("Clear", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(svgImages[i], null);
                    typeof(SVGImage).GetMethod("UpdateMaterial", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(svgImages[i], null);
                    svgImages[i].SetAllDirty();
                }
            }
        }
    }
	
	internal class Analytics
	{
        internal const string LAST_IP_ADDRESS_KEY = "SVG_Importer_Last_IPAddress";
        internal static string ipAdress;
        const string ClickySiteID = "100849007";
        const string ClickySiteAdminKey = "5d1829818bd7c05ca2c5b5188697948c";
        
        static DelayedCall wwwCall;
        static DelayedCall wwwIPCall;
        
        internal static void TrackEvent(string title, string eventValue)
        {
            GetLocalIPAddressString();
            if(ipAdress != null)
            {
                string request = "http://in.getclicky.com/in.php?" +
                    "site_id=" + ClickySiteID +  //click site id , found in preferences
                        "&sitekey_admin=" + ClickySiteAdminKey + //clicky site admin key, found in preferences
                        "&ip_address=" + ipAdress + //ip address of the user - used for mapping action trails
                        "&type=custom" +
                        "&href=" + "/"+eventValue.Replace(" ", "_") + //string that contains whatever event you want to track/log
                        "&title="+UnityEngine.WWW.EscapeURL(title)+
                        "&type=click";
                
                wwwCall = new DelayedCall();
                EditorCoroutine.StartCoroutine(wwwCall.WWW(request), wwwCall);
            }
        }
        
        internal static string GetLocalIPAddressString()
        {
            if(ipAdress == null)
            {
                wwwIPCall = new DelayedCall();
                wwwIPCall.wwwCallback = delegate(WWW obj) 
                {
                    if(!string.IsNullOrEmpty(obj.text))
                    {
                        EditorPrefs.SetString(LAST_IP_ADDRESS_KEY, ipAdress);
                        ipAdress = obj.text;
                    }
                };
                EditorCoroutine.StartCoroutine(wwwIPCall.WWW("https://api.ipify.org"), wwwIPCall);
                
                if(EditorPrefs.HasKey(LAST_IP_ADDRESS_KEY))
                {
                    return EditorPrefs.GetString(LAST_IP_ADDRESS_KEY);
                }
            }       
            
            return ipAdress;
        }
	}
}

