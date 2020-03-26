using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicController : MonoBehaviour	//This is just a singleton that lives on the music object to make it persist between scenes. In theroy, this class can make any object it is attached to persist
{
	public static MusicController control;

	private void Awake()
	{
		DoSingletonCheck();
	}
	bool DoSingletonCheck() //This logic checks if this is the only copy of this class in existance. It also returns a bool so we can do some logic only after this returns true, though we're not currently implementing that
	{
		if (control != this)    //Is this object *already* the static reference?
		{
			if (control)    //If not, then is there *another* static reference?
			{
				Destroy(gameObject);    //If yes, we don't want this one
				return false;
			}
			else
			{
				control = this; //If else, make this the new static reference

				DontDestroyOnLoad(this);	//This is what makes the object persist between scenes

				return true;
			}
		}
		else
		{
			return true;
		}
	}
}
