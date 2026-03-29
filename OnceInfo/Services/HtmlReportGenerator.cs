using System.Globalization;
using System.Net;
using System.Text;
using OnceInfo.Models;

namespace OnceInfo.Services
{
    public static class HtmlReportGenerator
    {
        private static string EscapeAttribute(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Replace("&", "&amp;")
                         .Replace("\"", "&quot;")
                         .Replace("'", "&#39;")
                         .Replace("<", "&lt;")
                         .Replace(">", "&gt;");
        }

        private static (decimal q1, decimal q2, decimal q3) CalculateQuartiles(List<decimal> values)
        {
            if (values == null || values.Count == 0)
                return (0, 0, 0);

            var sorted = values.OrderBy(v => v).ToList();
            int n = sorted.Count;

            decimal GetPercentile(int p)
            {
                if (n == 1) return sorted[0];
                double index = (p / 100.0) * (n - 1);
                int lower = (int)Math.Floor(index);
                int upper = (int)Math.Ceiling(index);
                if (lower == upper) return sorted[lower];
                return sorted[lower] + (sorted[upper] - sorted[lower]) * (decimal)(index - lower);
            }

            return (GetPercentile(25), GetPercentile(50), GetPercentile(75));
        }

        public static string GenerateReportHtml(List<RascaResultado> withSameValue, List<RascaResultado> withoutSameValue, int precioMinDefault = 0)
        {
            var allResults = (withSameValue ?? new List<RascaResultado>())
                .Concat(withoutSameValue ?? new List<RascaResultado>())
                .ToList();

            var uniquePrices = allResults
                .Select(r => r.Precio?.Replace("€", "").Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .Distinct()
                .OrderBy(p => decimal.TryParse(p?.Replace(",", "."), out decimal val) ? val : 0)
                .ToList();

            var percentages = allResults.Select(r => r.PorcentajePremio).ToList();
            var (q1, q2, q3) = CalculateQuartiles(percentages);

            var rascasPremiados = allResults.Select(r => (decimal)r.RascasPremiados).ToList();
            var (rascasQ1, rascasQ2, rascasQ3) = CalculateQuartiles(rascasPremiados);

            var sb = new StringBuilder();
            sb.AppendLine("<!doctype html>");
            sb.AppendLine("<html lang=\"es\">");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset=\"utf-8\">");
            sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\">");
            sb.AppendLine("<title>OnceInfo - Informe de Rascas</title>");
            sb.AppendLine("<style>");

            sb.AppendLine(":root {");
            sb.AppendLine("  --primary: #6366f1;");
            sb.AppendLine("  --primary-dark: #4f46e5;");
            sb.AppendLine("  --primary-light: #a5b4fc;");
            sb.AppendLine("  --secondary: #10b981;");
            sb.AppendLine("  --bg-main: #0f172a;");
            sb.AppendLine("  --bg-card: #1e293b;");
            sb.AppendLine("  --bg-table: #334155;");
            sb.AppendLine("  --text-primary: #f1f5f9;");
            sb.AppendLine("  --text-secondary: #94a3b8;");
            sb.AppendLine("  --border: #475569;");
            sb.AppendLine("  --shadow: 0 10px 40px rgba(0,0,0,0.4);");
            sb.AppendLine("}");
            sb.AppendLine("* { margin: 0; padding: 0; box-sizing: border-box; }");
            sb.AppendLine("html, body { height: 100%; overflow: hidden; }");
            sb.AppendLine("body {");
            sb.AppendLine("  font-family: 'Inter', 'Segoe UI', system-ui, sans-serif;");
            sb.AppendLine("  background: var(--bg-main);");
            sb.AppendLine("  color: var(--text-primary);");
            sb.AppendLine("  display: flex;");
            sb.AppendLine("  flex-direction: column;");
            sb.AppendLine("  padding: 12px;");
            sb.AppendLine("}");

            sb.AppendLine(".header { text-align: center; margin-bottom: 12px; flex-shrink: 0; }");
            sb.AppendLine(".header h1 { font-size: 1.75rem; font-weight: 800;");
            sb.AppendLine("  background: linear-gradient(135deg, var(--primary-light), var(--secondary));");
            sb.AppendLine("  -webkit-background-clip: text; -webkit-text-fill-color: transparent; background-clip: text; }");
            sb.AppendLine(".header .subtitle { color: var(--text-secondary); font-size: 0.85rem; margin-top: 2px; }");

            sb.AppendLine(".stats-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 8px; margin-bottom: 12px; flex-shrink: 0; }");
            sb.AppendLine(".stat-card { background: var(--bg-card); border-radius: 8px; padding: 10px 14px; border: 1px solid var(--border); }");
            sb.AppendLine(".stat-card .stat-value { font-size: 1.25rem; font-weight: 700; color: var(--primary-light); }");
            sb.AppendLine(".stat-card .stat-label { font-size: 0.7rem; color: var(--text-secondary); }");

