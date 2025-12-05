using UnityEngine;

public class SubsystemSelectionManager : MonoBehaviour
{
    public static SubsystemSelectionManager Instance;

    private SubsystemStateController currentSelected;

    private void Awake()
    {
        Instance = this;
    }

    public void Select(SubsystemStateController controller)
    {
        if (currentSelected != null && currentSelected != controller)
            currentSelected.Deselect();

        currentSelected = controller;
        controller.ApplySelectedState();
    }
}
