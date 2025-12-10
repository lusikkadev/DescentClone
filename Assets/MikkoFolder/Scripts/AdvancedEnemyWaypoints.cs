using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class AdvancedEnemyWaypoints : MonoBehaviour
{
    Transform Player;

    public float followSpeed = 300;
    public float turnSpeed = 5;
    public Transform waypointL;
    public Transform waypointR;
    public int waypointDistance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Player = GameObject.FindAnyObjectByType<PlayerController>().transform;
        waypointL.transform.position = transform.position + new Vector3(0, 0, -waypointDistance);
        waypointR.transform.position = transform.position + new Vector3(0, 0, waypointDistance);
    }

    // Update is called once per frame
    void Update()
    {
        if (Player == null) return;

        transform.position = Vector3.Lerp(
                            transform.position,
                            Player.position,
                            followSpeed * Time.deltaTime);



        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            Player.rotation,
                            turnSpeed * Time.deltaTime);

    }
}
