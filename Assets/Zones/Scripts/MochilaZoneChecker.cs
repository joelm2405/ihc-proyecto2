using UnityEngine;

public class MochilaZoneChecker : MonoBehaviour
{
    private bool puntosSumados = false; // Controlar si ya se sumaron los puntos.

    void OnTriggerEnter(Collider other)
    {
        // Verificamos si la mochila entró en la zona azul
        if (other.CompareTag("BlueZone")) // Asegúrate de que el "BlueZone" es el tag correcto de la zona azul.
        {
            // Si no se han sumado los puntos aún
            if (!puntosSumados)
            {
                ScoreManager.I.AddPoints(50f); // Sumar 50 puntos solo una vez
                puntosSumados = true; // Marcar que los puntos ya han sido sumados.
                Debug.Log("Puntos sumados por zona azul!");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        // Reseteamos la flag cuando la mochila sale de la zona azul
        if (other.CompareTag("BlueZone"))
        {
            puntosSumados = false; // Permitir sumar puntos nuevamente si la mochila vuelve a la zona.
        }
    }
}