            sb.AppendLine(".accordion { background: var(--bg-card); border-radius: 10px; border: 1px solid var(--border); margin-bottom: 12px; flex-shrink: 0; }");
            sb.AppendLine(".accordion-header { display: flex; align-items: center; justify-content: space-between; padding: 10px 14px; cursor: pointer; user-select: none; }");
            sb.AppendLine(".accordion-header h2 { font-size: 0.95rem; font-weight: 600; }");
            sb.AppendLine(".accordion-arrow { transition: transform 0.2s; font-size: 0.8rem; }");
            sb.AppendLine(".accordion.collapsed .accordion-arrow { transform: rotate(-90deg); }");
            sb.AppendLine(".accordion-content { padding: 0 14px 14px; }");
            sb.AppendLine(".accordion.collapsed .accordion-content { display: none; }");

            sb.AppendLine(".filters-row { display: flex; gap: 10px; align-items: flex-end; flex-wrap: wrap; }");
            sb.AppendLine(".filter-group { display: flex; flex-direction: column; gap: 4px; }");
            sb.AppendLine(".filter-group.flex-1 { flex: 1; min-width: 120px; }");
            sb.AppendLine(".filter-group label { font-size: 0.7rem; color: var(--text-secondary); text-transform: uppercase; letter-spacing: 0.05em; }");
            sb.AppendLine(".filter-group input, .filter-group select {");
            sb.AppendLine("  background: var(--bg-main); border: 1px solid var(--border); border-radius: 6px;");
            sb.AppendLine("  padding: 7px 10px; color: var(--text-primary); font-size: 0.85rem; transition: border-color 0.2s; }");
            sb.AppendLine(".filter-group input:focus, .filter-group select:focus { outline: none; border-color: var(--primary); }");
            sb.AppendLine(".filter-group input::placeholder { color: var(--text-secondary); }");

            sb.AppendLine(".multiselect-container { position: relative; min-width: 100px; }");
            sb.AppendLine(".multiselect-trigger {");
            sb.AppendLine("  background: var(--bg-main); border: 1px solid var(--border); border-radius: 6px;");
            sb.AppendLine("  padding: 7px 10px; color: var(--text-primary); cursor: pointer;");
            sb.AppendLine("  display: flex; justify-content: space-between; align-items: center; gap: 6px; font-size: 0.85rem; }");
            sb.AppendLine(".multiselect-trigger:hover { border-color: var(--primary); }");
            sb.AppendLine(".multiselect-trigger .selected-text { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }");
            sb.AppendLine(".multiselect-dropdown {");
            sb.AppendLine("  position: absolute; top: 100%; left: 0; right: 0; background: var(--bg-card);");
            sb.AppendLine("  border: 1px solid var(--border); border-radius: 6px; margin-top: 4px;");
            sb.AppendLine("  max-height: 180px; overflow-y: auto; z-index: 100; display: none; box-shadow: var(--shadow); }");
            sb.AppendLine(".multiselect-dropdown.open { display: block; }");
            sb.AppendLine(".multiselect-option { display: flex; align-items: center; gap: 6px; padding: 6px 10px; cursor: pointer; }");
            sb.AppendLine(".multiselect-option:hover { background: var(--bg-table); }");
            sb.AppendLine(".multiselect-option input { width: 14px; height: 14px; accent-color: var(--primary); }");
            sb.AppendLine(".multiselect-option label { cursor: pointer; text-transform: none; font-size: 0.8rem; color: var(--text-primary); }");

            sb.AppendLine(".info-tooltip { position: relative; display: inline-flex; align-items: center; cursor: help; margin-left: 2px; font-size: 0.8rem; }");
            sb.AppendLine(".info-tooltip::before {");
            sb.AppendLine("  content: attr(data-tooltip); position: absolute; bottom: 100%; left: 50%; transform: translateX(-50%);");
            sb.AppendLine("  background: var(--bg-table); color: var(--text-primary); padding: 6px 10px; border-radius: 6px;");
            sb.AppendLine("  font-size: 0.75rem; white-space: nowrap; opacity: 0; pointer-events: none; transition: opacity 0.2s;");
            sb.AppendLine("  z-index: 200; margin-bottom: 6px; box-shadow: var(--shadow); }");
            sb.AppendLine(".info-tooltip:hover::before { opacity: 1; }");

            sb.AppendLine(".btn { background: var(--primary); color: white; border: none; border-radius: 6px;");
            sb.AppendLine("  padding: 7px 14px; font-size: 0.85rem; font-weight: 500; cursor: pointer; transition: background 0.2s; white-space: nowrap; }");
            sb.AppendLine(".btn:hover { background: var(--primary-dark); }");
            sb.AppendLine(".btn-secondary { background: transparent; border: 1px solid var(--border); color: var(--text-primary); }");
            sb.AppendLine(".btn-secondary:hover { background: var(--bg-table); }");
            sb.AppendLine(".btn-icon { padding: 7px 10px; display: flex; align-items: center; justify-content: center; }");

