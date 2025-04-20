using UnityEngine;

public class Camera_follow : MonoBehaviour
{
    public Transform player; // Reference to the player's transform
    public Vector3 offset;   // Offset distance between the camera and the player
    public float scrollSpeed = 2f; // Speed of zooming in/out
    public float minDistance = 2f; // Minimum zoom distance
    public float maxDistance = 15f; // Maximum zoom distance

    private float yToZRatio = -9f / 5f; // Ratio between y and z (z = -9 when y = 5)

    // Update is called once per frame
    void LateUpdate()
    {
        // Update the camera's position to follow the player with the offset
        if (player != null)
        {
            transform.position = player.position + offset;
        }

        // Adjust the offset based on the scroll wheel input
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            // Modify the y component of the offset
            offset.y -= scrollInput * scrollSpeed;

            // Clamp the y component to stay within the min and max distance
            offset.y = Mathf.Clamp(offset.y, minDistance, maxDistance);

            // Adjust the z component based on the y component using the ratio
            offset.z = offset.y * yToZRatio;
        }
    }
}