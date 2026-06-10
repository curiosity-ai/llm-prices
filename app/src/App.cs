using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using H5;
using Tesserae;
using static H5.Core.dom;
using static Tesserae.UI;

namespace LlmPrices
{
    /// <summary>
    /// "Compare Studio" — a Tesserae (C# -> JS via h5) pricing explorer for LLMs.
    /// Renders a log/log input-vs-output price scatter, a token-mix "Compute" panel with a
    /// compare tray, and a blended-cost table of every model. Pricing data originates from
    /// https://github.com/simonw/llm-prices (current-v1.json generated at build time).
    /// </summary>
    internal static class App
    {
        // ---- state -------------------------------------------------------------------
        private static readonly List<ModelPrice> _all      = new List<ModelPrice>();
        private static readonly List<string>     _tray     = new List<string>(); // selection, in click order
        private static string                    _search   = "";
        private static string                    _sortCol  = "blended"; // name|input|cached|output|blended
        private static bool                      _sortAsc  = true;
        private static string                    _updatedAt = "";

        // ---- mutable hosts (rebuilt on change) ----------------------------------------
        private static HTMLElement _chartDiv;
        private static HTMLElement _legendDiv;
        private static Stack       _trayHolder;
        private static Stack       _trayHeaderHolder;
        private static Stack       _theadHolder;
        private static Stack       _rowsHolder;
        private static Stack       _footer;

        // token inputs
        private static TextBox _inTok;
        private static TextBox _cachedTok;
        private static TextBox _outTok;

        // ---- vendor metadata -----------------------------------------------------------

        private static readonly Dictionary<string, string> VendorNames = new Dictionary<string, string>
        {
            { "openai",      "OpenAI"      },
            { "anthropic",   "Anthropic"   },
            { "google",      "Google"      },
            { "deepseek",    "DeepSeek"    },
            { "moonshot-ai", "Moonshot AI" },
            { "meta",        "Meta"        },
            { "xai",         "xAI"         },
            { "mistral",     "Mistral"     },
            { "qwen",        "Qwen"        },
            { "amazon",      "Amazon"      },
            { "minimax",     "MiniMax"     },
        };

        private static readonly Dictionary<string, string> VendorColors = new Dictionary<string, string>
        {
            { "openai",      "#10a37f" },
            { "anthropic",   "#d97757" },
            { "google",      "#4285f4" },
            { "deepseek",    "#4d6bfe" },
            { "moonshot-ai", "#16161d" },
            { "meta",        "#0866ff" },
            { "xai",         "#101013" },
            { "mistral",     "#fa520f" },
            { "qwen",        "#6e56cf" },
            { "amazon",      "#e8912d" },
            { "minimax",     "#f23f5d" },
        };

        private static readonly Dictionary<string, string> VendorInitials = new Dictionary<string, string>
        {
            { "openai",      "AI" },
            { "anthropic",   "A\\" },
            { "google",      "G"  },
            { "deepseek",    "DS" },
            { "moonshot-ai", "K"  },
            { "meta",        "M"  },
            { "xai",         "X"  },
            { "mistral",     "M"  },
            { "qwen",        "Q"  },
            { "amazon",      "A"  },
            { "minimax",     "MM" },
        };

        private static string VendorName(string vendor)
        {
            if (VendorNames.TryGetValue(vendor, out var n)) return n;
            return string.IsNullOrEmpty(vendor) ? "Unknown" : char.ToUpper(vendor[0]) + vendor.Substring(1);
        }

        private static string VendorColor(string vendor)
        {
            return VendorColors.TryGetValue(vendor, out var c) ? c : "#94a3b8";
        }

        private static string VendorInitial(string vendor)
        {
            if (VendorInitials.TryGetValue(vendor, out var i)) return i;
            return string.IsNullOrEmpty(vendor) ? "?" : vendor.Substring(0, 1).ToUpper();
        }

        // ---- entry point ----------------------------------------------------------------

