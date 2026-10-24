using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

[Serializable]
public class AnimationAction : System.Object
{
    [Serializable]
    public class ActionEvent : UnityEvent { }

    public string name;
    public ActionEvent actionEvent = new ActionEvent();
    
    public void InvokeAnimationAction()
    {
        actionEvent.Invoke();
    }
}

public class AnimationActions : MonoBehaviour {

    public AnimationAction[] events;

    public void InvokeAnimationAction(string name)
    {
        if (events == null || events.Length == 0)
            return;

        for(int i = 0; i < events.Length; i++)
        {
            if(events[i] == null || events[i].actionEvent == null || events[i].name != name)
                continue;

            events[i].InvokeAnimationAction();
        }
    }
}