            sb.AppendLine(".columns-toggle { position: relative; }");
            sb.AppendLine(".columns-dropdown { position: absolute; top: 100%; right: 0; background: var(--bg-card);");
            sb.AppendLine("  border: 1px solid var(--border); border-radius: 6px; padding: 8px; margin-top: 4px;");
            sb.AppendLine("  min-width: 130px; display: none; z-index: 100; box-shadow: var(--shadow); }");
            sb.AppendLine(".columns-dropdown.open { display: block; }");
            sb.AppendLine(".columns-option { display: flex; align-items: center; gap: 6px; padding: 4px 6px; cursor: pointer; }");
            sb.AppendLine(".columns-option input { accent-color: var(--primary); }");

            sb.AppendLine(".table-wrapper { flex: 1; min-height: 0; display: flex; flex-direction: column; }");
            sb.AppendLine(".table-container { background: var(--bg-card); border-radius: 10px; border: 1px solid var(--border);");
            sb.AppendLine("  flex: 1; display: flex; flex-direction: column; overflow: hidden; }");
            sb.AppendLine(".table-scroll { flex: 1; overflow: auto; }");
            sb.AppendLine("table { width: 100%; border-collapse: collapse; }");
            sb.AppendLine("th, td { padding: 8px 12px; text-align: left; border-bottom: 1px solid var(--border); }");
            sb.AppendLine("th { background: var(--bg-main); font-weight: 600; font-size: 0.75rem; text-transform: uppercase;");
            sb.AppendLine("  letter-spacing: 0.05em; color: var(--text-secondary); position: sticky; top: 0; z-index: 10;");
            sb.AppendLine("  cursor: pointer; user-select: none; transition: background 0.2s; }");
            sb.AppendLine("th:hover { background: var(--bg-table); }");
            sb.AppendLine("th.sorted-asc::after, th.sorted-desc::after { content: ''; display: inline-block; margin-left: 4px; border: 4px solid transparent; }");
            sb.AppendLine("th.sorted-asc::after { border-bottom-color: var(--primary-light); margin-bottom: 2px; }");
            sb.AppendLine("th.sorted-desc::after { border-top-color: var(--primary-light); margin-top: 2px; }");
            sb.AppendLine("tbody tr { transition: background 0.15s; }");
            sb.AppendLine("tbody tr:hover { background: rgba(99, 102, 241, 0.1); }");
            sb.AppendLine("tbody tr.hidden { display: none; }");
            sb.AppendLine("td { font-size: 0.85rem; }");
            sb.AppendLine("td.hidden-col, th.hidden-col { display: none; }");

            sb.AppendLine(".badge { display: inline-flex; align-items: center; padding: 3px 8px; border-radius: 12px; font-size: 0.7rem; font-weight: 600; }");
            sb.AppendLine(".badge-price { background: rgba(99, 102, 241, 0.2); color: var(--primary-light); }");
            sb.AppendLine(".badge-series { background: rgba(148, 163, 184, 0.2); color: var(--text-secondary); }");
            sb.AppendLine(".badge-percent.low { background: rgba(239, 68, 68, 0.2); color: #f87171; }");
            sb.AppendLine(".badge-percent.medium { background: rgba(245, 158, 11, 0.2); color: #fbbf24; }");
            sb.AppendLine(".badge-percent.high { background: rgba(16, 185, 129, 0.2); color: #34d399; }");

            sb.AppendLine(".empty-state { text-align: center; padding: 40px 20px; color: var(--text-secondary); }");

            sb.AppendLine("@media (max-width: 768px) {");
            sb.AppendLine("  .header h1 { font-size: 1.4rem; }");
            sb.AppendLine("  .stats-grid { grid-template-columns: repeat(2, 1fr); }");
            sb.AppendLine("  .filters-row { flex-direction: column; align-items: stretch; }");
            sb.AppendLine("  .filter-group.flex-1 { min-width: 100%; }");
            sb.AppendLine("  .table-container { border-radius: 8px; }");
            sb.AppendLine("}");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine("<div class=\"header\">");
            sb.AppendLine("<h1>🎯 OnceInfo</h1>");
            sb.AppendLine("<p class=\"subtitle\">Análisis de probabilidad de premios en rascas de la ONCE</p>");
            sb.AppendLine("</div>");