        private static void Main()
        {
            EnsureViewport();
            Styles.Inject();
            document.title = "LLM Prices — Compare Studio";

            Theme.Light();

            MountToBody(BuildPage());

            PriceData.Load((models, updatedAt) =>
            {
                _all.Clear();
                if (models != null) _all.AddRange(models);
                _updatedAt = updatedAt ?? "unknown";

                // a friendly default tray so the page tells a story on first load
                if (_tray.Count == 0)
                {
                    foreach (var id in new[] { "deepseek-chat", "gemini-2.5-flash", "gpt-5", "claude-sonnet-4.5" })
                    {
                        if (_all.Any(m => m.Id == id)) _tray.Add(id);
                    }
                }

                RenderAll();
                UpdateFooterDate();
            });
        }

        private static void RenderAll()
        {
            RenderChart();
            RenderLegend();
            RenderTray();
            RenderTable();
        }

        // ---- page shell --------------------------------------------------------------

        private static IComponent BuildPage()
        {
            _trayHolder       = VStack().WS().Gap(14.px());
            _trayHeaderHolder = VStack().WS();
            _theadHolder      = VStack().WS();
            _rowsHolder       = VStack().WS();

            var content = VStack()
                .WS()
                .Padding(24.px())
                .Gap(20.px())
                .Style(s => { s.marginLeft = "auto"; s.marginRight = "auto"; })
                .Children(
                    BuildTopRow(),
                    BuildTableCard(),
                    BuildFooter());

            return VStack().S().ScrollY().Children(
                BuildNavBar(),
                content);
        }

        private static IComponent BuildNavBar()
        {
            var logo = Image("assets/img/curiosity-logo.svg").Class("lp-nav-logo").W(34.px()).H(34.px());

            var title = HStack().AlignItems(ItemAlign.Baseline).Gap(12.px()).Children(
                TextBlock("LLM Prices").Class("lp-nav-title"),
                TextBlock("Compare Studio").Class("lp-nav-sub"));

            var by = Raw(Div(_("lp-nav-by")));
            by.Do(r => r.Render().innerHTML =
                "by <a class='lp-nav-brand' href='https://curiosity.ai' target='_blank' rel='noopener'>curiosity.ai</a>");

            var about = TextBlock("About").Class("lp-nav-link").OnClick((s, e) => ShowAbout());

            var inner = HStack().WS().AlignItemsCenter().Gap(14.px())
                .PaddingLeft(24.px()).PaddingRight(24.px())
                .Style(st => { st.marginLeft = "auto"; st.marginRight = "auto"; })
                .Children(logo, title, Raw().Grow(), by, about);

            return VStack().WS().Class("lp-nav").Children(inner);
        }

        private static IComponent BuildTopRow()
        {
            var chart   = BuildChartCard();
            var compute = BuildComputeCard();

            if (Theme.IsMobileMode)
            {
                return VStack().WS().Gap(20.px()).Children(chart.WS(), compute.WS());
            }

            return HStack().WS().Gap(20.px()).AlignItems(ItemAlign.Stretch).Children(
                chart.Grow(),
                compute.W(380.px()).NoShrink());
        }

        // ---- price landscape (scatter) -------------------------------------------------

        private static IComponent BuildChartCard()
        {
            _chartDiv  = Div(_("lp-chart"));
            _legendDiv = Div(_("lp-legend"));

            var header = HStack().WS().AlignItems(ItemAlign.Baseline).Children(
                TextBlock("Price landscape").Class("lp-card-title").Grow(),
                TextBlock("log scale · $ per 1M tokens").Small().Foreground("var(--lp-muted)"));

            return VStack().Class("lp-card").WS().Padding(22.px()).Gap(10.px()).Children(
                header,
                TextBlock("Down & left is cheaper. Click any model to add it to the compare tray.")
                    .Small().Foreground("var(--lp-muted)"),
                Raw(_chartDiv).WS().Grow(),
                Raw(_legendDiv).WS());
        }

        private sealed class Pt
        {
            public ModelPrice M;
            public double X;
            public double Y;
            public bool Selected;
        }

