using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
//using GameKit.Dependencies.Utilities.Types;
using System;


//SCRIPT IS IRRELEVANT (Almost)
public class DropHandler : MonoBehaviour, IDropHandler
{
    [SerializeField] private Canvas canvas;
     private RectTransform rectTransform;

     [Header("Order Ticket pairing")]
     [SerializeField] private OrderTicketUI orderTicketUI;

     [SerializeField] private slotIncrement slotInc;

     //[SerializeField] private Transform[] slots;

     private int nextSlotIndex = 0;
    private void Awake()
    {
        // Optional: Initialize any necessary components or variables
        rectTransform = GetComponent<RectTransform>();
    }

    public void ResetSlots()
    {
        nextSlotIndex = 0;
        slotInc.slotReset();
    }

    public void OnDrop(PointerEventData eventData)
    {
       
       // NEW CODE (HELP FROM CLAUDE)

        if (eventData.pointerDrag == null) return;

        RectTransform droppedRect = eventData.pointerDrag.GetComponent<RectTransform>();

        droppedRect.SetParent(rectTransform.parent, false);
        droppedRect.anchoredPosition = rectTransform.anchoredPosition;

        DragDrop dragDrop = eventData.pointerDrag.GetComponent<DragDrop>();

    }

    
}
