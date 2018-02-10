// Copyright (C) 2015 Jaroslav Stehlik - All Rights Reserved
// This code can only be used under the standard Unity Asset Store End User License Agreement
// A Copy of the EULA APPENDIX 1 is available at http://unity3d.com/company/legal/as_terms

using UnityEditor;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Mime;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.IO;


namespace SVGImporter
{
    internal class SVGReportBugWindow : EditorWindow
    {
        string mailPattern = @"^[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+$";

        const string SVGReportBugWindow_LastEmailKey = "SVGReportBugWindow_LastEmailKey";
        public string lastEmail
        {
            get {
                string output = "@";
                if(EditorPrefs.HasKey(SVGReportBugWindow_LastEmailKey))
                {
                    output = EditorPrefs.GetString(SVGReportBugWindow_LastEmailKey);
                    if(string.IsNullOrEmpty(output))
                        return "@";
                }
                return output;
            }
            set {
                EditorPrefs.SetString(SVGReportBugWindow_LastEmailKey, value);
            }
        }


        public enum PROBLEM_TYPE {
            PleaseSpecify,
            FileImport,
            ProblemInEditor,
            ProblemInPlayer,
            FeatureRequest,
            Documentation,
            CrashBug,
        }

        public enum PROBLEM_OCCURRENCE {
            PleaseSpecify,
            Always,
            SometimesButNotAlways,
            ThisIsTheFirstTime
        }

        static SVGReportBugWindow windowWithRect;

        [MenuItem("Window/SVG Importer/Report a Bug...")]
        public static void ShowReportBugWindow()
        {
            windowWithRect = EditorWindow.GetWindowWithRect<SVGReportBugWindow>(new Rect(100f, 100f, 570f, 340f), true, "SVG Importer | Bug Reporter");
            windowWithRect.position = new Rect(100f, 100f, 570f, 360f);
        }

        protected virtual void OnLostFocus()
        {
            EditorWindow.FocusWindowIfItsOpen<SVGReportBugWindow>();
        }

        public static string emailField="@";
        public static string titleField;
        public static string descriptionField = "1. What happened\n\n2. How we can reproduce it";

        public const string defaultDescription = "1. What happened\n\n2. How we can reproduce it";

        public static PROBLEM_TYPE problemType;
        public static PROBLEM_OCCURRENCE problemOccurrence;
        public static List<Attachment> attachments = new List<Attachment>();

        public static void ResetToDefault()
        {
            descriptionField = defaultDescription;
            titleField = null;
            problemType = PROBLEM_TYPE.PleaseSpecify;
            problemOccurrence = PROBLEM_OCCURRENCE.PleaseSpecify;
            attachments.Clear();
        }

        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static void AddSVGAttachment(string filename, string data)
        {
            ContentType ct = new ContentType();
            ct.MediaType = MediaTypeNames.Text.Plain;
            ct.Name = filename + ".svg";
            attachments.Add(new Attachment(GenerateStreamFromString(data), ct));
        }

        void OnEnable()
        {
			SVGImporterLaunchEditor.OpenReportBugWindow();
            emailField = lastEmail;
        }
        
        void OnDisable()
        {
            ResetToDefault();
        }