            int totalRascas = (withSameValue?.Count ?? 0) + (withoutSameValue?.Count ?? 0);
            var uniqueRascas = allResults.Select(r => r.Nombre).Distinct().Count();
            sb.AppendLine("<div class=\"stats-grid\">");
            sb.AppendLine($"<div class=\"stat-card\"><div class=\"stat-value\">{uniqueRascas}</div><div class=\"stat-label\">Rascas</div></div>");
            sb.AppendLine($"<div class=\"stat-card\"><div class=\"stat-value\">{totalRascas}</div><div class=\"stat-label\">Registros</div></div>");
            sb.AppendLine($"<div class=\"stat-card\"><div class=\"stat-value\">{(precioMinDefault > 0 ? precioMinDefault + "€" : "Todos")}</div><div class=\"stat-label\">Premio Mín.</div></div>");
            sb.AppendLine("<div class=\"stat-card\"><div class=\"stat-value\" id=\"visibleCount\">0</div><div class=\"stat-label\">Visibles</div></div>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class=\"accordion\" id=\"filtersAccordion\">");
            sb.AppendLine("<div class=\"accordion-header\" onclick=\"toggleAccordion()\">");
            sb.AppendLine("<h2>🔍 Filtros</h2>");
            sb.AppendLine("<span class=\"accordion-arrow\">▼</span>");
            sb.AppendLine("</div>");
            sb.AppendLine("<div class=\"accordion-content\">");
            sb.AppendLine("<div class=\"filters-row\">");

            // Búsqueda
            sb.AppendLine("<div class=\"filter-group flex-1\">");
            sb.AppendLine("<label>Buscar</label>");
            sb.AppendLine("<input type=\"search\" id=\"search\" placeholder=\"Nombre o serie...\" style=\"width:100%\">");
            sb.AppendLine("</div>");

            // Modo
            if (precioMinDefault <= 10)
            {
                sb.AppendLine("<div class=\"filter-group\">");
                sb.AppendLine("<label>Modo</label>");
                sb.AppendLine("<select id=\"modeSelect\">");
                sb.AppendLine("<option value=\"all\">Todos</option>");
                sb.AppendLine("<option value=\"con\">Con mismo valor</option>");
                sb.AppendLine("<option value=\"sin\" selected>Sin mismo valor</option>");
                sb.AppendLine("</select>");
                sb.AppendLine("</div>");
            }

            // Precios
            sb.AppendLine("<div class=\"filter-group\">");
            sb.AppendLine("<label>Precio</label>");
            sb.AppendLine("<div class=\"multiselect-container\">");
            sb.AppendLine("<div class=\"multiselect-trigger\" id=\"priceTrigger\"><span class=\"selected-text\">Todos</span><span>▼</span></div>");
            sb.AppendLine("<div class=\"multiselect-dropdown\" id=\"priceDropdown\">");
            foreach (var price in uniquePrices)
            {
                var priceKey = price?.Replace(",", ".").Replace(" ", "") ?? "";
                sb.AppendLine($"<div class=\"multiselect-option\" onclick=\"event.stopPropagation()\">");
                sb.AppendLine($"<input type=\"checkbox\" id=\"price_{priceKey}\" data-price=\"{priceKey}\" checked>");
                sb.AppendLine($"<label for=\"price_{priceKey}\">{price}€</label></div>");
            }
            sb.AppendLine("</div></div></div>");

            // Porcentaje
            sb.AppendLine("<div class=\"filter-group\">");
            sb.AppendLine($"<label>Porcentaje <span class=\"info-tooltip\" data-tooltip=\"Q1: &lt;{q1:0.##}% | Q2: {q1:0.##}-{q2:0.##}% | Q3: {q2:0.##}-{q3:0.##}% | Q4: &gt;{q3:0.##}%\">ℹ️</span></label>");
            sb.AppendLine("<div class=\"multiselect-container\">");
            sb.AppendLine("<div class=\"multiselect-trigger\" id=\"porcTrigger\"><span class=\"selected-text\">Todos</span><span>▼</span></div>");
            sb.AppendLine("<div class=\"multiselect-dropdown\" id=\"porcDropdown\">");
            sb.AppendLine("<div class=\"multiselect-option\" onclick=\"event.stopPropagation()\">");
            sb.AppendLine("<input type=\"checkbox\" id=\"porc_all\" data-quartile=\"all\" checked><label for=\"porc_all\">Todos</label></div>");
            sb.AppendLine($"<div class=\"multiselect-option\" onclick=\"event.stopPropagation()\">");
            sb.AppendLine($"<input type=\"checkbox\" id=\"porc_q1\" data-quartile=\"q1\"><label for=\"porc_q1\">Bajo (&lt;{q1:0.##}%)</label></div>");
            sb.AppendLine($"<div class=\"multiselect-option\" onclick=\"event.stopPropagation()\">");
            sb.AppendLine($"<input type=\"checkbox\" id=\"porc_q2\" data-quartile=\"q2\"><label for=\"porc_q2\">Medio-Bajo</label></div>");
            sb.AppendLine($"<div class=\"multiselect-option\" onclick=\"event.stopPropagation()\">");
            sb.AppendLine($"<input type=\"checkbox\" id=\"porc_q3\" data-quartile=\"q3\"><label for=\"porc_q3\">Medio-Alto</label></div>");
            sb.AppendLine($"<div class=\"multiselect-option\" onclick=\"event.stopPropagation()\">");
            sb.AppendLine($"<input type=\"checkbox\" id=\"porc_q4\" data-quartile=\"q4\"><label for=\"porc_q4\">Alto (&gt;{q3:0.##}%)</label></div>");
            sb.AppendLine("</div></div></div>");

