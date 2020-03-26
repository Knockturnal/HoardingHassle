using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIController : MonoBehaviour   //This class deals with the in game UI and score
{
	#region Inspector Assignable Fields

	[SerializeField]	//The time within which a combo can happen, how fast the score text rises, how much the pitch goes up on the combo sound per combo, and the max pitch we can reach on the combo sound
	private float baseComboTime, risingTextTime, comboPitchIncrease, maxPitchIncrease;
	[SerializeField]
	private TextMeshProUGUI heightText, scoreText, comboText, gameOverScoreText;    //The different UI elements
	[SerializeField]
	private Image comboTimer;	//The bar that counts down the combo time
	[SerializeField]
	private GameObject comboPanel, risingScorePrefab, canvasParent, gameOverParent;	//The parents of the different UI groups
	[SerializeField]
	private AudioSource comboSound; //The sound to play when we get a combo

	#endregion

	#region Private fields

	private float score, height;	//The current score and tower height
	private int combo;	//The current combo
	Tweener fillTween;  //A reference to the tween that counts down the combo timer in the UI

	#endregion

	#region Public Functions
	public void BlockAdded(float accuracy)	//This is called from the Crane class and passes a float describing how accurate the last drop was between 0 and 10
	{
		score += accuracy + ((float)combo);	//The score we get for the drop is equal to the accuracy of the drop plus the current combo length
		scoreText.text = Mathf.Round(score).ToString() + " pt";	//Update the UI score display
		scoreText.transform.DOShakeScale(0.2f);	//We shake the UI score display to enforce that the value has changed

		SpawnRisingScoreText(Mathf.Round(accuracy));	//Spawn the rising score displaying what accuracy we got for the last drop

		combo++;	//Increment the combo counter
		comboText.text = (combo - 1).ToString();	//Set the combo text to the current combo *minus* one. (We start counting combos after two drops, which is called a 1 combo)
		comboText.transform.DOShakeScale(0.2f);	//Just as the score text, we shake the text to enforce that it changed

		if (combo > 1)	//If we are currently on a combo of 2 or more, display the combo related UI elements
		{
			comboPanel.SetActive(true);
			comboSound.pitch = 0.9f + Mathf.Min(comboPitchIncrease * combo, maxPitchIncrease);	//Set the pitch of the combo sound based on the combo length (it gets brighter and brighter)
			comboSound.Play();	//Play the combo sound
		}

		if (fillTween != null) { fillTween.Complete(); }	//If the combo timer tween exists we have to make sure that it completes before starting it again
		comboTimer.fillAmount = 1f;	//Set the combo timer to the full position
		fillTween = comboTimer.DOFillAmount(0f, baseComboTime);	//We activate a tween that decreases the fill amount on the combo timer over the amount of time dictated by "baseComboTime"
		fillTween.SetEase(Ease.Linear);	//Set the ease to linear so the countdown decreases evenly

		CancelInvoke("EndCombo");	//Since we just got a new block down we have to cancel the last call to stop the combo
		Invoke("EndCombo", baseComboTime);  //We call a function that ends the combo in the amount of time dictated by "baseComboTime". If we place a new block before that, this call is canceled (line above)
	}
	public void SetHeight(float currentHeight)  //The Crane script updates this with the crane's current height
	{
		height = currentHeight;
		heightText.text = (((float)Mathf.RoundToInt(height * 100f)) / 100f).ToString() + " tp";    //To get only two decimal places we multiply by 100, cast to int to remove the rest of the decimals, then cast back and divide by 100
	}
	public void GameOver() 
	{
		EndCombo();	//When we lose we end the combo regardless of circumstance
		gameOverParent.SetActive(true);	//Display the game over screen object
		gameOverScoreText.text = "You Horded: " + Mathf.Round(score).ToString() + "!";	//Change the game over text to reflect the score
		canvasParent.SetActive(false);	//Set the other UI to not display when we're seeing the Game Over screen
	}
	public void Restart()
	{
		//Reset all the values and UI texts
		score = 0f;	
		scoreText.text = "0 pt";
		height = 0f;
		heightText.text = "0 tp";
		combo = 0;

		gameOverParent.SetActive(false);	//Hide the game over screen
		canvasParent.SetActive(true);	//Display the game UI again
	}

	#endregion

	#region Private Functions
	private void SpawnRisingScoreText(float scoreToDisplay)	//This function spawns a text object in the middle of the screen that slowly rises and goes invisible
	{
		GameObject newRisingScore = Instantiate(risingScorePrefab, canvasParent.transform);	//Instantiate the score text prefab childed to the canvas
		RectTransform risingScoreTransform = newRisingScore.GetComponent<RectTransform>();	//We retrieve the score text's rect transform
		TextMeshProUGUI risingScoreText = newRisingScore.GetComponent<TextMeshProUGUI>();	//We retrieve the core text's text component

		risingScoreTransform.DOAnchorPosY(400f, risingTextTime);	//We assign a tween to move the text up 400 points (relative to the canvas) over the time dictated by risingTextTime

		risingScoreText.text = Mathf.CeilToInt(scoreToDisplay).ToString() + "!";	//We set the rising text's text to the accuracy of the last drop. TODO: Make the text say "Great", "Nice", "OK", "Bad" instead of showing numbers
		risingScoreText.fontSize = scoreToDisplay * 10f;	//Then set the font size to reflect the accuracy. The better, the bigger text!

		Color transparentVariant = risingScoreText.color;	//Setup a version of the text's color that is identical but 100% transparent that we can fade to
		transparentVariant.a = 0f;	//Set the above color's alpha value to 0 so it's completely transparent
		risingScoreText.DOColor(transparentVariant, risingTextTime);	//Create a tween that fades the color into the transparent version over the same time the text rises

		Destroy(newRisingScore, risingTextTime);	//Finally, when we are done rising, destroy the new instance
	}
	private void EndCombo() //We end the combo
	{
		combo = 0;      //Set the current combo number to zero
		comboPanel.SetActive(false);    //Disable the UI element that holds the combo related graphics
	}

	#endregion
}
