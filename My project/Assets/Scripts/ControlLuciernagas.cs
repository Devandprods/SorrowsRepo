using UnityEngine;

public class ControlLuciernagas : MonoBehaviour
{
    [SerializeField] private GameObject[] luciernagas;

    private void OnEnable()
    {
        CicloDiaNoche.AlCambiarFase += ManejarCambioFase;
    }

    private void OnDisable()
    {
        CicloDiaNoche.AlCambiarFase -= ManejarCambioFase;
    }

    private void ManejarCambioFase(FaseDia fase)
    {
        bool activar = fase == FaseDia.Noche;

        foreach (var luciernaga in luciernagas)
        {
            if (luciernaga == null) continue;
            ParticleSystem ps = luciernaga.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                if (activar) ps.Play();
                else ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}