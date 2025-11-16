using System.Globalization;
using System.Net;
using System.Text;
using OnceInfo.Models;

namespace OnceInfo.Services
{
    public static class HtmlReportGenerator
    {
        public static string GenerateReportHtml(List<RascaResultado> withSameValue, List<RascaResultado> withoutSameValue, int precioMinDefault = 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"es\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
            sb.AppendLine("<title>OnceInfo - Informe (" + precioMinDefault.ToString() + ")</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body{font-family:Segoe UI, Roboto, Arial; padding:18px; background:#f5f6fb; color:#222}");
            sb.AppendLine(".controls{margin-bottom:12px;display:flex;gap:12px;flex-wrap:wrap}");
            sb.AppendLine("table{border-collapse:collapse;width:100%;background:#fff;border:1px solid #ddd}");
            sb.AppendLine("th,td{padding:8px;border:1px solid #e6e6e6;text-align:left}");
            sb.AppendLine("th{background:#fafafa;position:sticky;top:0;z-index:2}");
            sb.AppendLine(".muted{color:#666;font-size:0.9em}");
            sb.AppendLine(".badge{display:inline-block;padding:2px 6px;border-radius:4px;background:#e8eef8;color:#1a56db;font-weight:600;font-size:0.85em}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("<h2>OnceInfo — Resultados (Premios a partir de " + precioMinDefault.ToString() + "€)</h2>");
            sb.AppendLine("<div class=\"controls\">");
            if (precioMinDefault <= 10)
            {
                sb.AppendLine("<label>Modo: <select id=\"modeSelect\">");
                sb.AppendLine("<option value=\"con\">Todos</option>");
                sb.AppendLine("<option value=\"sin\">Descartar mismo valor</option>");
                sb.AppendLine("</select></label>");
            }
            sb.AppendLine("<label>Precio: <input id=\"precioMin\" type=\"number\" value=\"0\" min=\"0\" max=\"10\" style=\"width:35px\"></label>");
            sb.AppendLine("<label>-<input id=\"precioMax\" type=\"number\" value=\"10\" min=\"1\" max=\"10\" style=\"width:35px; margin-left: 12px\"></label>");
            sb.AppendLine("<label>Buscar: <input id=\"search\" type=\"search\" placeholder=\"nombre, serie...\" style=\"width:200px\"></label>");
            sb.AppendLine("<button id=\"clear\">Limpiar filtros</button>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div style=\"overflow:auto;max-height:70vh\">");
            sb.AppendLine("<table id=\"results\">");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th>#</th><th>Nombre</th><th>Serie</th><th>Precio</th><th>Rascas Premiados</th><th>Porcentaje / €</th>");//<th>Modo</th>
            sb.AppendLine("</tr></thead>");
            sb.AppendLine("<tbody>");

            void WriteRows(IEnumerable<RascaResultado> list, string mode)
            {
                int idx = 0;
                foreach (var r in list)
                {
                    idx++;
                    var precio = (r.Precio ?? "").Replace(",", ".").Replace("€", "").Trim();
                    string precioEsc = WebUtility.HtmlEncode(r.Precio);
                    string nombreEsc = WebUtility.HtmlEncode(r.Nombre);
                    string serieEsc = WebUtility.HtmlEncode(r.Serie);
                    string porcent = r.PorcentajePremio.ToString("0.##", CultureInfo.InvariantCulture);
                    sb.AppendLine($"<tr data-mode=\"{mode}\" data-precio=\"{precio}\">");
                    sb.AppendLine($"<td>{idx}</td>");
                    //sb.AppendLine($"<td><span class=\"badge\">{(mode == "con" ? "Con" : "Sin")}</span></td>");
                    sb.AppendLine($"<td>{nombreEsc}</td>");
                    sb.AppendLine($"<td>{serieEsc}</td>");
                    sb.AppendLine($"<td>{precioEsc}</td>");
                    sb.AppendLine($"<td>{r.RascasPremiados}</td>");
                    sb.AppendLine($"<td>{porcent}</td>");
                    sb.AppendLine("</tr>");
                }
            }

            if (precioMinDefault <= 10)
                WriteRows(withSameValue.OrderByDescending(x => x.PorcentajePremio).ToList() ?? new List<RascaResultado>(), "con");
            WriteRows(withoutSameValue.OrderByDescending(x => x.PorcentajePremio).ToList() ?? new List<RascaResultado>(), "sin");

            sb.AppendLine("</tbody></table></div>");

            sb.AppendLine("<script>");
            if (precioMinDefault <= 10)
                sb.AppendLine("const modeSelect = document.getElementById('modeSelect');");
            sb.AppendLine("const precioMin = document.getElementById('precioMin');");
            sb.AppendLine("const precioMax = document.getElementById('precioMax');");
            sb.AppendLine("const search = document.getElementById('search');");
            sb.AppendLine("const clear = document.getElementById('clear');");
            sb.AppendLine("const rows = Array.from(document.querySelectorAll('#results tbody tr'));");
            sb.AppendLine("function applyFilters(){");
            if (precioMinDefault <= 10)
                sb.AppendLine("  const sel = modeSelect.value;"); // 'con' | 'sin'
            sb.AppendLine("  const min = parseFloat(precioMin.value || 0);");
            sb.AppendLine("  const max = parseFloat(precioMax.value || 10);");
            sb.AppendLine("  const q = (search.value||'').toLowerCase();");
            sb.AppendLine("  rows.forEach(r=>{");
            sb.AppendLine("    const rowMode = r.dataset.mode;");
            sb.AppendLine("    const precio = parseFloat(r.dataset.precio||'0');");
            sb.AppendLine("    const text = r.innerText.toLowerCase();");
            sb.AppendLine("    let visible = true;");
            if (precioMinDefault <= 10)
            {
                sb.AppendLine("    if(sel === 'con' && rowMode !== 'con') visible = false;");
                sb.AppendLine("    if(sel === 'sin' && rowMode !== 'sin') visible = false;");
            }
            sb.AppendLine("    if(precio < min || precio > max) visible = false;");
            sb.AppendLine("    if(q && text.indexOf(q) === -1) visible = false;");
            sb.AppendLine("    r.style.display = visible ? '' : 'none';");
            sb.AppendLine("  });");
            sb.AppendLine("}");
            if (precioMinDefault <= 10)
                sb.AppendLine("modeSelect.addEventListener('change', applyFilters);");
            sb.AppendLine("precioMin.addEventListener('input', applyFilters);");
            sb.AppendLine("precioMax.addEventListener('input', applyFilters);");
            sb.AppendLine("search.addEventListener('input', applyFilters);");
            sb.AppendLine("clear.addEventListener('click', ()=>{ " +
                (precioMinDefault <= 10 ? "modeSelect.value='sin'; " : "") +
                "precioMin.value='0'; precioMax.value='10'; search.value=''; applyFilters(); });");
            sb.AppendLine("applyFilters();");
            sb.AppendLine("</script>");

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }
    }
}