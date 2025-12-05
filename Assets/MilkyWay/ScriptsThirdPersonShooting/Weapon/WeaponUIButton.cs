using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public WeaponSubsystem controller;
    public WeaponManager weaponManager;

    private bool isHovering = false;

    private void Update()
    {
        bool rightHeld = Input.GetMouseButton(1);

        // --- Apply Hover Visuals Every Frame ---
        controller.SetHover(isHovering, rightHeld);

        // --- VISUAL STATE CHANGE (RMB + K) ---
        if (rightHeld && Input.GetKeyDown(KeyCode.K) && isHovering)
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

    // --- FUNCTIONAL STATE CHANGE (RMB + LMB) ---
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left &&
            Input.GetMouseButton(1))
        {
            // If NOT selected, this click ONLY selects — no transition
            if (!controller.isSelected)
            {
                weaponManager.Select(controller);
                return;
            }

            // If already selected, THIS click advances functional state
            controller.AdvanceFunctionalState();
        }
    }

}
