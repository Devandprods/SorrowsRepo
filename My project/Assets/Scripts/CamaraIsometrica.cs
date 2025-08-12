using UnityEngine;

public class CamaraIsometrica : MonoBehaviour
{
    public Transform objetivo; // Tu personaje
    public Vector3 offset = new Vector3(0, 0, -10); // Asegurate de mantener Z negativo

    void LateUpdate()
    {
        transform.position = objetivo.position + offset;
    }
}