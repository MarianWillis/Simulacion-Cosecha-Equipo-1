using UnityEngine;

namespace Puente
{
    /// <summary>
    /// Reencuadra automaticamente la camara asignada cada vez que llega un
    /// "init" nuevo del puente (ver GestorSimulacion.AlIniciar): al mandar
    /// "reiniciar" con un shape distinto (filas/columnas), el tamano real
    /// del campo cambia, y sin esto la camara se quedaria con el encuadre
    /// viejo.
    ///
    /// Regla de tres simple, sin FOV ni trigonometria: 30x30 ya se ve bien
    /// a la altura configurada en AlturaReferencia (la posicion que ya
    /// estaba puesta a mano en la escena) -- esa es la referencia. Para
    /// cualquier otro tamano, la altura escala proporcional al LADO MAS
    /// GRANDE del grid (filas o columnas, el que sea mayor), tratando el
    /// campo como si fuera cuadrado de ese tamano. Asi nunca se corta nada:
    /// un 40x30 se encuadra como un 40x40 (al lado de 30 le sobra margen,
    /// nunca le falta).
    ///
    /// El Far Clip Plane de la camara es un ajuste aparte, en el Inspector
    /// -- esta formula no lo usa para nada, asi que subirlo no cambia en
    /// nada como se ve un 30x30 (solo evita que un grid grande quede
    /// recortado por no poder verse tan lejos).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class EncuadreCamaraGranja : MonoBehaviour
    {
        private const int LadoReferencia = 30;
        private const float AlturaReferencia = 990f;

        [SerializeField] private Camera camara;

        private void Start()
        {
            // Start (no OnEnable/Awake): Unity garantiza que TODOS los Awake
            // de la escena ya corrieron antes de que corra cualquier Start,
            // asi que GestorSimulacion.Instancia ya existe seguro aca.
            if (camara == null) camara = GetComponent<Camera>();

            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar += Reencuadrar;
        }

        private void OnDestroy()
        {
            var gestor = GestorSimulacion.Instancia;
            if (gestor != null) gestor.AlIniciar -= Reencuadrar;
        }

        private void Reencuadrar(InitDTO init)
        {
            if (camara == null) return;

            float tamCelda = GestorSimulacion.Instancia.TamCelda;
            var centro = new Vector3((init.columnas - 1) * tamCelda / 2f, 0f, (init.filas - 1) * tamCelda / 2f);

            int ladoMayor = Mathf.Max(init.filas, init.columnas);
            float altura = AlturaReferencia * (ladoMayor / (float)LadoReferencia);

            camara.transform.position = new Vector3(centro.x, altura, centro.z);
        }
    }
}