            // Rascas Premiados
            sb.AppendLine("<div class=\"filter-group\">");
            sb.AppendLine("<label>Rascas</label>");
            sb.AppendLine("<select id=\"premSelect\">");
            sb.AppendLine("<option value=\"all\">Todos</option>");
            sb.AppendLine($"<option value=\"top\">Más prem. (&gt;{rascasQ3:0})</option>");
            sb.AppendLine($"<option value=\"mid\">Media</option>");
            sb.AppendLine($"<option value=\"low\">Menos prem. (&lt;{rascasQ1:0})</option>");
            sb.AppendLine("</select></div>");

            // Botones
            sb.AppendLine("<div class=\"filter-group\">");
            sb.AppendLine("<label>&nbsp;</label>");
            sb.AppendLine("<div style=\"display:flex;gap:6px;\">");
            sb.AppendLine("<button class=\"btn btn-secondary\" id=\"clearFilters\">Limpiar</button>");
            sb.AppendLine("<button class=\"btn\" id=\"exportCsv\">CSV</button>");
            sb.AppendLine("</div></div>");

            // Columnas
            sb.AppendLine("<div class=\"filter-group\">");
            sb.AppendLine("<label>&nbsp;</label>");
            sb.AppendLine("<div class=\"columns-toggle\">");
            sb.AppendLine("<button class=\"btn btn-secondary btn-icon\" id=\"columnsBtn\">☰</button>");
            sb.AppendLine("<div class=\"columns-dropdown\" id=\"columnsDropdown\">");
            sb.AppendLine("<div class=\"columns-option\"><input type=\"checkbox\" id=\"col_serie\" checked><label for=\"col_serie\">Serie</label></div>");
            sb.AppendLine("<div class=\"columns-option\"><input type=\"checkbox\" id=\"col_precio\" checked><label for=\"col_precio\">Precio</label></div>");
            sb.AppendLine("<div class=\"columns-option\"><input type=\"checkbox\" id=\"col_rascas\" checked><label for=\"col_rascas\">Rascas</label></div>");
            sb.AppendLine("</div></div></div>");

            sb.AppendLine("</div></div></div>");

            // Table
            sb.AppendLine("<div class=\"table-wrapper\">");
            sb.AppendLine("<div class=\"table-container\" id=\"tableContainer\">");
            sb.AppendLine("<div class=\"table-scroll\">");
            sb.AppendLine("<table id=\"results\">");
            sb.AppendLine("<thead><tr>");
            sb.AppendLine("<th data-sort=\"idx\">#</th>");
            sb.AppendLine("<th data-sort=\"nombre\">Nombre</th>");
            sb.AppendLine("<th data-sort=\"serie\" id=\"th_serie\">Serie</th>");
            sb.AppendLine("<th data-sort=\"precio\" id=\"th_precio\">Precio</th>");
            sb.AppendLine("<th data-sort=\"rascas\" id=\"th_rascas\">Rascas</th>");
            sb.AppendLine("<th data-sort=\"porcentaje\" class=\"sorted-desc\">Porcentaje</th>");
            sb.AppendLine("</tr></thead><tbody>");

            void WriteRows(IEnumerable<RascaResultado> list, string mode)
            {
                int idx = 0;
                foreach (var r in list)
                {
                    idx++;
                    var precio = (r.Precio ?? "").Replace(",", ".").Replace("€", "").Trim();
                    string precioEsc = WebUtility.HtmlEncode(r.Precio ?? "");
                    string nombreEsc = WebUtility.HtmlEncode(r.Nombre);
                    string serieEsc = WebUtility.HtmlEncode(r.Serie);
                    string porcent = r.PorcentajePremio.ToString("0.##", CultureInfo.InvariantCulture);

                    string badgeClass = r.PorcentajePremio < q1 ? "low" : r.PorcentajePremio > q3 ? "high" : "medium";
                    string quartile = r.PorcentajePremio < q1 ? "q1" : r.PorcentajePremio <= q2 ? "q2" : r.PorcentajePremio <= q3 ? "q3" : "q4";
                    string rascasQuartile = r.RascasPremiados < rascasQ1 ? "low" : r.RascasPremiados > rascasQ3 ? "top" : "mid";

                    sb.AppendLine($"<tr data-mode=\"{mode}\" data-precio=\"{precio}\" data-quartile=\"{quartile}\" data-rascas=\"{r.RascasPremiados}\" data-rascas-quartile=\"{rascasQuartile}\" data-nombre=\"{EscapeAttribute(r.Nombre)}\" data-serie=\"{EscapeAttribute(r.Serie)}\">");
                    sb.AppendLine($"<td>{idx}</td>");
                    sb.AppendLine($"<td>{nombreEsc}</td>");
                    sb.AppendLine($"<td class=\"col_serie\"><span class=\"badge badge-series\">{serieEsc}</span></td>");
                    sb.AppendLine($"<td class=\"col_precio\"><span class=\"badge badge-price\">{precioEsc}</span></td>");
                    sb.AppendLine($"<td class=\"col_rascas\">{r.RascasPremiados:N0}</td>");
                    sb.AppendLine($"<td><span class=\"badge badge-percent {badgeClass}\">{porcent}%</span></td>");
                    sb.AppendLine("</tr>");
                }
            }

