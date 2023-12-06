using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HookShotSystem : MonoBehaviour
{
    public void ReadyHookShot()
    {
        Vector3 origin = Camera.main.transform.position;
        var direction = Camera.main.transform.forward;
        var pp = Physics.Raycast(origin, direction, out RaycastHit raycastHit);
        //if()
    }
}
