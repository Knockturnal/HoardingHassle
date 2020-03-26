using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Crane : MonoBehaviour	//This class deals with the crane and all the main gameplay elements that derive from it
{
	#region Inspector Assignable Fields

	[SerializeField]
	private GameObject lastChainLink, TPPrefab, placeParticles;	//The chain link to which our block will attach, the prefab of the block, and the particles to play when we place a block
	[SerializeField]
	private float movePerBlock, swingStrength, pitchVariance;	//How much the camera moves up per block, how much the crane swings, and how much variance we allow in the different SFX
	[SerializeField]
	private CameraMover camMover;	//The script governing the camera's movement
	[SerializeField]
	private UIController scorer;	//The script that updates the UI and keeps track of score
	[SerializeField]
	private bool disablePhysicsOnPlacedBlocks;	//To make the game easier, we can freeze objects lowe down in the tower to make it sway less
	[SerializeField]
	private AudioSource releaseSound, spawnSound, loseSound, placeSound;    //The different sound effects. All these exist on the same GameObject as this script

	#endregion

	#region Private fields

	private GameObject holding, lastDropped, lastPlaced;	//The object we're holding, the last one we held, and the one before that
	private Rigidbody lastLinkRB;	//The RigidBody of the last link, so we only have to cache it once
	private float lastY;	//Where we die if we drop a block below
	private bool dead;		//Toggles when we are in the dead state
	private List<GameObject> placed;	//All the blocks we place. We keep track of them so we can remove them when we soft restart
	private Vector3 startPos;   //The start position of the crane object

	#endregion

	#region Private Functions

	private void Start()
	{
		lastLinkRB = lastChainLink.GetComponent<Rigidbody>();	//We cache the RigidBody of the last chain link
		placed = new List<GameObject>();	//Initializing the List. This always needs to be done once. (Unless the list is exposed in the inspector)
		startPos = transform.position;		//Set the start position to our current position, so we can return here when we soft restart
		Invoke("SpawnNewHeldObject", 1f);   //Spawn the first held block in one second
	}
	private void SpawnNewHeldObject()
	{
		//Instantiate the block prefab at the last chain link with the crane (this) as a parent, and a random rotation in the Y axis
		holding = Instantiate(TPPrefab, lastChainLink.transform.position, lastChainLink.transform.rotation * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), transform);
		holding.GetComponent<HingeJoint>().connectedBody = lastLinkRB;	//Assign the last chain link as the anchor of the hinge joint on the block (this is what makes it dangle from the chain)
		Rigidbody rb = holding.GetComponent<Rigidbody>();	//We retrieve the block's RigidBody
		rb.AddForce(Vector3.left * Random.Range(swingStrength, -swingStrength) * 100f, ForceMode.Impulse);		//Add some sideways force randomly based on the swingStrength. TODO: Make this higher the more score we get?
		rb.centerOfMass = rb.centerOfMass + (Vector3.down * 1.5f);	//We offset the RigidBody's center of mass to the *bottom* of the block. This makes the game way easier because the blocks settle faster

		Transform model = holding.transform.GetChild(0);	//This is not the best way to reference the child transform, but it spares some lines of code as long as we're pedantic that the model is always child 0

		model.localScale = Vector3.zero;	//We start by setting the new block's scale all the way to zero
		Tweener t = model.DOScale(Vector3.one, 0.2f);	//Then using a tween we quickly scale it up again
		t.SetEase(Ease.OutBounce);	//This ease type will go quickly at first and then bounce when it reaches the target

		spawnSound.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);	//Set the SFX's pitch randomly based on pitchVariance
		spawnSound.Play();	//Play the spawn sound
	}

	private void Update()
	{
		if (Input.GetMouseButtonDown(0))
		{
			if (holding != null && !dead)	//Run this code if we're holding a block and are still alive
			{
				DropBlock();
			}
			else if (dead) 
			{
				Restart();
			}
		}

		if (lastDropped)	//If there is an object we last dropped run this code (should be true for all but the first block)
		{
			if (lastDropped.transform.position.y < lastY && !dead)	//If the last dropped block has fallen below the death threshold and we're not already dead, do Game Over
			{
				GameOver();
			}
		}

		scorer.SetHeight((transform.position.y - startPos.y) / 2f);		//We send our current height minus our start height. Each block is 2 units tall so we divide by 2. This is displayed in the HUD as "tp"
	}

	private void DropBlock()	//Runs when we click LMB while alive
	{
		Destroy(holding.GetComponent<HingeJoint>());	//Release the block by removing the HingeJoint component from it
		holding.transform.parent = null;	//We also unparent the block we release so it exists on the root of the scene

		if (lastDropped)	//There is always a last block *except* for when we place the first block - which is why we have to check
		{
			lastY = lastDropped.transform.position.y + 1f;	//Set the death border to where the last block landed plus one unit up
			lastPlaced = lastDropped;	//We set the lastPlaced to what was in the lastDropped. In other words, we have now placed the last dropped block (before this one)
		}

		placed.Add(holding);	//Add the newly dropped block to the list of all blocks	
		lastDropped = holding;	//Set the block we held to now be the one we last dropped
		holding = null;		//We're not holding anything anymore
		Invoke("MoveNextBlock", 1.5f);	//Move the crane up in 1.5 seconds

		releaseSound.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);	//Set the pitch of the release sound randomly based on the soundVariance
		releaseSound.Play();	//Play the release sound
	}

	private void GameOver()
	{
		CancelInvoke();		//We cancel any invoke requests running so we don't suddenly trigger gameplay code while dead
		loseSound.Play();	//Play the losing sound
		camMover.DoRecap();	//We tell the camera moving script to do the game over behaviour
		scorer.GameOver();	//And the scoring/UI script to do the same
		dead = true;	//While we are dead, we are not activating any new gameplay functionality
	}

	private void Restart()	//We're doing a soft restart instead of reloading the scene. It is both cheaper and more elegant - but more prone to bugs
	{
		for (int i = 0; i < placed.Count; i++)	//Loops over the list of placed blocks and destroys all of them
		{
			Destroy(placed[i]);
		}

		placed.Clear();	//Then clear out the references from the list

		dead = false;	//We are no longer dead
		lastY = 0f;		//The death border is reset
		lastDropped = null;	//The last held object is now null
		lastPlaced = null;	//The last placed object is also null

		transform.position = startPos;	//Reset the crane to its starting position

		camMover.Restart();	//Tell the camera mover script that we are restarting the game
		scorer.Restart();	//Tell the score/UI script the same

		if (holding == null)	//If we aren't already holding a block, spawn one in 1 second (it is possible the tower crubles while the crane holds a block)
		{
			Invoke("SpawnNewHeldObject", 1f);
		}
	}

	private void MoveNextBlock() //This function moves the crane up and plays some effects
	{
		Tweener t = transform.DOLocalMoveY(transform.localPosition.y + movePerBlock, 0.5f);	//Move the crane up
		t.SetEase(Ease.OutExpo);    //OutExpo starts quickly and then slowly comes to a halt

		scorer.BlockAdded(ScoreBlock());    //We tell the score script to score our last block with our accuracy
		PlaceDropped();	//Then we "place" the last block we dropped

		Invoke("SpawnNewHeldObject", 0.5f);	//Spawn a new block in the crane in .5 seconds

	}

	private void PlaceDropped()	//This is called when we move to the next position. We then assume that the last block has settled
	{
		placeSound.Play();	//Plays the placement sound (not the combo sound)
		placeParticles.transform.position = lastDropped.transform.position - (lastDropped.transform.up * 2f);	//Move the particle system to the bottom of the block
		placeParticles.GetComponent<ParticleSystem>().Play();	//Play the particle system

		if (lastPlaced)	//If there is a last placed block (when we have placed 2 or more blocks) AND we have selected to disable physics on placed blocks, we do so
		{
			lastPlaced.GetComponent<Rigidbody>().isKinematic = disablePhysicsOnPlacedBlocks;
		}
	}

	private float ScoreBlock()	//Compares how close to the center we got and score accordingly
	{
		float accuracyLast = 10f;	//We start with the highest possible score of 10

		if (lastPlaced)	//If this is the first block we don't run this code and just score a 10
		{
			Vector3 blockBottom = lastDropped.transform.position - (lastDropped.transform.up * 2f);	//Calculate the bottom of the block
			accuracyLast -= Mathf.Abs(blockBottom.x - lastPlaced.transform.position.x) * 15f;  //We subtract from the accuracy the difference in X position between the last placed block and the last dropped one
			accuracyLast = Mathf.Max(0f, accuracyLast); //We check that the number is not under 0. If it is we select 0 instad
		}

		return accuracyLast;
	}

	private void OnDrawGizmos()		//Visualizes the line where which we lose if a block falls under
	{
		Gizmos.color = Color.red;
		Gizmos.DrawLine(new Vector3(-10f, lastY, 0f), new Vector3(10f, lastY, 0f));	//We draw a horizontal line where the death border is
	}

	#endregion 
}
