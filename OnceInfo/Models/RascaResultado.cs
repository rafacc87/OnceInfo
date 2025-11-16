using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnceInfo.Models
{
    public class RascaResultado
    {
        public string Nombre { get; set; } = "";

        public string Serie { get; set; } = "";

        public string Precio { get; set; } = "";

        public int RascasPremiados { get; set; }

        public decimal PorcentajePremio { get; set; }
    }
}
