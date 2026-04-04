using HtmlAgilityPack;
using OnceInfo.Models;
using OnceInfo.Properties;
using OnceInfo.Services;
using System.Diagnostics;

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
            // Check for debug mode
            if (args.Contains("/debug"))
            {
                Console.WriteLine(">> Modo DEBUG activado - Generando datos de prueba...");
                GenerateDebugReport();
                return;
            }

            PlaywrightService.EnsurePlaywrightBrowsers();

            (top, nomin, euroGastado, precioMin) = RascaParser.ParseArguments(args);

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

                            decimal premioDeDec = RascaParser.ToDecimal(premioDe);
                            decimal precioI = RascaParser.ToDecimal(precios[i]);

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

                    var priceDecimal = RascaParser.ToDecimal(precios[i]);
                    var serieDecimal = RascaParser.ToDecimal(serie);

                    if (euroGastado)
                    {
                        rasca.PorcentajePremio = RascaParser.CalcularEurosPorEuroGastado(totalPremios, priceDecimal, serieDecimal);
                    }
                    else
                    {
                        rasca.PorcentajePremio = RascaParser.CalcularPorcentajePremio(rascasPremiados, serieDecimal);
                    }

                    list.Add(rasca);
                    i++;
                }
            }

            return list;
        }

        private static void GenerateDebugReport()
        {
            var random = new Random();
            var nombresRascas = new[]
            {
                "Rasca Classic", "Rasca Premium", "Rasca Gold", "Rasca Platinum",
                "Rasca Fortune", "Rasca Lucky", "Rasca Million", "Rasca Deluxe",
                "Rasca Royal", "Rasca Star", "Rasca Diamond", "Rasca Emerald",
                "Rasca Ruby", "Rasca Sapphire", "Rasca Bronze", "Rasca Silver"
            };

            var precios = new[] { "1", "2", "3", "5", "10","0.5", "0.25" };
            var series = new[] { "50.000", "100.000", "200.000", "500.000", "1.000.000" };

            var resultadosCon = new List<RascaResultado>();
            var resultadosSin = new List<RascaResultado>();

            foreach (var nombre in nombresRascas)
            {
                foreach (var precio in precios)
                {
                    var precioDecimal = decimal.Parse(precio);
                    var serie = series[random.Next(series.Length)];
                    var serieDecimal = decimal.Parse(serie.Replace(".", "").Replace(",", ""));

                    // Simular rascas premiados (entre 100 y 50000)
                    int rascasPremiados = random.Next(100, 50001);

                    // Simular porcentaje de premio (entre 0.5 y 50 para que haya variedad)
                    decimal porcentajeBase = (decimal)(random.NextDouble() * 49.5 + 0.5);
                    decimal porcentaje = Math.Round(porcentajeBase, 2);

                    var resultado = new RascaResultado
                    {
                        Nombre = nombre,
                        Serie = serie,
                        Precio = precio + "€",
                        RascasPremiados = rascasPremiados,
                        PorcentajePremio = porcentaje
                    };

                    resultadosCon.Add(resultado);

                    // Versión "sin mismo valor" con porcentaje ligeramente diferente
                    var resultadoSin = new RascaResultado
                    {
                        Nombre = nombre,
                        Serie = serie,
                        Precio = precio + "€",
                        RascasPremiados = rascasPremiados - random.Next(50, 500),
                        PorcentajePremio = Math.Round(porcentaje * (decimal)0.95, 2)
                    };
                    resultadosSin.Add(resultadoSin);
                }
            }

            // Mezclar para que no estén ordenados
            resultadosCon = resultadosCon.OrderBy(x => random.Next()).Take(60).ToList();
            resultadosSin = resultadosSin.OrderBy(x => random.Next()).Take(60).ToList();

            Console.WriteLine($"> Generando informe con {resultadosCon.Count} registros (con mismo valor)");
            Console.WriteLine($"> Generando informe con {resultadosSin.Count} registros (sin mismo valor)");

            // Genera informe HTML
            var html = HtmlReportGenerator.GenerateReportHtml(resultadosCon, resultadosSin, precioMin);
            var outPath = Path.Combine(Environment.CurrentDirectory, "onceinfo-report-debug.html");
            File.WriteAllText(outPath, html, System.Text.Encoding.UTF8);

            Console.WriteLine($"> Informe guardado: {outPath}");
            Console.WriteLine("> Abriendo en navegador...");

            Process.Start(new ProcessStartInfo
            {
                FileName = outPath,
                UseShellExecute = true
            });

            Console.WriteLine("> Presiona cualquier tecla para salir...");
            Console.ReadKey();
        }

        private static void ShowProgress(int progresoActual, int total, int size = 26)
        {
            int percent = (progresoActual * 100) / Math.Max(total, 1);
            int percentBar = (progresoActual * size) / Math.Max(total, 1);
            string progressBar = new string('█', percentBar) + new string('░', size - percentBar);
            Console.Write($"\r {progressBar} {percent}%");
        }
    }
}