            if (precioMinDefault <= 10)
                WriteRows((withSameValue ?? new List<RascaResultado>()).OrderByDescending(x => x.PorcentajePremio).ToList(), "con");
            WriteRows((withoutSameValue ?? new List<RascaResultado>()).OrderByDescending(x => x.PorcentajePremio).ToList(), "sin");

            sb.AppendLine("</tbody></table></div></div></div>");
            sb.AppendLine("<div class=\"empty-state\" id=\"emptyState\" style=\"display:none;\"><p>No se encontraron resultados</p></div>");

            // JavaScript
            sb.AppendLine("<script>");
            sb.AppendLine("(function() {");
            sb.AppendLine("  const Q1 = " + q1.ToString(CultureInfo.InvariantCulture) + ";");
            sb.AppendLine("  const Q2 = " + q2.ToString(CultureInfo.InvariantCulture) + ";");
            sb.AppendLine("  const Q3 = " + q3.ToString(CultureInfo.InvariantCulture) + ";");
            sb.AppendLine("  const RASCAS_Q1 = " + rascasQ1.ToString("0", CultureInfo.InvariantCulture) + ";");
            sb.AppendLine("  const RASCAS_Q3 = " + rascasQ3.ToString("0", CultureInfo.InvariantCulture) + ";");
            sb.AppendLine("  let rows = Array.from(document.querySelectorAll('#results tbody tr'));");
            sb.AppendLine("  let sortColumn = 'porcentaje';");
            sb.AppendLine("  let sortDirection = 'desc';");
            sb.AppendLine("  const searchInput = document.getElementById('search');");
            if (precioMinDefault <= 10)
                sb.AppendLine("  const modeSelect = document.getElementById('modeSelect');");
            sb.AppendLine("  const premSelect = document.getElementById('premSelect');");
            sb.AppendLine("  const visibleCount = document.getElementById('visibleCount');");
            sb.AppendLine("  const emptyState = document.getElementById('emptyState');");
            sb.AppendLine("  const tableContainer = document.getElementById('tableContainer');");
            sb.AppendLine("  const clearBtn = document.getElementById('clearFilters');");
            sb.AppendLine("  const exportBtn = document.getElementById('exportCsv');");

            // Accordion
            sb.AppendLine("  window.toggleAccordion = function() { document.getElementById('filtersAccordion').classList.toggle('collapsed'); };");

            // Multiselect setup
            sb.AppendLine("  function setupMultiselect(triggerId, dropdownId) {");
            sb.AppendLine("    const trigger = document.getElementById(triggerId);");
            sb.AppendLine("    const dropdown = document.getElementById(dropdownId);");
            sb.AppendLine("    trigger.addEventListener('click', function(e) { e.stopPropagation(); dropdown.classList.toggle('open'); });");
            sb.AppendLine("    document.addEventListener('click', function() { dropdown.classList.remove('open'); });");
            sb.AppendLine("    dropdown.querySelectorAll('input').forEach(function(cb) {");
            sb.AppendLine("      cb.addEventListener('change', function() { updateMultiselectText(dropdown, trigger); applyFilters(); });");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            sb.AppendLine("  function updateMultiselectText(dropdown, trigger) {");
            sb.AppendLine("    const checkboxes = dropdown.querySelectorAll('input[type=\"checkbox\"]');");
            sb.AppendLine("    const checked = Array.from(checkboxes).filter(function(cb) { return cb.checked; });");
            sb.AppendLine("    const textEl = trigger.querySelector('.selected-text');");
            sb.AppendLine("    if (checked.length === 0) textEl.textContent = 'Ninguno';");
            sb.AppendLine("    else if (checked.length === checkboxes.length) textEl.textContent = 'Todos';");
            sb.AppendLine("    else textEl.textContent = checked.length + ' sel.';");
            sb.AppendLine("  }");

            sb.AppendLine("  setupMultiselect('priceTrigger', 'priceDropdown');");
            sb.AppendLine("  setupMultiselect('porcTrigger', 'porcDropdown');");

