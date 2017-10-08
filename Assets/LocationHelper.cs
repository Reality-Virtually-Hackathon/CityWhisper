using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationHelper {
	public static Vector3 calculatePosition(Vector3 origWorldPosition, Vector3 targetWorldPosition) {
		return targetWorldPosition - origWorldPosition;
	}

	public static Vector3 calculateWorldPosition(Vector3 origWorldPosition, Vector3 targetPosition) {
		return origWorldPosition + targetPosition;
	}

	public static Vector3 calculateXYZFromWorldCoordinate(float latitude, float longitude, float altitude) {
		float radius = 6378137 + altitude;
		float rlat = latitude * Mathf.PI / 180;
		float rlong = longitude * Mathf.PI / 180;
		float x = radius * Mathf.Cos(rlat) * Mathf.Cos(rlong);
		float y = radius * Mathf.Cos(rlat) * Mathf.Sin(rlong);
		float z = radius * Mathf.Sin(rlat);
		float zFactor = 1.0f - 1.0f / 298.257223563f;
		float adjZ = z * zFactor;

		return new Vector3(x, y, adjZ);
	}
}
