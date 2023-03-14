using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIModelManager : MonoBehaviour {

    [Header("UI Element")]
    [SerializeField]
    private GameObject UiElement;

    private List<ModelLoader.Model> lModels;

    private void Start()
	{
        PopulateLibModel();
	}

    // Instantiate all UI element to drag and drop model
    // Setting up the sprite of the UI
    // Setting up the text of the UI
    // Store the Model to use at drag and drop,
    public void PopulateLibModel()
    {
        if ((lModels = ModelLoader.Instance.GetAllModel()) == null)
            return;
        GameObject lUIElement = null;
        Image lImage = null;
        Text lText = null;
        UIModel lScript = null;
        foreach (ModelLoader.Model lModel in lModels) {

            if ((lUIElement = Instantiate(UiElement, transform)) == null)
                continue;
			
            if ((lImage = lUIElement.transform.Find("Frame/Sprite").GetComponent<Image>()) != null)
                lImage.sprite = lModel.Sprite;
            if ((lText = lUIElement.GetComponentInChildren<Text>()) != null)
                lText.text = lModel.GameObject.name;
            if ((lScript = lUIElement.GetComponent<UIModel>()) != null)
                lScript.Model = lModel;
        }        
    }

    public void ClearElement()
    {
        foreach (Transform lChild in transform) {
            Destroy(lChild.gameObject);
        }
    }

    // Update the UI pool of models
    // Call when user import new model
    public void UpdateAvailableModel()
    {
        ClearElement();
        PopulateLibModel();
    }
}