        private static void RenderChart()
        {
            const double W = 1000, H = 540;
            const double padL = 64, padR = 30, padT = 18, padB = 52;

            var models = _all.Where(m => m.Input > 0 && m.Output > 0).ToList();
            if (models.Count == 0)
            {
                _chartDiv.innerHTML = "<div class='lp-chart-empty'>Loading prices…</div>";
                return;
            }

            double minX = Math.Log10(models.Min(m => m.Input));
            double maxX = Math.Log10(models.Max(m => m.Input));
            double minY = Math.Log10(models.Min(m => m.Output));
            double maxY = Math.Log10(models.Max(m => m.Output));

            double padX = Math.Max(0.08, (maxX - minX) * 0.05);
            double padY = Math.Max(0.08, (maxY - minY) * 0.05);
            minX -= padX; maxX += padX;
            minY -= padY; maxY += padY;

            Func<double, double> sx = v => padL + (Math.Log10(v) - minX) / (maxX - minX) * (W - padL - padR);
            Func<double, double> sy = v => H - padB - (Math.Log10(v) - minY) / (maxY - minY) * (H - padT - padB);

            var pts = models
                .Select(m => new Pt { M = m, X = sx(m.Input), Y = sy(m.Output), Selected = _tray.Contains(m.Id) })
                .OrderBy(p => p.Selected ? 1 : 0) // draw selected on top
                .ToList();

            var sb = new StringBuilder();
            sb.Append("<svg viewBox='0 0 " + W + " " + H + "' class='lp-svg' role='img' aria-label='Input vs output price scatter plot'>");

            // best-value region: below the median input & output price
            var sortedIn  = models.Select(m => m.Input).OrderBy(v => v).ToList();
            var sortedOut = models.Select(m => m.Output).OrderBy(v => v).ToList();
            double medIn  = sortedIn[sortedIn.Count / 2];
            double medOut = sortedOut[sortedOut.Count / 2];
            double bx = sx(medIn), by = sy(medOut);
            sb.Append(Fmt("<rect x='{0}' y='{1}' width='{2}' height='{3}' fill='#22c55e' opacity='0.10'/>",
                padL, by, bx - padL, H - padB - by));

            // gridlines + ticks
            foreach (var t in Ticks(minX, maxX))
            {
                double x = sx(t);
                sb.Append(Fmt("<line x1='{0}' y1='{1}' x2='{0}' y2='{2}' class='lp-grid'/>", x, padT, H - padB));
                sb.Append(Fmt("<text x='{0}' y='{1}' text-anchor='middle' class='lp-tick'>{2}</text>", x, H - padB + 22, TickLabel(t)));
            }
            foreach (var t in Ticks(minY, maxY))
            {
                double y = sy(t);
                sb.Append(Fmt("<line x1='{0}' y1='{1}' x2='{2}' y2='{1}' class='lp-grid'/>", padL, y, W - padR));
                sb.Append(Fmt("<text x='{0}' y='{1}' text-anchor='end' class='lp-tick'>{2}</text>", padL - 10, y + 4, TickLabel(t)));
            }

            // axis titles
            sb.Append(Fmt("<text x='{0}' y='{1}' text-anchor='middle' class='lp-axis'>Input price →</text>", padL + (W - padL - padR) / 2, H - 8));
            sb.Append(Fmt("<text x='{0}' y='{1}' text-anchor='middle' class='lp-axis' transform='rotate(-90 {0} {1})'>Output price →</text>", 18, padT + (H - padT - padB) / 2));

            // points
            foreach (var p in pts)
            {
                string color = VendorColor(p.M.Vendor);
                string title = Esc(p.M.Name) + " — $" + TrimNum(p.M.Input) + " in / $" + TrimNum(p.M.Output) + " out";

                if (p.Selected)
                {
                    sb.Append(Fmt("<circle cx='{0}' cy='{1}' r='12' fill='none' stroke='{2}' stroke-opacity='0.4' stroke-width='3'/>", p.X, p.Y, color));
                }

                sb.Append(Fmt("<circle data-id='{0}' cx='{1}' cy='{2}' r='{3}' fill='{4}' fill-opacity='{5}' class='lp-pt'><title>{6}</title></circle>",
                    Esc(p.M.Id), p.X, p.Y, p.Selected ? 7 : 5.5, color, p.Selected ? 1 : 0.85, title));
            }

            // labels for selected models, drawn on top with a simple vertical de-overlap
            var labels = pts.Where(p => p.Selected).OrderBy(p => p.Y).ToList();
            for (int i = 1; i < labels.Count; i++)
            {
                double dy = labels[i].Y - labels[i - 1].Y;
                if (dy < 18 && Math.Abs(labels[i].X - labels[i - 1].X) < 220)
                {
                    labels[i].Y = labels[i - 1].Y + 18;
                }
            }
            foreach (var p in labels)
            {
                bool flip = p.X > W * 0.72;
                sb.Append(Fmt("<text x='{0}' y='{1}' text-anchor='{2}' class='lp-pt-label'>{3}</text>",
                    flip ? p.X - 16 : p.X + 16, p.Y + 4.5, flip ? "end" : "start", Esc(p.M.Name)));
            }

            sb.Append(Fmt("<text x='{0}' y='{1}' class='lp-best-label'>BEST VALUE</text>", padL + 12, H - padB - 12));

            sb.Append("</svg>");
            _chartDiv.innerHTML = sb.ToString();

            // wire up clicks
            var nodes = _chartDiv.querySelectorAll("[data-id]");
            for (uint i = 0; i < nodes.length; i++)
            {
                var el = nodes[(int)i].As<HTMLElement>();
                el.addEventListener("click", e =>
                {
                    var id = el.getAttribute("data-id");
                    if (!string.IsNullOrEmpty(id)) Toggle(id);
                });
            }
        }

