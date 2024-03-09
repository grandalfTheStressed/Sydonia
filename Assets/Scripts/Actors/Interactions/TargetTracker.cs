
using System.Collections.Generic;
using UnityEngine;

public class TargetTracker : MonoBehaviour {

    private Dictionary<int, GameObject> nearbyObjects = new();

    private const string LockOnTag = "LockOnTarget";

    public GameObject UpdateClosetTarget() {
        float minDistance = float.MaxValue;
        GameObject closestObject = null;

        using var enumerator = nearbyObjects.Values.GetEnumerator();
        
        if (nearbyObjects.Count < 1) return null;
        
        while(enumerator.MoveNext()) {
            float tempDistance = Vector3.SqrMagnitude(enumerator.Current.transform.position - transform.position);

            if (!(tempDistance < minDistance)) continue;
            
            minDistance = tempDistance;
            closestObject = enumerator.Current;
        }

        return closestObject;
    }

    private void OnTriggerEnter(Collider other) {
        if (!nearbyObjects.TryGetValue(other.GetInstanceID(), out GameObject value) && other.CompareTag(LockOnTag)) {
            nearbyObjects[other.GetInstanceID()] = other.transform.gameObject;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (nearbyObjects.TryGetValue(other.GetInstanceID(), out GameObject value)) {
            nearbyObjects.Remove(other.GetInstanceID());
        }
    }
}
