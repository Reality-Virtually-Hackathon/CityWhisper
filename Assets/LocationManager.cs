using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationManager : MonoBehaviour {
	public int distanceFromCamera = 1;

	private Vector3 origWorldPosition;

	private bool setOriginalValues = true;

	IEnumerator SetOriginalWorldCordinate() {
		// check if user has location service enabled
		if (!Input.location.isEnabledByUser)
			yield break;

		// Start service before querying location
		Input.location.Start (1f,.1f);

		// Wait until service initializes
		int maxWait = 20;
		while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0) {
			yield return new WaitForSeconds (1);
			maxWait--;
		}
			
		// Service didn't initialize in 20 seconds
		if (maxWait < 1) {
			Console.WriteLine("Timed out");
			yield break;
		}
			
		// Connection has failed
		if (Input.location.status == LocationServiceStatus.Failed) {
			Console.WriteLine("Unable to determine device location");
			yield break;
		} else {
			float latitude = Input.location.lastData.latitude;
			float longitude = Input.location.lastData.longitude;
			float altitude = Input.location.lastData.altitude;

			// Access granted and location value could be retrieved
			Console.WriteLine("Original Location: Latitude: " + latitude + " Longitude: " + longitude + 
				" Altitude: " + altitude);

			origWorldPosition = LocationHelper.calculateXYZFromWorldCoordinate(latitude, longitude, altitude);

			Console.WriteLine("Original World Location: X: " + origWorldPosition.x + " Y: " + origWorldPosition.y + 
				" Z: " + origWorldPosition.z);
		}

		Input.location.Stop();
	}

	public Vector3 getOrigWorldPosition() {
		return origWorldPosition;
	}

	// Use this for initialization
	void Start () {
		if (setOriginalValues) { 	
			StartCoroutine ("SetOriginalWorldCordinate");
			setOriginalValues = false;
		}
	}

	// Update is called once per frame
	void Update () {
		
	}
}