        private static void RenderLegend()
        {
            var vendors = _all.GroupBy(m => m.Vendor)
                              .OrderByDescending(g => g.Count())
                              .Select(g => g.Key)
                              .ToList();
            var sb = new StringBuilder();
            foreach (var v in vendors)
            {
                sb.Append(Fmt("<span class='lp-legend-item'><span class='lp-legend-dot' style='background:{0}'></span>{1}</span>",
                    VendorColor(v), Esc(VendorName(v))));
            }
            _legendDiv.innerHTML = sb.ToString();
        }

        private static List<double> Ticks(double logMin, double logMax)
        {
            var candidates = new[] { 0.001, 0.003, 0.01, 0.03, 0.1, 0.3, 1, 3, 10, 30, 100, 300, 1000 };
            return candidates.Where(c => Math.Log10(c) >= logMin && Math.Log10(c) <= logMax).ToList();
        }

        private static string TickLabel(double v)
        {
            return "$" + TrimNum(v);
        }

        // ---- compute panel -------------------------------------------------------------

        private static IComponent BuildComputeCard()
        {
            _inTok     = TokenInput("10,000");
            _cachedTok = TokenInput("100,000");
            _outTok    = TokenInput("10,000");

            var inputs = HStack().WS().Gap(10.px()).Children(
                TokenField("INPUT", _inTok).Grow(),
                TokenField("CACHED", _cachedTok).Grow(),
                TokenField("OUTPUT", _outTok).Grow());

            return VStack().Class("lp-card").WS().Padding(22.px()).Gap(14.px()).Children(
                TextBlock("Compute").Class("lp-card-title"),
                inputs,
                TextBlock("tokens per request").Small().Foreground("var(--lp-muted)"),
                _trayHeaderHolder,
                _trayHolder.Grow());
        }

        private static IComponent TokenField(string label, TextBox box)
        {
            return VStack().Gap(6.px()).Children(
                TextBlock(label).Class("lp-field-label"),
                box.WS());
        }

        private static TextBox TokenInput(string initial)
        {
            var box = TextBox(initial).Class("lp-input");
            box.OnInput((s, e) => { RenderTray(); RenderTable(); });
            return box;
        }

