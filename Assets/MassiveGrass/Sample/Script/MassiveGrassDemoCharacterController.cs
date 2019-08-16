using UnityEngine;

public class MassiveGrassDemoCharacterController : MonoBehaviour
{
    public Camera eye;
    public float velocity = 10f;

    private void Update()
    {
        var vec = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) vec += eye.transform.forward;
        if (Input.GetKey(KeyCode.A)) vec -= eye.transform.right;
        if (Input.GetKey(KeyCode.S)) vec -= eye.transform.forward;
        if (Input.GetKey(KeyCode.D)) vec += eye.transform.right;
        vec.Scale(new Vector3(1, 0, 1));
        transform.position += Time.deltaTime * vec.normalized * velocity;
    }
}
