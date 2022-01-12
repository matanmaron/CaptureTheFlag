using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMenu : MonoBehaviour
{
    [SerializeField] List<Sprite> screens;
    [SerializeField] Image spriteRenderer;
    int index = 0;

    public void Onclick()
    {
        index++;
        if (index >= screens.Count)
        {
            index = 0;
        }
        spriteRenderer.sprite = screens[index];
    }
}
