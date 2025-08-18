using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class PointerRayDebugger : MonoBehaviour
{
    public Camera cam;
    void Update()
    {
        if (!cam) cam = Camera.main;
        var ped = new PointerEventData(EventSystem.current){ position = Input.mousePosition };
        var uiHits = new List<RaycastResult>(); EventSystem.current?.RaycastAll(ped, uiHits);
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        bool hit = Physics.Raycast(ray, out var h, 1000f, ~0, QueryTriggerInteraction.Collide);
        Debug.Log($"UI: {(uiHits.Count>0?uiHits[0].gameObject.name:"none")}   PHYS: {(hit?h.collider.name:"none")}   Cam:{cam?.name}");
    }
}
