using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ModLoader
{
    public class DebugRectTransform : MonoBehaviour
    {
        private Image image;
        private RectTransform rect, childRect;
        private GameObject child;
        private Color colour;
        private void Start()
        {
            child = new GameObject("Background Image", typeof(RectTransform));
            child.transform.SetParent(transform, false);
            image = child.AddComponent<Image>();
            childRect = child.GetComponent<RectTransform>();
            rect = GetComponent<RectTransform>();
            childRect.sizeDelta = rect.sizeDelta;
            childRect.anchoredPosition = rect.anchoredPosition;
            childRect.localPosition += new Vector3(0, 0, 0f);
            image.color = colour;
        }
        public void SetColour(Color colour)
        {
            this.colour = colour;
            if (image != null)
                image.color = colour;
        }
    }
}