        private static void RenderTray()
        {
            _trayHeaderHolder.Clear();
            _trayHolder.Clear();

            var header = HStack().WS().AlignItemsCenter().Gap(10.px()).Children(
                TextBlock("COMPARE TRAY").Class("lp-tray-title").Grow(),
                TextBlock(_tray.Count == 1 ? "1 model" : _tray.Count + " models").Small().Foreground("var(--lp-muted)"));

            if (_tray.Count > 0)
            {
                var clear = TextBlock("clear").Class("lp-clear-link").OnClick((s, e) =>
                {
                    _tray.Clear();
                    RenderAll();
                });
                header.Add(clear);
            }

            _trayHeaderHolder.Add(header);

            if (_tray.Count == 0)
            {
                _trayHolder.Add(VStack().WS().Class("lp-tray-empty").Padding(14.px()).Children(
                    TextBlock("Click models on the chart, or tick them in the table below, to compare costs here.")
                        .Small().Foreground("var(--lp-muted)")));
                return;
            }

            double inTok = Val(_inTok), cachedTok = Val(_cachedTok), outTok = Val(_outTok);

            var rows = _all
                .Where(m => _tray.Contains(m.Id))
                .Select(m => new { Model = m, Cost = CostOf(m, inTok, cachedTok, outTok) })
                .OrderBy(x => x.Cost)
                .ToList();

            double maxCost = rows.Count > 0 ? rows.Max(r => r.Cost) : 0;

            bool first = true;
            foreach (var r in rows)
            {
                bool best = first && rows.Count > 1; first = false;

                var cost = TextBlock(Money(r.Cost)).Class(best ? "lp-tray-cost lp-good" : "lp-tray-cost");

                var bar  = Div(_("lp-bar"));
                var fill = Div(_(best ? "lp-bar-fill lp-bar-best" : "lp-bar-fill"));
                fill.style.width = (maxCost > 0 ? Math.Max(2, r.Cost / maxCost * 100) : 0) + "%";
                bar.appendChild(fill);

                var item = VStack().WS().Gap(7.px()).Class("lp-tray-item").Children(
                    HStack().WS().AlignItemsCenter().Gap(10.px()).Children(
                        VendorTile(r.Model.Vendor),
                        TextBlock(r.Model.Name).SemiBold().Foreground("var(--lp-text)").Grow(),
                        cost),
                    Raw(bar).WS());

                var id = r.Model.Id;
                item.Tooltip("Click to remove from the tray");
                item.Do(el => el.Render().addEventListener("click", e => Toggle(id)));

                _trayHolder.Add(item);
            }

            if (rows.Count > 1 && rows[0].Cost > 0)
            {
                double ratio = rows[rows.Count - 1].Cost / rows[0].Cost;
                var note = Raw(Div(_("lp-tray-note")));
                note.Do(el => el.Render().innerHTML =
                    "<b class='lp-good'>" + Esc(rows[0].Model.Name) + "</b> is " + ratio.ToString("0.0") +
                    "× cheaper than the priciest.");
                _trayHolder.Add(note);
            }
        }

        // ---- all-models table ------------------------------------------------------------

        private static IComponent BuildTableCard()
        {
            var search = SearchBox("Search models or providers...")
                .SearchAsYouType()
                .OnSearch((s, term) => { _search = term ?? ""; RenderTable(); });

            var header = HStack().WS().AlignItemsCenter().Gap(12.px()).Children(
                HStack().AlignItems(ItemAlign.Baseline).Gap(8.px()).Grow().Children(
                    TextBlock("All models").Class("lp-card-title"),
                    TextBlock("· computed = cost of current token mix").Small().Foreground("var(--lp-muted)")),
                search.W(260.px()));

            return VStack().Class("lp-card").WS().Padding(22.px()).Gap(8.px()).Children(
                header,
                _theadHolder,
                _rowsHolder);
        }

        private static void RenderTable()
        {
            RenderTableHeader();
            _rowsHolder.Clear();

            double inTok = Val(_inTok), cachedTok = Val(_cachedTok), outTok = Val(_outTok);

            var rows = _all.Where(Matches).ToList();
            rows = Sort(rows, inTok, cachedTok, outTok);

            if (rows.Count == 0)
            {
                _rowsHolder.Add(VStack().WS().Padding(24.px()).Children(
                    TextBlock(_all.Count == 0 ? "Loading prices..." : "No models match your search.")
                        .Small().Foreground("var(--lp-muted)").TextCenter()));
                return;
            }

            double minBlended = rows.Min(m => CostOf(m, inTok, cachedTok, outTok));

            foreach (var m in rows)
            {
                _rowsHolder.Add(Row(m, inTok, cachedTok, outTok, minBlended));
            }
        }

        private static void RenderTableHeader()
        {
            _theadHolder.Clear();

            _theadHolder.Add(HStack().WS().Class("lp-thead").AlignItemsCenter().Gap(12.px())
                .PaddingBottom(10.px()).PaddingLeft(10.px()).PaddingRight(10.px()).Children(
                    Raw().W(18.px()),
                    Th("MODEL", "name").Grow(),
                    Th("INPUT", "input").W(110.px()),
                    Th("CACHED", "cached").W(110.px()),
                    Th("OUTPUT", "output").W(110.px()),
                    Th("COMPUTED", "computed").W(110.px())));
        }

