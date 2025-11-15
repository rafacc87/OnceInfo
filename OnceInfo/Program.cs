using HtmlAgilityPack;
using OnceInfo.Models;

namespace OnceInfo
{
    internal class Program
    {
        // Configuraciones
        private static bool nomin = false;
        private static int top = 0;
        private static int precioMin = 0;
        private static bool euroGastado = false; // Euro gastado o papeleta comprada

        private static string urlOnce = "https://www.juegosonce.es";
        private static string urlBaseRascas = urlOnce + "/rascas-todos";
        private static string nodoListadoRascas = "//*[@id=\"lstRascasTodos\"]/li/a";
        private static string nodoPrecioListadoRascas = "./span[@class='precio']";

        private static string nodoNombreRasca = "//header/div/div/h2";
        private static string nodosPRasca = "//div[@class=\"contenido\"]/h3";
        private static string textoP = "Premios por cada serie de boletos de ";
        private static string textoMultiP = " con precio";
        private static string nodosListadoRasca = "//ul[@class='premiosrascas']";
        private static string nodosPremio = "./li";

        static void Main(string[] args)
        {
            ReadParams(args);

            Console.WriteLine("¡Bienvenido a OnceInfo!");
            Console.WriteLine();

            Console.WriteLine("- Obteniendo listado de rascas:");
            HtmlDocument doc = GetHtml(urlBaseRascas);
            if (doc.GetElementbyId("msgerror") != null)
            {
                Console.WriteLine($">> No está disponible ahora mismo juegosonce <<");
                return;
            }
            HtmlNodeCollection lstRascasTodos = doc.DocumentNode.SelectNodes(nodoListadoRascas);
            Console.WriteLine($"> Encontrados { lstRascasTodos.Count } rascas.");

            Console.WriteLine();
            Console.WriteLine("- Analizanzo rascas:");

            List<RascaResultado> resultados = new();
            int total = lstRascasTodos.Count();
            for (int p = 0; p < total; p++)
            {
                Thread.Sleep(500);

                ShowProgress(p, total);
                string precio = lstRascasTodos[p].SelectSingleNode(nodoPrecioListadoRascas).GetAttributeValue("data-precio", string.Empty);
                resultados.AddRange(GetRascaResultado(lstRascasTodos[p].GetAttributeValue("href", string.Empty), precio));
            }
            ShowProgress(total, total);

            Console.WriteLine();
            Console.WriteLine("> Completado");

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
            foreach(var r in resultados)
            {
                string inicio = $"{++i} > {r.Nombre} con precio {r.Precio} euros tiene";
                decimal porcentaje = decimal.Round(r.PorcentajePremio, 2, MidpointRounding.AwayFromZero);

                if (euroGastado)
                    Console.WriteLine($"{inicio} un promedio de {porcentaje} euros en premios por euro gastado");
                else
                    Console.WriteLine($"{inicio} una probabilidad de {porcentaje}%");
            }
            Console.ReadKey();
        }

        private static void ShowProgress(int progresoActual, int total, int size = 26)
        {
            int percent = (progresoActual * 100) / total;
            int percentBar = (progresoActual * size) / total;
            string progressBar = new string('█', percentBar) + new string('░', size - percentBar);
            Console.Write($"\r {progressBar} {percent}%");
        }

        private static List<RascaResultado> GetRascaResultado(string urlRasca, string precio)
        {
            List<RascaResultado> list = new();
            HtmlDocument doc = GetHtml(urlOnce + urlRasca);

            string nombre = doc.DocumentNode.SelectSingleNode(nodoNombreRasca).InnerText;
            HtmlNodeCollection pRasca = doc.DocumentNode.SelectNodes(nodosPRasca);
            HtmlNodeCollection listadoRasca = doc.DocumentNode.SelectNodes(nodosListadoRasca);

            // Buscar párrafo de premio
            int i = 0;
            string[] precios = precio.Split(" - ");
            foreach (var p in pRasca)
            {
                string serie = p.InnerHtml;
                if (serie.StartsWith(textoP))
                {
                    serie = serie[(textoP.Length)..].Replace(":", "");
                    if (serie.Contains(textoMultiP))
                    {
                        serie = serie[..serie.IndexOf(" ")];
                    }

                    HtmlNodeCollection premios = listadoRasca[i].SelectNodes(nodosPremio);

                    decimal totalPremios = 0;
                    int rascasPremiados = 0;
                    foreach (var premio in premios)
                    {
                        string texto = premio.InnerHtml;
                        texto = texto.Substring(0, texto.IndexOf(" ")).Replace(".", "");

                        string premioDe = premio.SelectSingleNode("./span").InnerHtml;
                        // Limpiar premioDe
                        {
                            int indiceEspacio = premioDe.IndexOf(" ");
                            if (indiceEspacio != -1)
                            {
                                string subcadena = premioDe[..indiceEspacio];
                                premioDe = subcadena.Replace(".", "");
                            }
                            else
                            {
                                premioDe = premioDe.Replace("€", "").Trim();
                            }
                        }
                        // Descarta los premios del mismo valor de coste del rasca
                        // Descarta los premios inferiores a precioMin
                        if (nomin && decimal.Parse(premioDe) == decimal.Parse(precios[i]) || 
                            precioMin > decimal.Parse(premioDe))
                        {
                            break;
                        }

                        rascasPremiados += int.Parse(texto);
                        totalPremios += decimal.Parse(premioDe) * decimal.Parse(texto);
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
                        rasca.PorcentajePremio = totalPremios / (decimal.Parse(precios[i].Replace(".", "")) * decimal.Parse(serie.Replace(".", "")));
                    }
                    else
                    {
                        rasca.PorcentajePremio = (rascasPremiados / decimal.Parse(serie.Replace(".", ""))) * 100;
                    }

                    list.Add(rasca);
                    i++;
                }
            }

            return list;
        }

        private static HtmlDocument GetHtml(string url)
        {
            HtmlWeb web = new();
            return web.Load(url);
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

                    switch(conf)
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