// Copyright (C) 2015 Jaroslav Stehlik
// Released under the license found in the LICENSE file.

using System;
using UnityEditor;
using UnityEngine;


/// <summary>
/// Define a popup window that return a result.
/// Base class for IModal call implementation.
/// </summary>
namespace SVGImporter
{
    /// <summary>
    /// This EditorWindow can recieve and send Modal inputs.
    /// </summary>
    public interface IModal
    {
        /// <summary>
        /// Called when the Modal shortcut is pressed.
        /// The implementation should call Create if the condition are right.
        /// </summary>
        void ModalRequest(bool shift);
        
        /// <summary>
        /// Called when the associated modal is closed.
        /// </summary>
        void ModalClosed(ModalWindow window);
    }

    public enum WindowResult
    {
        None,
        Ok,
        Cancel,
        Invalid,
        LostFocus
    }

    public abstract class ModalWindow : EditorWindow
    {
        public const float TITLEBAR = 18;
        
        protected IModal owner;
        
        protected WindowResult result = WindowResult.None;
        
        public WindowResult Result
        {
            get { return result; }
        }
        
        protected virtual void OnLostFocus()
        {
            result = WindowResult.LostFocus;
            
            if (owner != null)
                owner.ModalClosed(this);
        }
        
        protected virtual void Cancel()
        {
            result = WindowResult.Cancel;
            
            if (owner != null)
                owner.ModalClosed(this);
            
            Close();
        }
        
        protected virtual void Ok()
        {
            result = WindowResult.Ok;
            
            if (owner != null)
                owner.ModalClosed(this);
            
            Close();
        }

        protected abstract void Draw(Rect region);
    }
}
