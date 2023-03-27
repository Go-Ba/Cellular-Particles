using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ElementSelectButton : MonoBehaviour, IPointerClickHandler
{
    ObjectParticleSim2D particleSim;
    [SerializeField] ElementData element;
    [SerializeField] Image buttonVisual;

    private void Start()
    {
        buttonVisual.color = element.color;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (particleSim == null)
            particleSim = FindObjectOfType<ObjectParticleSim2D>();

        particleSim.SetSelectedElement(element);
    }
}
