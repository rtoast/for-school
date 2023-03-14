using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIElementGridParam : MonoBehaviour {

    [SerializeField]
    private Image icon;

    [SerializeField]
    private Text text;

    public Image Icon { get { return icon; } }

    public Text Text { get { return text; } }
}
