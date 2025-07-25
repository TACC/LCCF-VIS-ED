using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NPCManager : MonoBehaviour
{
    [Header("NPC References")]
    public GameObject npc1;
    public GameObject npc2;

    [Header("Shirt References")]
    public Image npc1Shirt;
    public Image npc2Shirt;

    [Header("Movement Settings")]
    public Transform centerPosition;
    public Transform leftOffscreen;
    public Transform rightOffscreen;
    public float moveSpeed = 5f;

    [Header("UI References")]
    public GameObject dialogueBox; // This is the container (text box panel)
    public NPCOrderManager orderManager; // Reference to NPCOrderManager

    private GameObject currentNPC;
    private GameObject nextNPC;
    private Image currentShirt;
    private Image nextShirt;

    private bool isNPC1Active = true;

    void Start()
    {
        // Initialize NPC1 at center, NPC2 offscreen
        npc1.transform.position = centerPosition.position;
        npc2.transform.position = rightOffscreen.position;
        currentNPC = npc1;
        nextNPC = npc2;
        currentShirt = npc1Shirt;
        nextShirt = npc2Shirt;

        orderManager.GenerateOrder();
    }

    public void SwapNPCs()
    {
        StartCoroutine(HandleNPCSwap());
    }

    private IEnumerator HandleNPCSwap()
    {
        dialogueBox.SetActive(false);

        // Slide current NPC offscreen
        Vector3 targetOffscreen = isNPC1Active ? leftOffscreen.position : rightOffscreen.position;
        while (Vector3.Distance(currentNPC.transform.position, targetOffscreen) > 0.1f)
        {
            currentNPC.transform.position = Vector3.MoveTowards(currentNPC.transform.position, targetOffscreen, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Change shirt color
        currentShirt.color = GetRandomColor();

        // Slide next NPC onscreen
        Vector3 targetOnscreen = centerPosition.position;
        while (Vector3.Distance(nextNPC.transform.position, targetOnscreen) > 0.1f)
        {
            nextNPC.transform.position = Vector3.MoveTowards(nextNPC.transform.position, targetOnscreen, moveSpeed * Time.deltaTime);
            yield return null;
        }

        // Set new active NPC
        isNPC1Active = !isNPC1Active;
        currentNPC = isNPC1Active ? npc1 : npc2;
        nextNPC = isNPC1Active ? npc2 : npc1;
        currentShirt = isNPC1Active ? npc1Shirt : npc2Shirt;
        nextShirt = isNPC1Active ? npc2Shirt : npc1Shirt;

        dialogueBox.SetActive(true);
        orderManager.GenerateOrder();
    }

    private Color GetRandomColor()
    {
        return new Color(Random.value, Random.value, Random.value);
    }
}
