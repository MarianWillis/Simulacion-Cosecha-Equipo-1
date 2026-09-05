using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Puente
{
    // Espejo en C# del protocolo JSON que manda puente_unity.py (ver
    // granja_multiagente/puente_unity.py). Los nombres de campo coinciden
    // exactamente con las claves del JSON para que Newtonsoft.Json los
    // mapee sin atributos extra.

    [Serializable]
    public class MensajeBase
    {
        public string tipo; // "init" | "paso" | "fin"
    }

    [Serializable]
    public class ObstaculoDTO
    {
        public int fila;
        public int col;
        public int tipo_visual; // 0 = roca, 1 = poste de riego
    }

    [Serializable]
    public class AgenteInitDTO
    {
        public int id;
        public string clase; // "harvester" | "tractor"
        public int fila;
        public int col;
        public int capacidad;
        public float gasolina_max;
    }

    [Serializable]
    public class InitDTO : MensajeBase
    {
        public int filas;
        public int columnas;
        public float tam_celda;
        public int[] silo;

        [JsonProperty("base")]
        public int[] baseCombustible;

        public List<ObstaculoDTO> obstaculos;
        public List<int[]> trigo_listo;
        public List<int[]> camino;
        public List<AgenteInitDTO> harvesters;
        public List<AgenteInitDTO> tractores;
    }

    [Serializable]
    public class AgenteEstadoDTO
    {
        public int id;
        public string clase; // "harvester" | "tractor"
        public int fila;
        public int col;
        public int[] direccion; // [df, dc]
        public string estado;
        public float gasolina;
        public int carga;

        // Solo aplica a harvesters (los tractores siempre mandan 0). A
        // diferencia de `carga`, que baja al descargar en el tractor, este
        // es el total acumulado de celdas cosechadas desde que arranco la sim.
        public int cosechado_total;
    }

    [Serializable]
    public class MetricasDTO
    {
        public float cosechado_pct;
        public int grano_entregado;
        public int recargas_totales;
        public int distancia_total;
        public int descomposturas_totales;
    }

    [Serializable]
    public class PasoDTO : MensajeBase
    {
        public int t;
        public List<AgenteEstadoDTO> agentes;
        public List<int[]> cosechadas;
        public MetricasDTO metricas;
    }

    [Serializable]
    public class FinDTO : MensajeBase
    {
        public Dictionary<string, object> reportes;
    }

    // Comandos que Unity manda al puente (ver puente_unity.py, _escuchar_comandos).
    // No hace falta un "tipo" comun aca porque cada uno se serializa por su cuenta
    // con JsonConvert.SerializeObject justo antes de enviarlo.

    [Serializable]
    public class ComandoSimpleDTO
    {
        public string tipo; // "pausar" | "reanudar"
    }

    [Serializable]
    public class ParametrosReinicioDTO
    {
        // Solo se serializan los campos que se setean explicitamente (ver
        // NullValueHandling.Ignore en ConexionSimulacion.EnviarReiniciar):
        // el puente conserva el valor actual de cualquier parametro omitido.
        public int[] shape;
        public int? n_harvesters;
        public int? n_tractores;
        public float? pct_obstaculos;
        public int? capacidad_harvester;
        public int? capacidad_tractor;
        public float? prob_descompostura;
        public int? seed;
        public int? steps;
    }

    [Serializable]
    public class ComandoReiniciarDTO
    {
        public string tipo = "reiniciar";
        public ParametrosReinicioDTO parametros;
    }
}
