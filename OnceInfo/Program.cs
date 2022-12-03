using HtmlAgilityPack;
using OnceInfo.Models;

namespace OnceInfo
{
    internal class Program
    {
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
            resultados = resultados.OrderByDescending(x => x.PorcentajePremio).Take(10).ToList();

            Console.WriteLine();
            Console.WriteLine("> Completado");

            Console.WriteLine("Aquí tienes los resultados:");
            foreach(var r in resultados)
            {
                Console.WriteLine($"> {r.Nombre} con precio {r.Precio}€ tiene una probabilidad de {decimal.Round(r.PorcentajePremio, 2, MidpointRounding.AwayFromZero)}");
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
                        string texto = premio.InnerHtml;
                        texto = texto.Substring(0, texto.IndexOf(" ")).Replace(".", "");
                        rascasPremiados += int.Parse(texto);
                    }

                    list.Add(new RascaResultado() { 
                        Nombre = nombre, 
                        Serie = serie, 
                        Precio = precios[i], 
                        RascasPremiados = rascasPremiados, 
                        PorcentajePremio = rascasPremiados * 100 / decimal.Parse(serie.Replace(".", ""))
                    });
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
    }
}