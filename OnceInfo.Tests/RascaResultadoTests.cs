using OnceInfo.Models;

namespace OnceInfo.Tests
{
    [TestClass]
    public class RascaResultadoTests
    {
        [TestMethod]
        public void RascaResultado_CrearInstancia_ValoresPorDefectoCorrectos()
        {
            // Arrange & Act
            var resultado = new RascaResultado();

            // Assert
            Assert.AreEqual(string.Empty, resultado.Nombre);
            Assert.AreEqual(string.Empty, resultado.Serie);
            Assert.AreEqual(string.Empty, resultado.Precio);
            Assert.AreEqual(0, resultado.RascasPremiados);
            Assert.AreEqual(0m, resultado.PorcentajePremio);
        }

        [TestMethod]
        public void RascaResultado_AsignarPropiedades_ValoresCorrectos()
        {
            // Arrange & Act
            var resultado = new RascaResultado
            {
                Nombre = "Rasca Ejemplo",
                Serie = "100000",
                Precio = "5",
                RascasPremiados = 500,
                PorcentajePremio = 12.5m
            };

            // Assert
            Assert.AreEqual("Rasca Ejemplo", resultado.Nombre);
            Assert.AreEqual("100000", resultado.Serie);
            Assert.AreEqual("5", resultado.Precio);
            Assert.AreEqual(500, resultado.RascasPremiados);
            Assert.AreEqual(12.5m, resultado.PorcentajePremio);
        }

        [TestMethod]
        public void RascaResultado_ConPrecioConFormato_GuardaCorrectamente()
        {
            // Arrange & Act
            var resultado = new RascaResultado
            {
                Precio = "1 - 3"
            };

            // Assert
            Assert.AreEqual("1 - 3", resultado.Precio);
        }
    }
}