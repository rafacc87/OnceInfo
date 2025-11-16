using HtmlAgilityPack;
using OnceInfo.Models;
using OnceInfo.Properties;
using OnceInfo.Services;
using System.Diagnostics;
using System.Globalization;

namespace OnceInfo
{
    internal class Program
    {
        // Configuraciones
        private static bool nomin = false;
        private static int top = 0;
        private static int precioMin = 0;
        private static bool euroGastado = false; // Euro gastado o papeleta comprada

        static async Task Main(string[] args)
        {
            PlaywrightService.EnsurePlaywrightBrowsers();
            
            ReadParams(args);

            Console.WriteLine("¡Bienvenido a OnceInfo!");
            Console.WriteLine();

            Console.WriteLine("- Obteniendo listado de rascas:");
            HtmlDocument doc = await PlaywrightService.GetHtmlDocumentAsync(Resources.Url + Resources.Path_rasca);
            if (doc.GetElementbyId("msgerror") != null)
            {
                Console.WriteLine($">> No está disponible ahora mismo juegosonce <<");
                return;
            }
            HtmlNodeCollection lstRascasTodos = doc.DocumentNode.SelectNodes(Resources.Nodo_Listado_Rascas);

            // Verifica que se hayan conseguido obtener rascas
            if (lstRascasTodos == null)
            {
                Console.WriteLine($">> Parece que OnceInfo no puede obtener datos de juegosonce <<");
                return;
            }

            Console.WriteLine($"> Encontrados { lstRascasTodos.Count } rascas.");
            Console.WriteLine();
            Console.WriteLine("- Analizando rascas:");

            var rascaItems = lstRascasTodos.Select(n => new
            {
                Href = n.GetAttributeValue("href", string.Empty),
                Precio = n.SelectSingleNode(Resources.Nodo_Precio_Listado_Rascas).GetAttributeValue("data-precio", string.Empty)
            }).ToList();

            List<RascaResultado> resultadosCon = new();
            List<RascaResultado> resultadosSin = new();
            int total = rascaItems.Count;
            for (int p = 0; p < total; p++)
            {
                Thread.Sleep(1000);
                ShowProgress(p, total);

                var item = rascaItems[p];
                HtmlDocument rascaDoc;
                try
                {
                    rascaDoc = await PlaywrightService.GetHtmlDocumentAsync(Resources.Url + item.Href);
                }
                catch (System.Exception ex)
                {
                    try
                    {
                        Thread.Sleep(5000);
                        rascaDoc = await PlaywrightService.GetHtmlDocumentAsync(Resources.Url + item.Href, timeout: 90000);
                    }
                    catch
                    {
                        Console.WriteLine($"\n>> Error cargando {item.Href} tras reintento: {ex.Message}");
                        continue;
                    }
                    continue;
                }

                resultadosCon.AddRange(ParseRascaResultados(rascaDoc, item.Precio, excludeSameValue: false, precioMinArg: precioMin));
                resultadosSin.AddRange(ParseRascaResultados(rascaDoc, item.Precio, excludeSameValue: true, precioMinArg: precioMin));
            }
            ShowProgress(total, total);

            Console.WriteLine();
            Console.WriteLine("> Completado");

            // Selecciona qué mostrar en consola según la opción /nomin
            var resultados = nomin ? resultadosSin : resultadosCon;
            string tipoProbabilidad = euroGastado ? "por euro gastado" : "por cupón";

            if (top > 0)
            {
                resultados = resultados.OrderByDescending(x => x.PorcentajePremio).Take(top).ToList();
                Console.WriteLine($"Aquí tienes el top {top} {tipoProbabilidad}:");
            }
            else
            {
                resultados = resultados.OrderByDescending(x => x.PorcentajePremio).ToList();
                Console.WriteLine($"Aquí tienes todos los resultados {tipoProbabilidad}:");
            }

            int i = 0;
            foreach (var r in resultados)
            {
                string inicio = $"{++i} > {r.Nombre} con precio {r.Precio} euros tiene";
                decimal porcentaje = decimal.Round(r.PorcentajePremio, 2, MidpointRounding.AwayFromZero);

                if (euroGastado)
                    Console.WriteLine($"{inicio} un promedio de {porcentaje} euros en premios por euro gastado");
                else
                    Console.WriteLine($"{inicio} una probabilidad de {porcentaje}%");
            }

            // Genera informe HTML con ambas versiones (con y sin mismo valor) y lo abre
            var html = HtmlReportGenerator.GenerateReportHtml(resultadosCon, resultadosSin, precioMin);
            var outPath = Path.Combine(Environment.CurrentDirectory, "onceinfo-report.html");
            File.WriteAllText(outPath, html, System.Text.Encoding.UTF8);
            Process.Start(new ProcessStartInfo { FileName = outPath, UseShellExecute = true });

            Console.ReadKey();
        }