            // Columns toggle
            sb.AppendLine("  const columnsBtn = document.getElementById('columnsBtn');");
            sb.AppendLine("  const columnsDropdown = document.getElementById('columnsDropdown');");
            sb.AppendLine("  columnsBtn.addEventListener('click', function(e) { e.stopPropagation(); columnsDropdown.classList.toggle('open'); });");
            sb.AppendLine("  document.addEventListener('click', function() { columnsDropdown.classList.remove('open'); });");
            sb.AppendLine("  ['col_serie', 'col_precio', 'col_rascas'].forEach(function(col) {");
            sb.AppendLine("    document.getElementById(col).addEventListener('change', function() {");
            sb.AppendLine("      const show = this.checked;");
            sb.AppendLine("      document.querySelectorAll('td.' + col).forEach(function(el) { el.classList.toggle('hidden-col', !show); });");
            sb.AppendLine("      document.getElementById('th_' + col.replace('col_', '')).classList.toggle('hidden-col', !show);");
            sb.AppendLine("    });");
            sb.AppendLine("  });");

            // Mobile columns
            sb.AppendLine("  if (window.innerWidth < 768) {");
            sb.AppendLine("    ['col_serie', 'col_precio', 'col_rascas'].forEach(function(col) {");
            sb.AppendLine("      document.getElementById(col).checked = false;");
            sb.AppendLine("      document.querySelectorAll('td.' + col).forEach(function(el) { el.classList.add('hidden-col'); });");
            sb.AppendLine("      document.getElementById('th_' + col.replace('col_', '')).classList.add('hidden-col');");
            sb.AppendLine("    });");
            sb.AppendLine("  }");

            sb.AppendLine("  function getSelectedPrices() {");
            sb.AppendLine("    return Array.from(document.querySelectorAll('#priceDropdown input:checked')).map(function(cb) {");
            sb.AppendLine("      return parseFloat(cb.dataset.price) || 0;");
            sb.AppendLine("    });");
            sb.AppendLine("  }");
            sb.AppendLine("  function getSelectedQuartiles() {");
            sb.AppendLine("    if (document.getElementById('porc_all').checked) return ['q1', 'q2', 'q3', 'q4'];");
            sb.AppendLine("    return Array.from(document.querySelectorAll('#porcDropdown input:checked')).map(function(cb) {");
            sb.AppendLine("      return cb.dataset.quartile;");
            sb.AppendLine("    }).filter(function(q) { return q && q !== 'all'; });");
            sb.AppendLine("  }");

            sb.AppendLine("  function applyFilters() {");
            sb.AppendLine("    const q = (searchInput.value || '').toLowerCase();");
            if (precioMinDefault <= 10)
                sb.AppendLine("    const mode = modeSelect.value;");
            sb.AppendLine("    const prem = premSelect.value;");
            sb.AppendLine("    const selectedPrices = getSelectedPrices();");
            sb.AppendLine("    const selectedQuartiles = getSelectedQuartiles();");
            sb.AppendLine("    let count = 0;");
            sb.AppendLine("    rows.forEach(function(row) {");
            sb.AppendLine("      const rowMode = row.dataset.mode;");
            sb.AppendLine("      const precio = parseFloat(row.dataset.precio || 0);");
            sb.AppendLine("      const quartile = row.dataset.quartile;");
            sb.AppendLine("      const rascasQ = row.dataset.rascasQuartile;");
            sb.AppendLine("      const nombre = (row.dataset.nombre || '').toLowerCase();");
            sb.AppendLine("      const serie = (row.dataset.serie || '').toLowerCase();");
            sb.AppendLine("      let visible = true;");
            if (precioMinDefault <= 10)
            {
                sb.AppendLine("      if (mode === 'con' && rowMode !== 'con') visible = false;");
                sb.AppendLine("      if (mode === 'sin' && rowMode !== 'sin') visible = false;");
            }
            else
            {
                sb.AppendLine("      if (rowMode === 'con') visible = false;");
            }
            sb.AppendLine("      if (selectedPrices.length > 0 && !selectedPrices.includes(precio)) visible = false;");
            sb.AppendLine("      if (selectedQuartiles.length > 0 && !selectedQuartiles.includes(quartile)) visible = false;");
            sb.AppendLine("      if (prem === 'top' && rascasQ !== 'top') visible = false;");
            sb.AppendLine("      if (prem === 'mid' && rascasQ !== 'mid') visible = false;");
            sb.AppendLine("      if (prem === 'low' && rascasQ !== 'low') visible = false;");
            sb.AppendLine("      if (q && nombre.indexOf(q) === -1 && serie.indexOf(q) === -1) visible = false;");
            sb.AppendLine("      row.classList.toggle('hidden', !visible);");
            sb.AppendLine("      if (visible) count++;");
            sb.AppendLine("    });");
            sb.AppendLine("    visibleCount.textContent = count;");
            sb.AppendLine("    emptyState.style.display = count === 0 ? 'block' : 'none';");
            sb.AppendLine("    tableContainer.style.display = count === 0 ? 'none' : 'flex';");
            sb.AppendLine("  }");

