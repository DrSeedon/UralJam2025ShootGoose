using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Seedon
{
    /// <summary>
    /// Класс для управления UI панелью.
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        [HideInInspector] public UIPanelsManager uiPanelsManager;
        public UnityEvent OnShow;
        public UnityEvent OnHide;
        public bool isHideOnStart = false;

        private async void Start()
        {
            if (isHideOnStart)
            {
                await Task.Delay(2000);
                Hide();
            }
        }

        public async void Show()
        {
            if (uiPanelsManager != null) uiPanelsManager.HideAll();
            gameObject.SetActive(true);
            await Task.Delay(100);
            OnShow?.Invoke();
        }

        public void Hide()
        {
            OnHide?.Invoke();
            gameObject.SetActive(false);
        }
    }
}