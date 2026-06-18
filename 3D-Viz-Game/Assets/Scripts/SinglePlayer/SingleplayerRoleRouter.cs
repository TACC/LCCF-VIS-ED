using UnityEngine;

public class SingleplayerRoleRouter : MonoBehaviour
{
    public static SingleplayerRoleRouter Instance;

    private void Awake()
    {
        Instance = this;
    }

    public void AssignLocalRole(string assignedRole)
    {
        string[] allRoles = { "Cashier", "Kitchen", "Stocker", "Dishwasher" };
        foreach (string role in allRoles)
        {
            GameObject roleGO = GameObject.Find($"{role}Cam");
            if (roleGO != null)
                roleGO.SetActive(true);
        }
    }
}