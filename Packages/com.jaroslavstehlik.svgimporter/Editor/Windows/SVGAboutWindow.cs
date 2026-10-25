// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System;
using UnityEditor;
using UnityEngine;

namespace SVGImporter
{
    internal class AboutWindow : EditorWindow
    {
        //
        // Static Fields
        //
        private static GUIContent s_SGILogo;
        
        private static GUIContent s_Header;
        
		private const string kSpecialThanksNames = "Thanks to my girlfriend Kate to her endless patience\nand support and also thanks to my beloved parents.";
        private const string kSpecialThanksNamesTesters = "Thanks to Anders Gallon for his exquisite bugreports.\nBig thanks to JaimeAL for his great forum support!";
        
        private static GUIContent s_ClipperLogo;
        
        //
        // Fields
        //
        private double m_LastScrollUpdate;
        
        private bool m_ShowDetailedVersion;
        
        
//        private float m_TotalCreditsHeight = float.PositiveInfinity;
        
//        private readonly string kCreditsNames = "Jaroslav Stehlik";
        
//        private float m_TextYPos = 120f;
        
//        private float m_TextInitialYPos = 120f;

        //
        // Static Methods
        //
        private static void LoadLogos()
        {
            if (AboutWindow.s_ClipperLogo != null)
            {
                return;
            }
            AboutWindow.s_ClipperLogo = new GUIContent(LoadImage("SVGImporterGPCLogo"));
            AboutWindow.s_SGILogo = new GUIContent(LoadImage("SVGImporterSGILogo"));
            AboutWindow.s_Header = new GUIContent(LoadImage("SVGImporterLogo"));
        }

        protected static Texture2D LoadImage(string name)
        {
            Texture2D output = null;
            string[] guid = AssetDatabase.FindAssets(name+" t:Texture2D");
            if(guid != null && guid.Length > 0 && !string.IsNullOrEmpty(guid[0]))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid[0]);
				output = (Texture2D)AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D));
            }
            return output;
        }

        [MenuItem("Window/SVG Importer/About")]
        private static void ShowAboutWindow()
        {
            AboutWindow windowWithRect = EditorWindow.GetWindowWithRect<AboutWindow>(new Rect(100f, 100f, 570f, 340f), true, "About SVG Importer");
            windowWithRect.position = new Rect(100f, 100f, 570f, 340f);
        }

		[MenuItem("Window/SVG Importer/Help/Documentation")]
		private static void ShowDocumentation()
		{
			// The PDF lives in the package's hidden Documentation~ folder, so it is not an asset.
			var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AboutWindow).Assembly);
			if(package != null)
			{
				string pdf = System.IO.Path.Combine(package.resolvedPath, "Documentation~", "SVG Importer - documentation.pdf");
				if(System.IO.File.Exists(pdf))
				{
					Application.OpenURL("file://" + pdf);
					return;
				}
			}
			Application.OpenURL("https://svgimporter.com/assets/docs/svg-importer-documentation.pdf");
		}

		[MenuItem("Window/SVG Importer/Help/Website")]
		private static void ShowWebsite()
		{
			Application.OpenURL("https://svgimporter.com/");
		}

		[MenuItem("Window/SVG Importer/Help/Youtube")]
		private static void ShowYoutube()
		{
			Application.OpenURL("https://www.youtube.com/@Svgimporter");
		}

		[MenuItem("Window/SVG Importer/Help/Facebook")]
		private static void ShowFacebook()
		{
			Application.OpenURL("https://www.facebook.com/svgimporter");
		}

		[MenuItem("Window/SVG Importer/Help/Twitter")]
		private static void ShowTwitter()
		{
			Application.OpenURL("https://twitter.com/svgimporter");
		}
        
        //
        // Methods
        //
		
		public void OnEnable()
		{
			AboutWindow.LoadLogos();
			//EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Combine(EditorApplication.update, new EditorApplication.CallbackFunction(this.UpdateScroll));
			//this.m_LastScrollUpdate = EditorApplication.timeSinceStartup;
		}

        public void OnDisable()
        {
            //EditorApplication.update = (EditorApplication.CallbackFunction)Delegate.Remove(EditorApplication.update, new EditorApplication.CallbackFunction(this.UpdateScroll));
        }
        
		static void OnLastRectClick(Action action)
		{
			Rect lastRect = GUILayoutUtility.GetLastRect();
			if(Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
			{
				if(action == null)
					return;

				action();
			}
		}

        public void OnGUI()
        {            
			GUILayout.Space(10f);

			GUILayout.BeginHorizontal(new GUILayoutOption[0]);
			GUILayout.FlexibleSpace();
			GUILayout.Label(string.Format("Version {0}", SVGImporterSettings.version), new GUILayoutOption[0]);            
			GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
            GUILayout.Label(AboutWindow.s_Header, GUIStyle.none, new GUILayoutOption[0]);

			OnLastRectClick(delegate() { Application.OpenURL("http://www.svgimporter.com"); });

           
            GUILayout.Space(4f);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
            GUILayout.TextArea(kSpecialThanksNames + "\n"+kSpecialThanksNamesTesters, GUI.skin.label);
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label(AboutWindow.s_ClipperLogo, new GUILayoutOption[0]);
			OnLastRectClick(delegate() { Application.OpenURL("http://www.angusj.com/delphi/clipper.php"); });
            GUILayout.Label("Clipping powered by Clipper.\n\n(c) 2010-2014 Angus Johnson", "MiniLabel", new GUILayoutOption[]
            {
                GUILayout.Width(200f)
            });
			OnLastRectClick(delegate() { Application.OpenURL("http://www.angusj.com/delphi/clipper.php"); });		

            GUILayout.Label(AboutWindow.s_SGILogo, new GUILayoutOption[0]);
			OnLastRectClick(delegate() { Application.OpenURL("https://github.com/speps/LibTessDotNet"); });
            GUILayout.Label("Tesselation powered by LibTessDotNet.\n\n(c) 2011 Silicon Graphics, Inc.", "MiniLabel", new GUILayoutOption[]
            {
                GUILayout.Width(200f)
            });
			OnLastRectClick(delegate() { Application.OpenURL("https://github.com/speps/LibTessDotNet"); });

            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Space(5f);
            GUILayout.BeginVertical(new GUILayoutOption[0]);
            GUILayout.FlexibleSpace();
            GUILayout.Label("\n" + "Copyright (c) 2015 Jaroslav Stehlik", "MiniLabel", new GUILayoutOption[0]);
            GUILayout.EndVertical();
            GUILayout.Space(10f);
            GUILayout.FlexibleSpace();
            GUILayout.Space(5f);
            GUILayout.EndHorizontal();
            GUILayout.Space(5f);
        }
    }
}