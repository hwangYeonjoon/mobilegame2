using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace ScrapGuard
{
    // A held press opens the workshop once and never also activates on release.
    public sealed class RobotInputButton : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler
    {
        public Action Tap,Hold;bool pressed,held;float since;int pointer;
        public void Click(){if(!held)Tap?.Invoke();held=false;}
        public void OnPointerDown(PointerEventData e){if(!GetComponent<Button>().IsInteractable())return;pointer=e.pointerId;since=Time.unscaledTime;pressed=true;held=false;}
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)pressed=false;}
        public void OnPointerExit(PointerEventData e){if(e.pointerId==pointer)pressed=false;}
        void Update(){if(pressed&&!held&&Time.unscaledTime-since>=.45f){held=true;pressed=false;Hold?.Invoke();}}
        void OnDisable(){pressed=false;held=false;}
    }
}
