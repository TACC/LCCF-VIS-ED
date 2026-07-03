using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using GameKit.Dependencies.Utilities.Types;
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

        //NEW CODE 

        //Set it to the center of the specified slot
        // if (eventData.pointerDrag!= null)
        // {
        //     //Optional: Handle the dropped object, e.g., move it to this position
        //     RectTransform droppedRect = eventData.pointerDrag.GetComponent<RectTransform>();
        //     droppedRect.SetParent(rectTransform.parent, false); // Set the parent to this container
        //     droppedRect.anchoredPosition = Vector2.zero; // Optional: Reset position to the center of the container
            
        //     DragDrop dragDrop = eventData.pointerDrag.GetComponent<DragDrop>();
        //     if (dragDrop != null)
        //     {
        //         dragDrop.NotifyDroppedOnTarget();
        //     }
            
        //     //Set position to above based on available slots using nextSlotIndex
        //     int currentSlot = nextSlotIndex;
        //     nextSlotIndex++;


       

        // if (orderTicketUI != null)
        // {
        //     orderTicketUI.RevealRow(currentSlot); //Change to match rows in order of placement
        //     //Reveal the subsequent dropdown
        //     DragDrop itemInfo = eventData.pointerDrag.GetComponent<DragDrop>();
        //     if (itemInfo != null)
        //     {
        //         //orderTicketUI.RevealRow(nextSlotIndex)
        //         orderTicketUI.RegisterDrop(currentSlot, itemInfo.itemValue);
        //         orderTicketUI.RegisterDropName(currentSlot, itemInfo.ingredientName);
        //     }

        //     // if (nextSlotIndex < slots.Length)
        //     //     {
        //     //         orderTicketUI.RevealRow(nextSlotIndex);
        //     //     }
        // }

        // }

        // //NEW CODE-SIMPLE 

        // if (eventData.pointerDrag == null) return;

        // RectTransform droppedRect = eventData.pointerDrag.GetComponent<RectTransform>();

        // // Parent directly to ticketArea's parent, snap to ticketArea's position
        // droppedRect.SetParent(rectTransform.parent, false);
        // droppedRect.anchoredPosition = rectTransform.anchoredPosition;

        // DragDrop dragDrop = eventData.pointerDrag.GetComponent<DragDrop>();
       
       // NEW CODE (HELP FROM CLAUDE)

        if (eventData.pointerDrag == null) return;

        RectTransform droppedRect = eventData.pointerDrag.GetComponent<RectTransform>();

        droppedRect.SetParent(rectTransform.parent, false);
        droppedRect.anchoredPosition = rectTransform.anchoredPosition;

        DragDrop dragDrop = eventData.pointerDrag.GetComponent<DragDrop>();

        //Uncomment
        // if (dragDrop != null)
        // {
        //     dragDrop.NotifyDroppedOnTarget();

        //     if (orderTicketUI != null)
        //     {
        //         int slotForThisDrop = dragDrop.GetPendingSlotIndex();
        //         orderTicketUI.RegisterDrop(slotForThisDrop, dragDrop.itemValue);
        //         orderTicketUI.RegisterDropName(slotForThisDrop, dragDrop.ingredientName);
        //     }
        // }
    }

    
}
