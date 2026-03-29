using OnceInfo.Services;

namespace OnceInfo.Tests
{
    [TestClass]
    public class RascaParserTests
    {
        #region ToDecimal Tests

        [TestMethod]
        public void ToDecimal_ConNumeroSimple_DevuelveValorCorrecto()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(1234m, RascaParser.ToDecimal("1234"));
        }

        [TestMethod]
        public void ToDecimal_ConNumeroConDecimales_DevuelveValorCorrecto()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(12.34m, RascaParser.ToDecimal("12.34"));
        }

        [TestMethod]
        public void ToDecimal_ConSimboloEuro_DevuelveValorCorrecto()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(10m, RascaParser.ToDecimal("10€"));
            Assert.AreEqual(10m, RascaParser.ToDecimal("10 €"));
        }

        [TestMethod]
        public void ToDecimal_ConSeparadorMilesEspañol_DevuelveValorCorrecto()
        {
            // Arrange & Act & Assert
            // Formato español: 1.234 (separador de miles) -> 1234
            Assert.AreEqual(1234m, RascaParser.ToDecimal("1.234"));
        }

        [TestMethod]
        public void ToDecimal_ConSeparadorDecimalComa_DevuelveValorCorrecto()
        {
            // Arrange & Act & Assert
            // Solo coma: 12,34 -> 12.34
            Assert.AreEqual(12.34m, RascaParser.ToDecimal("12,34"));
        }

        [TestMethod]
        public void ToDecimal_ConSeparadoresEspañolCompleto_DevuelveValorCorrecto()
        {
            // Arrange & Act & Assert
            // 1.234,56 -> 1234.56
            Assert.AreEqual(1234.56m, RascaParser.ToDecimal("1.234,56"));
        }

        [TestMethod]
        public void ToDecimal_ConEspacioTrasNumero_DevuelveValorCorrecto()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(100m, RascaParser.ToDecimal("100 euros"));
            Assert.AreEqual(50m, RascaParser.ToDecimal("50 €"));
        }

        [TestMethod]
        public void ToDecimal_ConCadenaVacia_DevuelveCero()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0m, RascaParser.ToDecimal(""));
            Assert.AreEqual(0m, RascaParser.ToDecimal("   "));
            Assert.AreEqual(0m, RascaParser.ToDecimal(null));
        }

        [TestMethod]
        public void ToDecimal_ConNumeroNegativo_DevuelveValorCorrecto()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(-100m, RascaParser.ToDecimal("-100"));
        }

        [TestMethod]
        public void ToDecimal_ConCaracteresInvalidos_FiltraCorrectamente()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(100m, RascaParser.ToDecimal("abc100xyz"));
        }

        #endregion

        #region CalcularPorcentajePremio Tests

        [TestMethod]
        public void CalcularPorcentajePremio_ValoresNormales_DevuelvePorcentajeCorrecto()
        {
            // Arrange: 50 premiados de 1000 series
            // Act
            var resultado = RascaParser.CalcularPorcentajePremio(50, 1000);
            // Assert: 50/1000 * 100 = 5%
            Assert.AreEqual(5m, resultado);
        }

        [TestMethod]
        public void CalcularPorcentajePremio_SeriesCero_DevuelveCero()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0m, RascaParser.CalcularPorcentajePremio(50, 0));
        }

        [TestMethod]
        public void CalcularPorcentajePremio_SinPremiados_DevuelveCero()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0m, RascaParser.CalcularPorcentajePremio(0, 1000));
        }

        [TestMethod]
        public void CalcularPorcentajePremio_TodosPremiados_DevuelveCien()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(100m, RascaParser.CalcularPorcentajePremio(1000, 1000));
        }

        #endregion

        #region CalcularEurosPorEuroGastado Tests

        [TestMethod]
        public void CalcularEurosPorEuroGastado_ValoresNormales_DevuelveValorCorrecto()
        {
            // Arrange: 1000€ en premios, precio 5€, 100 series
            // Total apostado: 5 * 100 = 500€
            // Euros por euro: 1000 / 500 = 2
            // Act
            var resultado = RascaParser.CalcularEurosPorEuroGastado(1000, 5, 100);
            // Assert
            Assert.AreEqual(2m, resultado);
        }

        [TestMethod]
        public void CalcularEurosPorEuroGastado_PrecioCero_DevuelveCero()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0m, RascaParser.CalcularEurosPorEuroGastado(1000, 0, 100));
        }

        [TestMethod]
        public void CalcularEurosPorEuroGastado_SeriesCero_DevuelveCero()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0m, RascaParser.CalcularEurosPorEuroGastado(1000, 5, 0));
        }

        [TestMethod]
        public void CalcularEurosPorEuroGastado_SinPremios_DevuelveCero()
        {
            // Arrange & Act & Assert
            Assert.AreEqual(0m, RascaParser.CalcularEurosPorEuroGastado(0, 5, 100));
        }

        #endregion

        #region ParseArguments Tests

        [TestMethod]
        public void ParseArguments_SinArgumentos_DevuelveValoresPorDefecto()
        {
            // Arrange & Act
            var (top, nomin, euroGastado, precioMin) = RascaParser.ParseArguments(Array.Empty<string>());

            // Assert
            Assert.AreEqual(0, top);
            Assert.IsFalse(nomin);
            Assert.IsFalse(euroGastado);
            Assert.AreEqual(0, precioMin);
        }

        [TestMethod]
        public void ParseArguments_ConNomin_DevuelveNominActivo()
        {
            // Arrange & Act
            var (top, nomin, euroGastado, precioMin) = RascaParser.ParseArguments(new[] { "/nomin" });

            // Assert
            Assert.AreEqual(0, top);
            Assert.IsTrue(nomin);
            Assert.IsFalse(euroGastado);
            Assert.AreEqual(0, precioMin);
        }

        [TestMethod]
        public void ParseArguments_ConEuro_DevuelveEuroGastadoActivo()
        {
            // Arrange & Act
            var (top, nomin, euroGastado, precioMin) = RascaParser.ParseArguments(new[] { "/euro" });

            // Assert
            Assert.AreEqual(0, top);
            Assert.IsFalse(nomin);
            Assert.IsTrue(euroGastado);
            Assert.AreEqual(0, precioMin);
        }

        [TestMethod]
        public void ParseArguments_ConTop_DevuelveTopCorrecto()
        {
            // Arrange & Act
            var (top, nomin, euroGastado, precioMin) = RascaParser.ParseArguments(new[] { "/t", "10" });

            // Assert
            Assert.AreEqual(10, top);
            Assert.IsFalse(nomin);
            Assert.IsFalse(euroGastado);
            Assert.AreEqual(0, precioMin);
        }

        [TestMethod]
        public void ParseArguments_ConPrecioMin_DevuelvePrecioMinCorrecto()
        {
            // Arrange & Act
            var (top, nomin, euroGastado, precioMin) = RascaParser.ParseArguments(new[] { "/p", "5" });

            // Assert
            Assert.AreEqual(0, top);
            Assert.IsFalse(nomin);
            Assert.IsFalse(euroGastado);
            Assert.AreEqual(5, precioMin);
        }

        [TestMethod]
        public void ParseArguments_ConMultiplesArgumentos_DevuelveTodosCorrectos()
        {
            // Arrange & Act
            var (top, nomin, euroGastado, precioMin) = RascaParser.ParseArguments(new[] { "/t", "5", "/nomin", "/euro", "/p", "10" });

            // Assert
            Assert.AreEqual(5, top);
            Assert.IsTrue(nomin);
            Assert.IsTrue(euroGastado);
            Assert.AreEqual(10, precioMin);
        }

        [TestMethod]
        public void ParseArguments_ConNull_DevuelveValoresPorDefecto()
        {
            // Arrange & Act
            var (top, nomin, euroGastado, precioMin) = RascaParser.ParseArguments(null!);

            // Assert
            Assert.AreEqual(0, top);
            Assert.IsFalse(nomin);
            Assert.IsFalse(euroGastado);
            Assert.AreEqual(0, precioMin);
        }

        #endregion
    }
}