using HtmlAgilityPack;
using OnceInfo.Models;

namespace OnceInfo
{
    internal class Program
    {
        // Configuraciones
        private static bool nomin = false;
        private static int top = 0;
        private static bool euroGastado = false; // Euro gastado o papeleta comprada

        private static string urlOnce = "https://www.juegosonce.es";
        private static string urlBaseRascas = urlOnce + "/rascas-todos";
        private static string nodoListadoRascas = "//*[@id=\"lstRascasTodos\"]/li/a";
        private static string nodoPrecioListadoRascas = "./span[@class='precio']";

        private static string nodoNombreRasca = "//h3[@class='ocu']";
        private static string nodosPRasca = "//section[@class=\"cont infojuego on\"]/div[@class=\"intcont\"]/p";
        private static string textoP = "Premios por cada serie de boletos de ";
        private static string textoMultiP = " con precio";
        private static string nodosListadoRasca = "//section[@class=\"cont infojuego on\"]/div[@class=\"intcont\"]/ul[@class='premiosrascas']";
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
            foreach (var link in lstRascasTodos)
            {
                Thread.Sleep(500);

                string precio = link.SelectSingleNode(nodoPrecioListadoRascas).GetAttributeValue("data-precio", string.Empty);
                resultados.AddRange(GetRascaResultado(link.GetAttributeValue("href", string.Empty), precio));
            }

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

                Console.WriteLine("Aquí tienes todos los resultados {tipoProbabilidad}:");
            }

            int i = 0;
            foreach(var r in resultados)
            {
                Console.WriteLine($"{++i} > {r.Nombre} con precio {r.Precio} euros tiene una probabilidad de {decimal.Round(r.PorcentajePremio, 2, MidpointRounding.AwayFromZero)}");
            }
        }

        private static List<RascaResultado> GetRascaResultado(string urlRasca, string precio)
        {
            List<RascaResultado> list = new();
            HtmlDocument doc = GetHtml(urlOnce + urlRasca);

            string nombre = doc.DocumentNode.SelectSingleNode(nodoNombreRasca).InnerText;
            HtmlNodeCollection pRasca = doc.DocumentNode.SelectNodes(nodosPRasca);
            HtmlNodeCollection listadoRasca = doc.DocumentNode.SelectNodes(nodosListadoRasca);

            // Buscar parrafo de premio
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

                    int rascasPremiados = 0;
                    foreach (var premio in premios)
                    {
                        // Descarta los premios del mismo valor de coste del rasca
                        if (nomin)
                        {
                            string premioDe = premio.SelectSingleNode("./span").InnerHtml;
                            if (premioDe.Contains(precios[i] + " €")) break;
                        }
                        string texto = premio.InnerHtml;
                        texto = texto.Substring(0, texto.IndexOf(" ")).Replace(".", "");
                        rascasPremiados += int.Parse(texto);
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
                        rasca.PorcentajePremio = rascasPremiados * 100 / (decimal.Parse(precios[i].Replace(".", "")) * decimal.Parse(serie.Replace(".", "")));
                    }
                    else
                    {
                        rasca.PorcentajePremio = rascasPremiados * 100 / decimal.Parse(serie.Replace(".", ""));
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

                    conf = "";
                }
            }
        }
    }
}