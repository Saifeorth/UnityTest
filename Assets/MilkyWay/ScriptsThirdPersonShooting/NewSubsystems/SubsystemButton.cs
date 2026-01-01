using UnityEngine;
using UnityEngine.EventSystems;

public class SubsystemButton : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public SubsystemStateController controller;

    private bool isHovering = false;

    private void Update()
    {
        bool rightHeld = Input.GetMouseButton(1);

        // Always tell controller whether we're hovering and if RMB is held so it can show hover visuals.
        controller.SetHover(isHovering, rightHeld);

        // VISUAL STATE CHANGE (RMB + K) while hovering
        if (rightHeld && isHovering && Input.GetKeyDown(KeyCode.K))
        {
            controller.CycleVisualState();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
    }

    // FUNCTIONAL STATE CHANGE (RMB + LMB) while hovering
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left && Input.GetMouseButton(1))
        {
            // Immediately change functional state (overwrites any ongoing transition).
            controller.AdvanceFunctionalStateImmediate();
        }
    }
}
