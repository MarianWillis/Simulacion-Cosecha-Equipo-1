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
    }

    [Serializable]
    public class MetricasDTO
    {
        public float cosechado_pct;
        public int grano_entregado;
        public float gasolina_total;
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
}