        private static List<RascaResultado> ParseRascaResultados(HtmlDocument doc, string precio, bool excludeSameValue, int precioMinArg)
        {
            var list = new List<RascaResultado>();
            string nombre = doc.DocumentNode.SelectSingleNode(Resources.Nodo_Nombre_Rasca)?.InnerText?.Trim() ?? string.Empty;
            var pRasca = doc.DocumentNode.SelectNodes(Resources.Nodos_PRasca);
            var listadoRasca = doc.DocumentNode.SelectNodes(Resources.Nodos_Listado_Rasca);
            if (pRasca == null || listadoRasca == null) return list;

            int i = 0;
            string[] precios = precio.Split(" - ");

            decimal ToDecimal(string s)
            {
                if (string.IsNullOrWhiteSpace(s)) return 0;
                s = s.Trim();
                var idxSpace = s.IndexOf(' ');
                if (idxSpace >= 0) s = s.Substring(0, idxSpace);
                s = s.Replace("€", "").Trim();
                if (s.Contains(".") && s.Contains(",")) s = s.Replace(".", "").Replace(",", ".");
                else if (s.Contains(",") && !s.Contains(".")) s = s.Replace(",", ".");
                else if (!s.Contains(",") && s.Contains("."))
                {
                    // si hay punto y más de 2 dígitos tras el punto, asumimos separador de miles
                    var pos = s.LastIndexOf('.');
                    if (s.Length - pos - 1 > 2) s = s.Replace(".", "");
                }
                s = new string(s.Where(c => char.IsDigit(c) || c == '.' || c == '-').ToArray());
                if (string.IsNullOrWhiteSpace(s)) return 0;
                return decimal.Parse(s, CultureInfo.InvariantCulture);
            }

            foreach (var p in pRasca)
            {
                string serie = p.InnerHtml;
                if (serie.StartsWith(Resources.TextoP))
                {
                    serie = serie[(Resources.TextoP.Length)..].Replace(":", "");
                    if (serie.Contains(Resources.Texto_MultiP))
                        serie = serie[..serie.IndexOf(" ")];

                    if (i >= precios.Length) break; // seguridad

                    var premiosNode = listadoRasca[i].SelectNodes(Resources.Nodos_Premio);
                    decimal totalPremios = 0;
                    int rascasPremiados = 0;

                    if (premiosNode != null)
                    {
                        foreach (var premio in premiosNode)
                        {
                            string texto = premio.InnerHtml;
                            texto = texto.Substring(0, texto.IndexOf(" ")).Replace(".", "").Trim();

                            string premioDe = premio.SelectSingleNode("./span").InnerHtml;
                            int indiceEspacio = premioDe.IndexOf(" ");
                            if (indiceEspacio != -1)
                                premioDe = premioDe[..indiceEspacio].Replace(".", "");
                            else
                                premioDe = premioDe.Replace("€", "").Trim();

                            decimal premioDeDec = ToDecimal(premioDe);
                            decimal precioI = ToDecimal(precios[i]);

                            // Mantiene la lógica original: si se cumple la condición se rompe el bucle de premios
                            if ((excludeSameValue && premioDeDec == precioI) || (precioMinArg > premioDeDec))
                            {
                                break;
                            }

                            if (int.TryParse(texto, out int veces))
                            {
                                rascasPremiados += veces;
                                totalPremios += premioDeDec * veces;
                            }
                        }
                    }

                    var rasca = new RascaResultado()
                    {
                        Nombre = nombre,
                        Serie = serie,
                        Precio = precios[i],
                        RascasPremiados = rascasPremiados,
                    };

                    if (euroGastado)
                    {
                        var priceDecimal = ToDecimal(precios[i]);
                        var serieDecimal = ToDecimal(serie);
                        if (priceDecimal != 0 && serieDecimal != 0)
                            rasca.PorcentajePremio = totalPremios / (priceDecimal * serieDecimal);
                        else
                            rasca.PorcentajePremio = 0;
                    }
                    else
                    {
                        var serieDecimal = ToDecimal(serie);
                        if (serieDecimal != 0)
                            rasca.PorcentajePremio = (rascasPremiados / serieDecimal) * 100;
                        else
                            rasca.PorcentajePremio = 0;
                    }

                    list.Add(rasca);
                    i++;
                }
            }

            return list;
        }

        private static void ShowProgress(int progresoActual, int total, int size = 26)
        {
            int percent = (progresoActual * 100) / Math.Max(total, 1);
            int percentBar = (progresoActual * size) / Math.Max(total, 1);
            string progressBar = new string('█', percentBar) + new string('░', size - percentBar);
            Console.Write($"\r {progressBar} {percent}%");
        }

        private static void ReadParams(string[] args)
        {
            if (!args.Any()) return;

            string conf = "";
            foreach (string arg in args)
            {
                if (string.IsNullOrEmpty(conf))
                {
                    conf = arg;

                    switch (conf)
                    {
                        case "/t":
                            break;
                        case "/nomin":
                            conf = "";
                            nomin = true;
                            break;
                        case "/euro":
                            conf = "";
                            euroGastado = true;
                            break;
                        case "/p":
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    switch (conf)
                    {
                        case "/t":
                            top = int.Parse(arg);
                            break;
                    }
                    switch (conf)
                    {
                        case "/p":
                            precioMin = int.Parse(arg);
                            break;
                    }

                    conf = "";
                }
            }
        }
    }
}