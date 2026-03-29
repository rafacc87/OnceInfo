using System.Globalization;
using System.Text.RegularExpressions;

namespace OnceInfo.Services
{
    /// <summary>
    /// Parser para convertir valores de precios y series, y calcular resultados de rascas.
    /// </summary>
    public static class RascaParser
    {
        /// <summary>
        /// Convierte una cadena de texto con formato de moneda español a decimal.
        /// Maneja formatos como "1.234,56 €", "1,234.56€", "1234", "1.234", etc.
        /// </summary>
        /// <param name="s">Cadena a convertir</param>
        /// <returns>Valor decimal o 0 si no se puede parsear</returns>
        public static decimal ToDecimal(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return 0;

            s = s.Trim();

            // Eliminar espacios después del número (ej: "1.234 €" -> "1.234")
            var idxSpace = s.IndexOf(' ');
            if (idxSpace >= 0)
                s = s.Substring(0, idxSpace);

            // Eliminar símbolo de euro
            s = s.Replace("€", "").Trim();

            // Normalizar separadores decimales
            if (s.Contains(".") && s.Contains(","))
            {
                // Formato español: 1.234,56 -> quitar punto de miles, coma a punto decimal
                s = s.Replace(".", "").Replace(",", ".");
            }
            else if (s.Contains(",") && !s.Contains("."))
            {
                // Solo coma: 1234,56 -> punto decimal
                s = s.Replace(",", ".");
            }
            else if (!s.Contains(",") && s.Contains("."))
            {
                // Solo punto: puede ser decimal o separador de miles
                var pos = s.LastIndexOf('.');
                if (s.Length - pos - 1 > 2)
                {
                    // Más de 2 dígitos tras el punto -> era separador de miles
                    s = s.Replace(".", "");
                }
            }

            // Filtrar solo dígitos, punto y signo negativo
            s = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());

            if (string.IsNullOrWhiteSpace(s))
                return 0;

            return decimal.TryParse(s, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result)
                ? result
                : 0;
        }

        /// <summary>
        /// Calcula el porcentaje de premios por número de cupones.
        /// </summary>
        /// <param name="rascasPremiados">Número de rascas premiados</param>
        /// <param name="totalSeries">Total de series/cupones</param>
        /// <returns>Porcentaje de premios</returns>
        public static decimal CalcularPorcentajePremio(int rascasPremiados, decimal totalSeries)
        {
            if (totalSeries == 0)
                return 0;

            return (rascasPremiados / totalSeries) * 100;
        }

        /// <summary>
        /// Calcula los euros ganados por euro gastado.
        /// </summary>
        /// <param name="totalPremios">Suma total de premios en euros</param>
        /// <param name="precio">Precio del rasca</param>
        /// <param name="totalSeries">Total de series/cupones</param>
        /// <returns>Euros ganados por euro gastado</returns>
        public static decimal CalcularEurosPorEuroGastado(decimal totalPremios, decimal precio, decimal totalSeries)
        {
            if (precio == 0 || totalSeries == 0)
                return 0;

            return totalPremios / (precio * totalSeries);
        }

        /// <summary>
        /// Parsea los argumentos de línea de comandos y devuelve la configuración.
        /// </summary>
        /// <param name="args">Array de argumentos</param>
        /// <returns>Tupla con (top, nomin, euroGastado, precioMin)</returns>
        public static (int top, bool nomin, bool euroGastado, int precioMin) ParseArguments(string[] args)
        {
            int top = 0;
            bool nomin = false;
            bool euroGastado = false;
            int precioMin = 0;

            if (args == null || !args.Any())
                return (top, nomin, euroGastado, precioMin);

            string conf = "";
            foreach (string arg in args)
            {
                if (string.IsNullOrEmpty(conf))
                {
                    conf = arg;

                    switch (conf)
                    {
                        case "/t":
                        case "/p":
                            break;
                        case "/nomin":
                            conf = "";
                            nomin = true;
                            break;
                        case "/euro":
                            conf = "";
                            euroGastado = true;
                            break;
                        default:
                            conf = "";
                            break;
                    }
                }
                else
                {
                    if (conf == "/t" && int.TryParse(arg, out int topValue))
                        top = topValue;
                    else if (conf == "/p" && int.TryParse(arg, out int precioMinValue))
                        precioMin = precioMinValue;

                    conf = "";
                }
            }

            return (top, nomin, euroGastado, precioMin);
        }
    }
}