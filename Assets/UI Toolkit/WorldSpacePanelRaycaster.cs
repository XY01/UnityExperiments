using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace UnityEngine.UIElements
{
    public class WorldSpacePanelRaycaster : PanelRaycaster
    {
        private PanelRaycaster _panelRaycaster;

        public float yOffset = 0;
        
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if(_panelRaycaster == null)
                TryGetComponent(out _panelRaycaster);
            
            print(eventData.position);
            Vector2 newPos = eventData.position;
            newPos.y -= yOffset;
            eventData.position = newPos;

            _panelRaycaster.Raycast(eventData, resultAppendList);
        }
    }
}
