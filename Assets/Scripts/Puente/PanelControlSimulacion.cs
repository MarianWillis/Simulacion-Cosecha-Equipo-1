using UnityEngine;
using UnityEngine.UI;

namespace Puente
{
    /// <summary>
    /// Wrapper para botones de UI: ConexionSimulacion.EnviarReiniciar tiene
    /// varios parametros opcionales, y el picker de OnClick() del Inspector
    /// solo soporta metodos con cero o un argumento simple, asi que no se
    /// puede apuntar un boton directo a EnviarReiniciar. Este componente lee
    /// los InputField del panel, arma los argumentos y llama a
    /// ConexionSimulacion con un solo metodo sin parametros.
    ///
    /// Un campo vacio se manda como "sin cambios": el puente conserva el
    /// valor actual de ese parametro (ver PARAMETROS en granja.py).
    /// EnviarPausar/EnviarReanudar no necesitan wrapper: se pueden apuntar
    /// directo a ConexionSimulacion desde el boton.
    /// </summary>
    public class PanelControlSimulacion : MonoBehaviour
    {
        [SerializeField] private ConexionSimulacion conexion;

        [Header("Campos de reinicio (vacio = sin cambios)")]
        [SerializeField] private InputField campoFilas;
        [SerializeField] private InputField campoColumnas;
        [SerializeField] private InputField campoHarvesters;
        [SerializeField] private InputField campoTractores;
        [SerializeField] private InputField campoSeed;
        [SerializeField] private InputField campoSteps;

        public void EnviarReiniciarDesdeUI()
        {
            if (conexion == null)
            {
                Debug.LogError("PanelControlSimulacion no tiene asignada la ConexionSimulacion.");
                return;
            }

            int? filas = ParseOpcional(campoFilas);
            int? columnas = ParseOpcional(campoColumnas);

            int[] shape = null;
            if (filas.HasValue && columnas.HasValue)
            {
                shape = new[] { filas.Value, columnas.Value };
            }
            else if (filas.HasValue || columnas.HasValue)
            {
                Debug.LogWarning("Filas y columnas deben mandarse juntas: se ignora el tamano de grid.");
            }

            conexion.EnviarReiniciar(
                shape: shape,
                nHarvesters: ParseOpcional(campoHarvesters),
                nTractores: ParseOpcional(campoTractores),
                seed: ParseOpcional(campoSeed),
                steps: ParseOpcional(campoSteps));
        }

        private static int? ParseOpcional(InputField campo)
        {
            if (campo == null || string.IsNullOrWhiteSpace(campo.text))
                return null;

            if (int.TryParse(campo.text, out var valor))
                return valor;

            Debug.LogWarning($"Valor invalido en '{campo.name}': '{campo.text}'");
            return null;
        }
    }
}
