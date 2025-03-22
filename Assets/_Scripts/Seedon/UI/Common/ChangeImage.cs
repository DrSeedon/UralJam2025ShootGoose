using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Seedon
{
    public class ChangeImage : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Sprite spriteOn;
        public Sprite spriteOff;
        public Image image;

        void Start()
        {
            image.sprite = spriteOff;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            image.sprite = spriteOn;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            image.sprite = spriteOff;
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            image.sprite = spriteOff;
        }

    }
}