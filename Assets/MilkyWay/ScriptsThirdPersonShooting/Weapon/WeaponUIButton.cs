using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponUIButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public WeaponSubsystem controller;
    public WeaponManager weaponManager;

    private bool isHovering = false;

    private void Update()
    {
        bool rightHeld = Input.GetMouseButton(1) || Input.GetKey(KeyCode.Space);

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
        if (eventData.button == PointerEventData.InputButton.Left && (Input.GetMouseButton(1) || Input.GetKey(KeyCode.Space))) 
        {
            controller.AdvanceFunctionalStateImmediate();
        }
    }

}
