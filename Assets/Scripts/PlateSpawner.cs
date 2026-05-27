using UnityEngine;
using UnityEngine.UI;

public class PlateSpawner : MonoBehaviour
{

    public GameObject platePrefab;
    public RectTransform plateContainer;
    public Button spawnButton;

    public int maxPlates = 3;

    void Start()
    {
        spawnButton.onClick.AddListener(SpawnPlate);
    }

    public void SpawnPlate()
    {

        if (plateContainer.childCount >= maxPlates)
        {
            Debug.Log("Only 3 plates allowed");
            return;
        }

        var newPlate = Instantiate(platePrefab, plateContainer, false);
    }
}