        private static TextBlock Th(string label, string col)
        {
            bool active = _sortCol == col;
            var text = label + (active ? (_sortAsc ? " ↑" : " ↓") : "");
            var tb = TextBlock(text).Class(active ? "lp-th lp-th-active" : "lp-th");
            if (col != "name") tb.TextRight();
            tb.OnClick((s, e) =>
            {
                if (_sortCol == col) { _sortAsc = !_sortAsc; }
                else { _sortCol = col; _sortAsc = true; }
                RenderTable();
            });
            return tb;
        }

        private static IComponent Row(ModelPrice m, double inTok, double cachedTok, double outTok, double minBlended)
        {
            bool sel = _tray.Contains(m.Id);
            double blended = CostOf(m, inTok, cachedTok, outTok);
            bool cheapest = blended <= minBlended + 1e-12;

            var check = Div(_(sel ? "lp-cb lp-cb-on" : "lp-cb"));
            if (sel) check.textContent = "✓";

            var name = VStack().Gap(2.px()).Grow().Children(
                TextBlock(m.Name).SemiBold().Foreground("var(--lp-text)"),
                TextBlock(VendorName(m.Vendor)).Tiny().Foreground("var(--lp-muted)"));

            var row = HStack()
                .Class(sel ? "lp-row lp-row-selected" : "lp-row")
                .WS().AlignItemsCenter().Gap(12.px())
                .PaddingTop(12.px()).PaddingBottom(12.px()).PaddingLeft(10.px()).PaddingRight(10.px())
                .Children(
                    Raw(check).W(18.px()).NoShrink(),
                    VendorTile(m.Vendor),
                    name,
                    Cell(Price(m.Input)),
                    Cell(m.InputCached.HasValue ? Price(m.InputCached.Value) : "—"),
                    Cell(Price(m.Output)),
                    TextBlock(Money(blended)).Class(cheapest ? "lp-cell lp-blended lp-good" : "lp-cell lp-blended").W(110.px()).TextRight());

            row.Do(r => r.Render().addEventListener("click", e => Toggle(m.Id)));
            return row;
        }

        private static TextBlock Cell(string text)
        {
            return TextBlock(text).Class("lp-cell").W(110.px()).TextRight();
        }

        private static void Toggle(string id)
        {
            if (!_tray.Remove(id)) _tray.Add(id);
            RenderAll();
        }

        // ---- shared bits ---------------------------------------------------------------

        private static IComponent VendorTile(string vendor)
        {
            var tile = Div(_("lp-tile"));
            tile.style.background = VendorColor(vendor);
            tile.textContent = VendorInitial(vendor);
            tile.title = VendorName(vendor);
            return Raw(tile).W(30.px()).H(30.px()).NoShrink();
        }

        private static bool Matches(ModelPrice m)
        {
            if (string.IsNullOrEmpty(_search)) return true;
            var q = _search.ToLower();
            return m.Name.ToLower().Contains(q)
                || m.Vendor.ToLower().Contains(q)
                || VendorName(m.Vendor).ToLower().Contains(q);
        }

        private static List<ModelPrice> Sort(List<ModelPrice> rows, double inTok, double cachedTok, double outTok)
        {
            IEnumerable<ModelPrice> q;
            switch (_sortCol)
            {
                case "name":    q = rows.OrderBy(m => m.Name); break;
                case "input":   q = rows.OrderBy(m => m.Input); break;
                case "cached":  q = rows.OrderBy(m => m.InputCached ?? double.MaxValue); break;
                case "output":  q = rows.OrderBy(m => m.Output); break;
                default:        q = rows.OrderBy(m => CostOf(m, inTok, cachedTok, outTok)); break;
            }
            var list = q.ToList();
            if (!_sortAsc) list.Reverse();
            return list;
        }

        private static double Val(TextBox t)
        {
            var raw = (t.Text ?? "").Replace(",", "").Trim();
            return double.TryParse(raw, out var d) && d > 0 ? d : 0;
        }

        private static double CostOf(ModelPrice m, double inTok, double cachedTok, double outTok)
        {
            double cachedPrice = m.InputCached ?? m.Input;
            return inTok * m.Input / 1_000_000.0
                 + cachedTok * cachedPrice / 1_000_000.0
                 + outTok * m.Output / 1_000_000.0;
        }

        // ---- formatting ------------------------------------------------------------------

