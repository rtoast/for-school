using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINotifier : MonoBehaviour {

    [SerializeField]
    private Text Text;

    private CanvasGroup mCanvasGroup;

    private bool mIsBusy;

    public bool IsBusy { get { return mIsBusy; } }

	private void Start()
	{
        if ((mCanvasGroup = GetComponent<CanvasGroup>()) != null)
            mCanvasGroup.alpha = 0F;
        gameObject.SetActive(false);
        mIsBusy = false;
	}

	public void Notify(float iDuration, string iMessage)
    {
        if (!mIsBusy) {
            if (Text == null || string.IsNullOrEmpty(iMessage))
                return;
            mIsBusy = true;
            Text.text = iMessage;
            gameObject.SetActive(true);
            StartCoroutine(NotifyImplAsync(iDuration));
        }
    }

    private IEnumerator NotifyImplAsync(float iDuration)
    {
        if (mCanvasGroup == null)
            yield return null;
        
		// Fade in
        yield return Utils.FadeToAsync(1F, 0.4F, mCanvasGroup);

        // Displaying time
        if (iDuration > 0)
            yield return new WaitForSeconds(iDuration);

        // Fade out
        yield return Utils.FadeToAsync(0F, 0.4F, mCanvasGroup, () => { mIsBusy = false; gameObject.SetActive(false); });
        mIsBusy = false;
        gameObject.SetActive(false);
    }

}