            sb.AppendLine("  function sortTable(column) {");
            sb.AppendLine("    const headers = document.querySelectorAll('#results th');");
            sb.AppendLine("    headers.forEach(function(h) { h.classList.remove('sorted-asc', 'sorted-desc'); });");
            sb.AppendLine("    if (sortColumn === column) { sortDirection = sortDirection === 'asc' ? 'desc' : 'asc'; }");
            sb.AppendLine("    else { sortColumn = column; sortDirection = column === 'porcentaje' ? 'desc' : 'asc'; }");
            sb.AppendLine("    document.querySelector('th[data-sort=\"' + column + '\"]').classList.add(sortDirection === 'asc' ? 'sorted-asc' : 'sorted-desc');");
            sb.AppendLine("    const getValue = function(row, col) {");
            sb.AppendLine("      switch(col) {");
            sb.AppendLine("        case 'idx': return parseInt(row.cells[0].textContent) || 0;");
            sb.AppendLine("        case 'nombre': return (row.dataset.nombre || '').toLowerCase();");
            sb.AppendLine("        case 'serie': return (row.dataset.serie || '').toLowerCase();");
            sb.AppendLine("        case 'precio': return parseFloat(row.dataset.precio) || 0;");
            sb.AppendLine("        case 'rascas': return parseInt(row.dataset.rascas) || 0;");
            sb.AppendLine("        case 'porcentaje': return parseFloat(row.dataset.porcentaje || row.querySelector('td:nth-child(6) .badge').textContent) || 0;");
            sb.AppendLine("        default: return 0;");
            sb.AppendLine("      }");
            sb.AppendLine("    };");
            sb.AppendLine("    rows.sort(function(a, b) {");
            sb.AppendLine("      const va = getValue(a, column); const vb = getValue(b, column);");
            sb.AppendLine("      if (typeof va === 'string') return sortDirection === 'asc' ? va.localeCompare(vb) : vb.localeCompare(va);");
            sb.AppendLine("      return sortDirection === 'asc' ? va - vb : vb - va;");
            sb.AppendLine("    });");
            sb.AppendLine("    const tbody = document.querySelector('#results tbody');");
            sb.AppendLine("    rows.forEach(function(row, i) { row.cells[0].textContent = i + 1; tbody.appendChild(row); });");
            sb.AppendLine("  }");

            sb.AppendLine("  function clearFilters() {");
            sb.AppendLine("    searchInput.value = '';");
            if (precioMinDefault <= 10)
                sb.AppendLine("    modeSelect.value = 'sin';");
            sb.AppendLine("    premSelect.value = 'all';");
            sb.AppendLine("    document.querySelectorAll('#priceDropdown input').forEach(function(cb) { cb.checked = true; });");
            sb.AppendLine("    document.querySelectorAll('#porcDropdown input').forEach(function(cb) { cb.checked = cb.id === 'porc_all'; });");
            sb.AppendLine("    document.querySelector('#priceTrigger .selected-text').textContent = 'Todos';");
            sb.AppendLine("    document.querySelector('#porcTrigger .selected-text').textContent = 'Todos';");
            sb.AppendLine("    applyFilters();");
            sb.AppendLine("  }");

            sb.AppendLine("  function exportCsv() {");
            sb.AppendLine("    const visibleRows = rows.filter(function(r) { return !r.classList.contains('hidden'); });");
            sb.AppendLine("    let csv = '\\uFEFF#;Nombre;Serie;Precio;Rascas;Porcentaje\\n';");
            sb.AppendLine("    visibleRows.forEach(function(row, i) {");
            sb.AppendLine("      csv += (i+1) + ';' + row.dataset.nombre + ';' + row.dataset.serie + ';' + row.dataset.precio + ';' + row.dataset.rascas + ';' + row.dataset.porcentaje + '\\n';");
            sb.AppendLine("    });");
            sb.AppendLine("    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8' });");
            sb.AppendLine("    const a = document.createElement('a');");
            sb.AppendLine("    a.href = URL.createObjectURL(blob);");
            sb.AppendLine("    a.download = 'onceinfo-export.csv';");
            sb.AppendLine("    a.click();");
            sb.AppendLine("  }");

            sb.AppendLine("  searchInput.addEventListener('input', applyFilters);");
            if (precioMinDefault <= 10)
                sb.AppendLine("  modeSelect.addEventListener('change', applyFilters);");
            sb.AppendLine("  premSelect.addEventListener('change', applyFilters);");
            sb.AppendLine("  clearBtn.addEventListener('click', clearFilters);");
            sb.AppendLine("  exportBtn.addEventListener('click', exportCsv);");
            sb.AppendLine("  document.querySelectorAll('#results th').forEach(function(th) {");
            sb.AppendLine("    th.addEventListener('click', function() { const col = th.dataset.sort; if (col) sortTable(col); });");
            sb.AppendLine("  });");
            sb.AppendLine("  applyFilters();");
            sb.AppendLine("})();");
            sb.AppendLine("</script>");
            sb.AppendLine("</body></html>");

            return sb.ToString();
        }
    }
}