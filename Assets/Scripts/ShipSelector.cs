using UnityEngine;
using System;
using System.Collections.Generic;

public class ShipSelector : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask shipLayer;
    private Camera cam;

    [Header("Outline Colors")]
    public Color playerHoverColor = Color.white;
    public Color enemyHoverColor = Color.red;
    public Color selectedColor = Color.green;

    private Outline hoveredOutline;
    private Outline selectedOutline;

    public ShipController SelectedShip { get; private set; }
    public event Action<ShipController> OnSelectionChanged;

    private AudioSource audioSource;
    public AudioClip selectionSound;
    public AudioClip wrongSelectionSound;

    void Awake()
    {
        cam = Camera.main;
        audioSource = GetComponent<AudioSource>();

        // Disable all outlines at start to avoid visible white outlines
        DisableAllOutlines();
    }

    void Update()
    {
        HandleHover();
        HandleSelection();
    }

    // ---------------------- Core Logic ----------------------

    private void HandleSelection()
    {
        // Left click = select player ship
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, shipLayer))
            {
                var ship = hit.collider.GetComponentInParent<ShipController>();

                // ✅ Only allow selecting non-enemy ships
                if (ship != null && !ship.CompareTag("Enemy"))
                {
                    SelectShip(ship);
                    return;
                }
                else
                {
                    if (ship.CompareTag("Enemy") && SelectedShip ==null)
                    {
                        audioSource?.PlayOneShot(wrongSelectionSound);
                        return;
                    }
                }
            }
        }

        // Right click = deselect
        if (Input.GetMouseButtonDown(1))
        {
            DeselectShip();
        }
    }

    private void HandleHover()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, shipLayer))
        {
            var ship = hit.collider.GetComponentInParent<ShipController>();
            if (ship != null)
            {
                // If already hovering same ship, do nothing
                if (hoveredOutline != null && hoveredOutline.gameObject == ship.gameObject)
                    return;

                // Clear previous hover (if not selected)
                ClearHoverOutline();

                // ✅ Skip hover outline if this is the currently selected ship
                if (SelectedShip != null && ship == SelectedShip)
                    return;

                // Apply new hover
                var outline = ship.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = true;
                    outline.OutlineColor = ship.CompareTag("Enemy") ? enemyHoverColor : playerHoverColor;
                    hoveredOutline = outline;
                }

                return;
            }
        }

        // Nothing hit → clear hover
        ClearHoverOutline();
    }

    private void SelectShip(ShipController ship)
    {
        // Deselect previous one
        DeselectShip();

        SelectedShip = ship;
        selectedOutline = ship.GetComponent<Outline>();
        if (selectedOutline != null)
        {
            selectedOutline.enabled = true;
            selectedOutline.OutlineColor = selectedColor;
        }

        audioSource?.PlayOneShot(selectionSound);
        OnSelectionChanged?.Invoke(SelectedShip);
    }

    public void DeselectShip()
    {
        if (SelectedShip == null) return;

        if (selectedOutline != null)
            selectedOutline.enabled = false;

        SelectedShip = null;
        selectedOutline = null;

        OnSelectionChanged?.Invoke(null);
    }

    // ---------------------- Utility ----------------------

    private void ClearHoverOutline()
    {
        if (hoveredOutline != null && hoveredOutline != selectedOutline)
        {
            hoveredOutline.enabled = false;
            hoveredOutline = null;
        }
    }

    private void DisableAllOutlines()
    {
        Outline[] allOutlines = FindObjectsByType<Outline>(FindObjectsSortMode.InstanceID);
        foreach (var outline in allOutlines)
            outline.enabled = false;
    }
}
