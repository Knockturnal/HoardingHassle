using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cinemachine;

public class CameraMover : MonoBehaviour	//This class moves the game camera
{
	[SerializeField]
	private float dampTime;	//The time we use to "catch up" with the moving crane
	[SerializeField]
	private Transform followTarget;	//Our current target. This will be the crane during gameplay
	[SerializeField]
	private bool keepOffset;	//If this bool is unchecked the target will always be centered on screen. Otherwise we keep it the same place on the screen as it starts
	[SerializeField]
	private GameObject wideCam; //This is currently unused, but is a Cinemachine VCam we can switch to to zoom out and see our masterpiece tower

	private Vector3 speed, offset, nextPos, startPos;   //The reference speed used to calculate the damping, the offset of the camera to the target, a reference to where we move to next, and where we start
	private Tweener endCrane;   //Reference to the tween that scrolls down when we get a game over
	private bool dead;  //Keep track of whether we're in play mode or not

	private void Awake()
	{
		startPos = transform.position;  //Store our start position so we can return here when we soft reset
		offset = followTarget.position;	//Start by storing the target's position as our offset
		offset.z = startPos.z;	//Then set the target Z value to the camera's Z value so we only move in one plane (considering this is a 2D style game)
		offset = startPos - offset;	//Calculate the offset to the target so we can keep the distance the same if we desire
	}

	private void LateUpdate()	//Runs after update. This is important as we want to move the camera only *after* all other objects are done moving this frame - otherwise we get stutter
	{
		if (!dead)	//Only follow the crane when we're alive
		{
			nextPos = followTarget.position;	//We start by putting our target in here raw, and then do the smooth damping after having set the Z position
			nextPos.z = startPos.z; //We do *not* want to move the camera's Z position as this is a 2D game, so set that back to the starting Z position

			//Smoothly move the camera towards the target. There is an "inline if" statement that asks whether we care about the offset or not
			nextPos = Vector3.SmoothDamp(transform.position, nextPos + (keepOffset == true ? offset : Vector3.zero), ref speed, dampTime);
			transform.position = nextPos;	
		}
	}
	IEnumerator Recap()	//This function makes the camera scroll back down the tower
	{
		dead = true;	//First, we set that we're dead so we're not following the crane
		yield return new WaitForSeconds(2f);	//Wait a couple seconds before starting to scroll
		float scrollTime = (transform.position.y - startPos.y) / 3f;	//The scroll time is based on the height of the tower. Could get tedious if you have a very high tower?
		endCrane = transform.DOMoveY(startPos.y, scrollTime);	//Assign the tween to move the camera down to the start position with the calculated time
		endCrane.SetEase(Ease.InOutSine);	//This ease mode starts and ends smoothly, moving fastest in the middle

		//Enable this code block to make the camera zoom out at the end
		/*
				yield return new WaitForSeconds(scrollTime + 1f);
				wideCam.SetActive(true);
				GetComponent<CinemachineVirtualCamera>().enabled = false;*/
	}
	public void DoRecap()	//This is called from the crane script and simply runs our coroutine
	{
		StartCoroutine(Recap());	//Coroutines cannot generally be called outside this class, so we have to have a function between
	}

	public void Restart()
	{
		StopCoroutine(Recap());	//We have to stop the coroutine if its running so that we don't start executing code out of order
		endCrane.Complete();	//Complete the tween if we haven't. In effect moves the camera to the start location instantly
		dead = false;	//Say we're no longer dead so we begin following the crane again
	}

}
