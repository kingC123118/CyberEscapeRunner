using UnityEngine;

/// <summary>
/// Keeps this object centered on the player's Z position every frame
/// (X/Y stay as authored). Used by the night-sky decoration rig so the
/// sky dome, stars, moon and meteor shower travel with the player in
/// an endless runner. Purely visual - does not touch gameplay objects.
/// </summary>
public class FollowPlayerZ : MonoBehaviour
{
    Transform target;

    void LateUpdate()
    {
        if (target == null)
        {
            var p = GameObject.Find("Player");
            if (p != null) target = p.transform;
        }
        if (target != null)
        {
            var v = transform.position;
            v.z = target.position.z;
            transform.position = v;
        }
    }
}
