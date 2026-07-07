using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.VisualScripting;

public class DragDrop : MonoBehaviour, IPointerDownHandler, IInitializePotentialDragHandler, IBeginDragHandler, 
IEndDragHandler, IDragHandler/*, IDropHandler*/
{

    [SerializeField] private Canvas canvas;

    [SerializeField] private Transform dragLayer;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 lastPosition;

    [SerializeField] private Transform originalParent;
    [SerializeField] private Transform[] slots;

    [SerializeField] private GameObject[] slotDrops;

    public string ingredientName;
    public string itemValue;

    [SerializeField] private RectTransform targetArea;

    [SerializeField] private slotIncrement slotInc;
    public bool wasDroppedOnTarget = false;
    public int thisSlotNum = -1;

    [SerializeField] private OrderTicketUI orderTicketUI;

    


    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        // dragCanvas = GetComponent<Canvas>();
        //dragCanvas.overrideSorting = true;
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Called when the drag operation starts
        print("Begin Drag");

        //If the object is on ticket and dragging it off
        if (wasDroppedOnTarget)
        {
            //set row visible to false and editable to false
            orderTicketUI.SetRowVisible(orderTicketUI.ingredientDropdowns[thisSlotNum], false);
            orderTicketUI.SetRowEditable(orderTicketUI.ingredientDropdowns[thisSlotNum], false);
            
            //Move everything up one (seperate method)
            //Claude entry *
            orderTicketUI.RegisterDrop(thisSlotNum, null);
            orderTicketUI.RegisterDropName(thisSlotNum, null);
            // *

            moveOnUp();
            //decrement slotInc
            slotInc.slotDec();

            thisSlotNum = -1;
        }

        wasDroppedOnTarget = false;
        
        transform.SetParent(dragLayer, true);
        transform.SetAsLastSibling();

        
        //gameObject.AddComponent<GraphicRaycaster>(); // Add a GraphicRaycaster to the dragged object

        canvasGroup.blocksRaycasts = false; // Optional: Disable raycast blocking to allow drop targets to receive events
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // Called when the potential drag is initialized
        print("Initialize Potential Drag");
        eventData.useDragThreshold = false; // Optional: Disable drag threshold for immediate dragging
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Called while the object is being dragged
        //print("Dragging");
        //rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPos))
        {
            rectTransform.anchoredPosition = localPointerPos + lastPosition;
        }
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
       //New (With parts by ChatGPT)
        print("End Drag");

        canvasGroup.blocksRaycasts = true;

        wasDroppedOnTarget = RectTransformUtility.RectangleContainsScreenPoint(
            targetArea,
            eventData.position,
            eventData.pressEventCamera
        );

        if (wasDroppedOnTarget)
        {
            // Save the slot BEFORE incrementing
            int currentSlot = slotInc.slotNum;

            thisSlotNum = currentSlot;

            // Snap into slot and then set dropdown active
            transform.SetParent(slots[currentSlot], true);
            rectTransform.anchoredPosition = Vector2.zero;

            slotDrops[currentSlot].SetActive(true);
            orderTicketUI.SetRowVisible(orderTicketUI.ingredientDropdowns[currentSlot], true);
            orderTicketUI.SetRowEditable(orderTicketUI.ingredientDropdowns[currentSlot], true);

            // Register this ingredient with the ticket
            if (orderTicketUI != null)
            {
                orderTicketUI.RegisterDrop(currentSlot, itemValue);
                orderTicketUI.RegisterDropName(currentSlot, ingredientName);

                Debug.Log($"Registered {ingredientName} ({itemValue}) in slot {currentSlot}");
            }
            else
            {
                Debug.LogWarning("OrderTicketUI reference is missing!");
            }

            // Move to the next slot
            slotInc.slotInc();
        }
        else
        {
            // Optional: return to original position if dropped outside
            transform.SetParent(originalParent, true);
            rectTransform.anchoredPosition = Vector2.zero;
            Debug.Log("Dropped outside ticket.");
        }
        
    }

    
    public void OnPointerDown(PointerEventData eventData)
    {
        // Start dragging the object
        print("Pointer Down");
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPos
        );

        lastPosition = rectTransform.anchoredPosition - localPointerPos;
    }

    //Will move each item up one slot when an item is removed from the ticket (Unless it is the last item)
    //Later edited with claude
    private void moveOnUp()
    {

        int lastFilledIndex = thisSlotNum;
        //For loop that goes from the current slot to the last slot
        for (int i = thisSlotNum; i < slotInc.slotNum - 1; i++)
        {
            if (slots[i + 1].childCount == 0) break;
            
            //Claude
            //Grab the actual ingredient icon
            Transform itemToMove = slots[i + 1].GetChild(0);
            DragDrop movedDragDrop = itemToMove.GetComponent<DragDrop>();

            //Move it
            itemToMove.SetParent(slots[i], true);
            RectTransform movedRect = itemToMove.GetComponent<RectTransform>();
            if (movedRect) movedRect.anchoredPosition = Vector2.zero;

            if (movedDragDrop) movedDragDrop.thisSlotNum = i;

            //Carry the player's chosen dropdown value up with the ingredient
            int carriedIndex = orderTicketUI.GetSelectedIndex(i + 1);
            orderTicketUI.SetDropdownIndex(i, carriedIndex);
            orderTicketUI.SetDropdownIndex(i + 1, 0);
            
            //Mine
            //Move the next slot's item to the current slot
            slotDrops[i + 1].SetActive(false);
            slotDrops[i].SetActive(true);

            // Claude *
            bool wasLocked = orderTicketUI.isSlotLocked(i + 1);
            orderTicketUI.SetSlotLocked(i + 1, false);
            orderTicketUI.SetSlotLocked(i, wasLocked);

            if (wasLocked)
            {
                orderTicketUI.SetRowVisible(orderTicketUI.ingredientDropdowns[i], false);
                orderTicketUI.SetRowEditable(orderTicketUI.ingredientDropdowns[i], false);
            }
            else
            {
                orderTicketUI.SetRowVisible(orderTicketUI.ingredientDropdowns[i], true);
                orderTicketUI.SetRowEditable(orderTicketUI.ingredientDropdowns[i], true);
            }
            // *

            //Register the moved item in the new slot
            orderTicketUI.RegisterDrop(i, orderTicketUI.getDropValue(i + 1));
            orderTicketUI.RegisterDropName(i, orderTicketUI.getDropName(i + 1));

            //Clear the old slot
            orderTicketUI.RegisterDrop(i + 1, null);
            orderTicketUI.RegisterDropName(i + 1, null);
            

            lastFilledIndex = i + 1;
        }

        //Claude
        orderTicketUI.SetRowVisible(orderTicketUI.ingredientDropdowns[lastFilledIndex], false);
        slotDrops[lastFilledIndex].SetActive(false);

    }

    //Claude
    public void ResetToPool()
    {
        thisSlotNum = -1;
        wasDroppedOnTarget = false;
        transform.SetParent(originalParent, true);
        rectTransform.anchoredPosition = Vector2.zero;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
    }

}