        void OnGUI()
        {            
            GUILayout.Space(20);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("What is the problem related to", GUILayout.Width(200));
            problemType = (PROBLEM_TYPE)EditorGUILayout.EnumPopup(problemType);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("How often does it happen", GUILayout.Width(200));
            problemOccurrence = (PROBLEM_OCCURRENCE)EditorGUILayout.EnumPopup(problemOccurrence);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("your email adress", GUILayout.Width(200));
            emailField = EditorGUILayout.TextArea(emailField, GUILayout.Width(200));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("Title");
            titleField = EditorGUILayout.TextArea(titleField);
            EditorGUILayout.LabelField("Describe the problem");
            descriptionField = EditorGUILayout.TextArea(descriptionField, GUILayout.Height(200));

            EditorGUILayout.BeginHorizontal();
            if(attachments != null && attachments.Count > 0)
            {
                if(attachments.Count == 1)
                {
                    GUILayout.Label("Added "+attachments.Count+" attachment.");
                } else {
                    GUILayout.Label("Added "+attachments.Count+" attachments.");
                }
            }
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Cancel"))
            {
                this.Close();
            }
            if(GUILayout.Button("Send"))
            {
                SendEmail();                
            }
            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    this.Close();
                }
            }
        }

        void SendEmail()
        {
            Regex regex = new Regex(mailPattern);
            if(!regex.IsMatch(emailField))
            {
                EditorUtility.DisplayDialog("SVG Importer | Bug Report", "Email is not valid!", "Ok");
                return;
            }

            if(problemType == PROBLEM_TYPE.PleaseSpecify)
            {
                EditorUtility.DisplayDialog("SVG Importer | Bug Report", "Please specify the problem type", "Ok");
                return;
            }

            if(problemOccurrence == PROBLEM_OCCURRENCE.PleaseSpecify)
            {
                EditorUtility.DisplayDialog("SVG Importer | Bug Report", "Please specify the problem occurrence", "Ok");
                return;
            }

            lastEmail = emailField;

            string pluginVersion = "SVG Importer version: "+SVGImporterSettings.version+"\n\n";

            string systemSpecs = "System Specs:\n" +
                    "Unity Version: "+Application.unityVersion+"\n" +
                    "Unity Build Settings: "+EditorUserBuildSettings.activeBuildTarget.ToString()+"\n" +
                    //"Version: "+Application.version+"\n\n" +
                    "operating System: "+SystemInfo.operatingSystem+"\n" +
                    "device Model: "+SystemInfo.deviceModel+"\n" +
                    "device Name: "+SystemInfo.deviceName+"\n" +
                    "device Type: "+SystemInfo.deviceType+"\n\n" +
                    //"deviceUniqueIdentifier: "+SystemInfo.deviceUniqueIdentifier+"\n" +
                    "processor Type: "+SystemInfo.processorType+"\n" +
                    "processor Count: "+SystemInfo.processorCount+"\n" +
                    "system Memory Size: "+SystemInfo.systemMemorySize+"\n\n" +
                    "graphics Device Vendor: "+SystemInfo.graphicsDeviceVendor+"\n" +
                    "graphics Device VendorID: "+SystemInfo.graphicsDeviceVendorID+"\n" +
                    "graphics Device Name: "+SystemInfo.graphicsDeviceName+"\n" +
                    "graphics Device ID: "+SystemInfo.graphicsDeviceID+"\n" +
                    "graphics Device Version: "+SystemInfo.graphicsDeviceVersion+"\n" +
                    "graphics Memory Size: "+SystemInfo.graphicsMemorySize+"\n" +
                    "graphics Shader Level: "+SystemInfo.graphicsShaderLevel+"\n" +
                    "max Texture Size: "+SystemInfo.maxTextureSize+"\n" +
                    "npot Support: "+SystemInfo.npotSupport+"\n" +
                    "supports Stencil: "+SystemInfo.supportsStencil+"\n" +
                    "supported Render Target Count: "+SystemInfo.supportedRenderTargetCount+"\n" +
                    "supports 3D Textures: "+SystemInfo.supports3DTextures+"\n" +
                    "supports Compute Shaders: "+SystemInfo.supportsComputeShaders+"\n" +
                    "supports Image Effects: "+SystemInfo.supportsImageEffects+"\n" +
                    "supports Instancing: "+SystemInfo.supportsInstancing+"\n" +
                    "supports Render To Cubemap: "+SystemInfo.supportsRenderToCubemap+"\n" +
                    "supports Shadows: "+SystemInfo.supportsShadows+"\n" +
                    "supports Sparse Textures: "+SystemInfo.supportsSparseTextures+"\n";

                    //"supportsLocationService: "+SystemInfo.supportsLocationService+"\n" +
                    //"supportsAccelerometer: "+SystemInfo.supportsAccelerometer+"\n" +
                    //"supportsGyroscope: "+SystemInfo.supportsGyroscope+"\n" +


            MailMessage mail = new MailMessage();
            
            mail.From = new MailAddress(emailField);
            mail.To.Add("support@svgimporter.com");
            mail.Subject = "Bug Report | Sender: "+emailField+" | "+titleField;
			mail.Body = "Problem Type: "+problemType.ToString()+"\n\n"+"Problem Occurrence: "+problemOccurrence.ToString()+"\n\n"+descriptionField+"\n\n\n\n\n"+pluginVersion+systemSpecs;
            if(attachments.Count > 0)
            {
                for(int i = 0; i < attachments.Count; i++)
                {
                    if(attachments[i] == null)
                        continue;

                    mail.Attachments.Add(attachments[i]);
                }
            }

            SmtpClient smtpServer = new SmtpClient("smtp.gmail.com");
            smtpServer.Port = 587;
            smtpServer.Credentials = new System.Net.NetworkCredential("noreply.junkmail.spam", "pAssWoRd123$") as ICredentialsByHost;
            smtpServer.EnableSsl = true;
            ServicePointManager.ServerCertificateValidationCallback = 
                delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) 
            { return true; };
            try {
                smtpServer.Send(mail);
                this.Close();
                ResetToDefault();
                EditorUtility.DisplayDialog("SVG Importer | Bug Report", "Thank you for your help!", "Ok");
            } catch (SmtpException exception)
            {
                ResetToDefault();
                EditorUtility.DisplayDialog("SVG Importer | Bug Report", "Sending Bug Report failed:\n"+exception.Message, "Ok");
            }
        }
    }
}