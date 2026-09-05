using TMPro;
using UnityEngine;

namespace Puente
{
    /// <summary>
    /// Wrapper para botones de UI: ConexionSimulacion.EnviarReiniciar tiene
    /// varios parametros opcionales, y el picker de OnClick() del Inspector
    /// solo soporta metodos con cero o un argumento simple, asi que no se
    /// puede apuntar un boton directo a EnviarReiniciar. Este componente lee
    /// los TMP_InputField del panel, arma los argumentos y llama a
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
        [SerializeField] private TMP_InputField campoFilas;
        [SerializeField] private TMP_InputField campoColumnas;
        [SerializeField] private TMP_InputField campoHarvesters;
        [SerializeField] private TMP_InputField campoTractores;
        [SerializeField] private TMP_InputField campoPctObstaculos;
        [SerializeField] private TMP_InputField campoCapacidadHarvester;
        [SerializeField] private TMP_InputField campoCapacidadTractor;
        [SerializeField] private TMP_InputField campoProbDescompostura;
        [SerializeField] private TMP_InputField campoSeed;
        [SerializeField] private TMP_InputField campoSteps;

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
                pctObstaculos: ParseOpcionalFloat(campoPctObstaculos),
                capacidadHarvester: ParseOpcional(campoCapacidadHarvester),
                capacidadTractor: ParseOpcional(campoCapacidadTractor),
                probDescompostura: ParseOpcionalFloat(campoProbDescompostura),
                seed: ParseOpcional(campoSeed),
                steps: ParseOpcional(campoSteps));
        }

        private static int? ParseOpcional(TMP_InputField campo)
        {
            if (campo == null || string.IsNullOrWhiteSpace(campo.text))
                return null;

            if (int.TryParse(campo.text, out var valor))
                return valor;

            Debug.LogWarning($"Valor invalido en '{campo.name}': '{campo.text}'");
            return null;
        }

        private static float? ParseOpcionalFloat(TMP_InputField campo)
        {
            if (campo == null || string.IsNullOrWhiteSpace(campo.text))
                return null;

            if (float.TryParse(campo.text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out var valor))
                return valor;

            Debug.LogWarning($"Valor invalido en '{campo.name}': '{campo.text}'");
            return null;
        }
    }
}