        /// <summary>Per-1M-token price, e.g. $0.15, $0.075, $15.00.</summary>
        private static string Price(double p)
        {
            if (p <= 0) return "$0";
            if (p >= 1) return "$" + p.ToString("#,##0.00");
            var two = p.ToString("0.00");
            if (Math.Abs(double.Parse(two) - p) < 1e-9) return "$" + two;
            var three = p.ToString("0.000");
            if (Math.Abs(double.Parse(three) - p) < 1e-9) return "$" + three;
            return "$" + p.ToString("0.####");
        }

        /// <summary>Cost of the current token mix, e.g. $0.021, $1.05.</summary>
        private static string Money(double v)
        {
            if (v <= 0) return "$0.000";
            if (v < 1)  return "$" + v.ToString("0.000");
            return "$" + v.ToString("#,##0.00");
        }

        /// <summary>Compact number for tick labels / tooltips, e.g. 0.1, 3, 75.</summary>
        private static string TrimNum(double v)
        {
            return v.ToString(v < 1 ? "0.###" : "#,##0.##");
        }

        private static string Fmt(string template, params object[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                var s = args[i] is double d ? d.ToString("0.##") : args[i].ToString();
                template = template.Replace("{" + i + "}", s);
            }
            return template;
        }

        private static string Esc(string s)
        {
            return (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("'", "&#39;").Replace("\"", "&quot;");
        }

        // ---- footer / about ----------------------------------------------------------

        private static IComponent BuildFooter()
        {
            _footer = HStack().WS().AlignItemsCenter().JustifyContent(ItemJustify.Center)
                .Gap(8.px()).Children(BuildFooterContent("Loading..."));
            return _footer;
        }

        private static void UpdateFooterDate()
        {
            _footer.Clear();
            _footer.Add(BuildFooterContent(_updatedAt));
        }

        private static IComponent BuildFooterContent(string date)
        {
            var src = Raw(A(_("lp-link", href: "https://github.com/simonw/llm-prices",
                target: "_blank", text: "simonw/llm-prices")));

            return HStack().AlignItemsCenter().Gap(6.px()).Children(
                TextBlock("Prices last updated: " + date).Tiny().Foreground("var(--lp-muted)"),
                TextBlock("·").Tiny().Foreground("var(--lp-muted)"),
                TextBlock("Data from").Tiny().Foreground("var(--lp-muted)"),
                src);
        }

        private static void ShowAbout()
        {
            var body = VStack().Gap(12.px()).Children(
                TextBlock("Compare Studio is a pricing explorer for large language models. Set your token mix, then click models on the price landscape (or tick them in the table) to compare what a request costs on each.")
                    .Foreground("var(--lp-text)"),
                TextBlock("Pricing data").SemiBold().Foreground("var(--lp-text)"),
                Raw(P(_(styles: s => s.color = "var(--lp-text)"),
                    Span(_(text: "All pricing data comes from the open-source ")),
                    A(_("lp-link", href: "https://github.com/simonw/llm-prices", target: "_blank", text: "simonw/llm-prices")),
                    Span(_(text: " project by Simon Willison, which also powers ")),
                    A(_("lp-link", href: "https://www.llm-prices.com/", target: "_blank", text: "llm-prices.com")),
                    Span(_(text: ".")))),
                Raw(P(_(styles: s => s.color = "var(--lp-muted)"),
                    Span(_(text: "Built by ")),
                    A(_("lp-link", href: "https://curiosity.ai", target: "_blank", text: "curiosity.ai")),
                    Span(_(text: " with Tesserae, a C# UI toolkit compiled to JavaScript.")))));

            Modal("About")
                .LightDismiss()
                .Width(520.px())
                .Content(body)
                .Show();
        }

        // ---- misc --------------------------------------------------------------------

        private static void EnsureViewport()
        {
            document.body.style.overflow = "hidden";
            if (document.head.querySelector("meta[name='viewport']") is null)
            {
                var meta = document.createElement("meta");
                meta["name"]    = "viewport";
                meta["content"] = "width=device-width, initial-scale=1.0, maximum-scale=5.0";
                document.head.appendChild(meta);
            }

            var icon = document.createElement("link");
            icon.setAttribute("rel", "icon");
            icon.setAttribute("type", "image/png");
            icon.setAttribute("href", "favicon.png");
            document.head.appendChild(icon);
        }
    }
}
