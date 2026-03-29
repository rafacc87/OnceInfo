using OnceInfo.Models;
using OnceInfo.Services;
using System.Globalization;

namespace OnceInfo.Tests
{
    [TestClass]
    public class HtmlReportGeneratorTests
    {
        [TestMethod]
        public void GenerateReportHtml_ConListaVacia_GeneraHtmlValido()
        {
            // Arrange
            var listaVacia = new List<RascaResultado>();

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(listaVacia, listaVacia, 0);

            // Assert
            Assert.IsTrue(html.Contains("<!doctype html>"));
            Assert.IsTrue(html.Contains("<html lang=\"es\">"));
            Assert.IsTrue(html.Contains("</html>"));
        }

        [TestMethod]
        public void GenerateReportHtml_ConResultados_GeneraTablaConDatos()
        {
            // Arrange
            var resultados = new List<RascaResultado>
            {
                new RascaResultado
                {
                    Nombre = "Rasca Test",
                    Serie = "100000",
                    Precio = "5",
                    RascasPremiados = 500,
                    PorcentajePremio = 12.5m
                }
            };

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(resultados, resultados, 0);

            // Assert
            Assert.IsTrue(html.Contains("Rasca Test"));
            Assert.IsTrue(html.Contains("100000"));
            Assert.IsTrue(html.Contains("500"));
            Assert.IsTrue(html.Contains("12.5"));
        }

        [TestMethod]
        public void GenerateReportHtml_EscapaCaracteresHtml_PrevieneXSS()
        {
            // Arrange
            var resultados = new List<RascaResultado>
            {
                new RascaResultado
                {
                    Nombre = "<script>alert('xss')</script>",
                    Serie = "100",
                    Precio = "1",
                    RascasPremiados = 1,
                    PorcentajePremio = 1m
                }
            };

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(resultados, resultados, 0);

            // Assert
            Assert.IsTrue(html.Contains("&lt;script&gt;"));
            Assert.IsFalse(html.Contains("<script>alert"));
        }

        [TestMethod]
        public void GenerateReportHtml_ConPrecioMin_IncluyeEnTitulo()
        {
            // Arrange
            var resultados = new List<RascaResultado>();

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(resultados, resultados, 5);

            // Assert
            Assert.IsTrue(html.Contains("Premios a partir de 5"));
        }

        [TestMethod]
        public void GenerateReportHtml_ConPrecioMinMayor10_OcultaSelectDeModo()
        {
            // Arrange
            var resultados = new List<RascaResultado>();

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(resultados, resultados, 15);

            // Assert
            Assert.IsFalse(html.Contains("<select id=\"modeSelect\">"));
        }

        [TestMethod]
        public void GenerateReportHtml_ConPrecioMinMenor10_MuestraSelectDeModo()
        {
            // Arrange
            var resultados = new List<RascaResultado>();

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(resultados, resultados, 5);

            // Assert
            Assert.IsTrue(html.Contains("<select id=\"modeSelect\">"));
        }

        [TestMethod]
        public void GenerateReportHtml_OrdenaPorPorcentajeDescendente()
        {
            // Arrange
            var resultados = new List<RascaResultado>
            {
                new RascaResultado { Nombre = "Bajo", Serie = "100", Precio = "1", RascasPremiados = 1, PorcentajePremio = 1m },
                new RascaResultado { Nombre = "Alto", Serie = "100", Precio = "1", RascasPremiados = 99, PorcentajePremio = 99m },
                new RascaResultado { Nombre = "Medio", Serie = "100", Precio = "1", RascasPremiados = 50, PorcentajePremio = 50m }
            };

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(resultados, resultados, 0);

            // Assert - Verificar que "Alto" aparece antes que "Medio" y "Medio" antes que "Bajo"
            var posicionAlto = html.IndexOf("Alto");
            var posicionMedio = html.IndexOf("Medio");
            var posicionBajo = html.IndexOf("Bajo");

            Assert.IsTrue(posicionAlto < posicionMedio, "Alto debe aparecer antes que Medio");
            Assert.IsTrue(posicionMedio < posicionBajo, "Medio debe aparecer antes que Bajo");
        }

        [TestMethod]
        public void GenerateReportHtml_IncluyeJavaScriptDeFiltros()
        {
            // Arrange
            var resultados = new List<RascaResultado>();

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(resultados, resultados, 0);

            // Assert
            Assert.IsTrue(html.Contains("<script>"));
            Assert.IsTrue(html.Contains("applyFilters"));
            Assert.IsTrue(html.Contains("</script>"));
        }

        [TestMethod]
        public void GenerateReportHtml_WithListasVacias_NoLanzaExcepcion()
        {
            // Arrange
            var listaVacia = new List<RascaResultado>();

            // Act
            var html = HtmlReportGenerator.GenerateReportHtml(listaVacia, listaVacia, 0);

            // Assert
            Assert.IsTrue(html.Contains("<!doctype html>"));
        }
    }
}