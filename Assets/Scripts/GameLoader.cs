using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;	//We need to import SceneManagement

public class GameLoader : MonoBehaviour	//This class deals with loading the game from the menu screen
{
	[SerializeField]
	private UnityEngine.UI.Image loadBar;	//The load bar graphic. Instead of importing the directive, I just directly reference it in this case
	[SerializeField]
	private GameObject menuParent, loadParent;	//The two transforms that hold our menu buttons (currently only one) and the loading related objects
	[SerializeField]
	private AudioSource clickSound;	//The sound to play when the button is clicked

    public void StartButtonPressed() //This function is called from the button component on the start button
	{
		menuParent.SetActive(false);	//We hide the menu
		loadParent.SetActive(true);		//And show the loading bar
		clickSound.Play();		//Then play the click sound

		StartCoroutine(LoadScene(1));	//Finally, start the coroutine to load the game scene
	}

	private IEnumerator LoadScene(int index) //This Coroutine loads the next scene asynchronously and updates the loading bar
	{
		AsyncOperation operation = SceneManager.LoadSceneAsync(index);	//We load the game scene asynchronously and cast the resulting AsyncOperation to a variable

		while (!operation.isDone) //This code will run once per frame when we aren't done loading
		{
			//Unity loads between 0 and 0.9 - this is because the last 0.1% is saved for actually changing scenes. We therefore remap the progress to be between 0 and 1
			float progress = Mathf.Clamp01(operation.progress / .9f);	

			loadBar.fillAmount = progress;	//Set the graphic's fill amount to reflect the loaded amount
			yield return null;	//Finally we yield until next frame
		}
	}
}
