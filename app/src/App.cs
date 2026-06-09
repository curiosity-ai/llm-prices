using System;
using System.Collections.Generic;
using System.Linq;
using H5;
using Tesserae;
using static H5.Core.dom;
using static Tesserae.UI;

namespace LlmPrices
{
    /// <summary>
    /// A Tesserae (C# -> JS via h5) rebuild of the llm-prices.com pricing calculator.
    /// Compiles to the repository root as index.html + app.js, reading the generated
    /// current-v1.json for its data. Pricing data originates from
    /// https://github.com/simonw/llm-prices.
    /// </summary>
    internal static class App
    {
        // ---- state -------------------------------------------------------------------
        private static readonly List<ModelPrice> _all       = new List<ModelPrice>();
        private static readonly HashSet<string>  _selected  = new HashSet<string>();
        private static string                    _search    = "";
        private static string                    _sortCol   = "input"; // "name" | "input" | "output"
        private static bool                      _sortAsc   = true;
        private static bool                      _onlySel   = false;
        private static string                    _updatedAt = "";

        // ---- mutable containers (rebuilt on change) ----------------------------------
        private static Stack _compareHolder;
        private static Stack _headerHolder;
        private static Stack _rowsScroll;
        private static Stack _results;

        // token inputs
        private static TextBox _inTok;
        private static TextBox _cachedTok;
        private static TextBox _outTok;

        private static void Main()
        {
            EnsureViewport();
            Styles.Inject();
            document.title = "LLM pricing calculator";

            // start in light mode (matches the gradient background)
            Theme.Light();

            MountToBody(BuildPage());

            PriceData.Load((models, updatedAt) =>
            {
                _all.Clear();
                if (models != null) _all.AddRange(models);
                _updatedAt = updatedAt ?? "unknown";
                RenderTable();
                RenderResults();
                UpdateFooterDate();
            });
        }

        // ---- page shell --------------------------------------------------------------

        private static IComponent BuildPage()
        {
            _compareHolder = VStack().WS();
            _headerHolder  = VStack().WS();
            _rowsScroll    = VStack().WS().Class("lp-scroll").Style(s => s.maxHeight = "62vh");
            _results       = VStack().WS().Gap(8.px());

            var content = VStack()
                .WS()
                .MaxWidth(1180.px())
                .Padding(24.px())
                .Gap(20.px())
                .Style(s => { s.marginLeft = "auto"; s.marginRight = "auto"; })
                .Children(
                    BuildHeader(),
                    BuildBody(),
                    BuildFooter());

            return VStack().S().ScrollY().Children(content);
        }

        private static IComponent BuildHeader()
        {
            var logo = Raw(Div(_("lp-logo"))).Do(r => r.Render().textContent = "$");

            var titleBlock = VStack().Children(
                TextBlock("LLM Prices").XLarge().SemiBold().Foreground("var(--lp-text)"),
                TextBlock("Compare token pricing across providers").Small().Foreground("var(--lp-muted)"));

            var about = Button("About").Class("lp-ghost-btn").OnClick(ShowAbout);

            var theme = Button("🌙").Class("lp-ghost-btn").Tooltip("Toggle dark mode")
                .OnClick(() =>
                {
                    var dark = document.body.classList.toggle("lp-dark");
                    if (dark) Theme.Dark(); else Theme.Light();
                });

            return HStack().WS().AlignItemsCenter().Gap(14.px()).Children(
                logo,
                titleBlock,
                Raw().Grow(),
                theme,
                about);
        }

        private static IComponent BuildBody()
        {
            var calc  = BuildCalculator();
            var table = BuildTable();

            if (Theme.IsMobileMode)
            {
                return VStack().WS().Gap(20.px()).Children(calc.WS(), table.WS());
            }

            return HStack().WS().Gap(20.px()).AlignItems(ItemAlign.Start).Children(
                calc.W(360.px()).NoShrink(),
                table.Grow());
        }

        // ---- calculator --------------------------------------------------------------

        private static IComponent BuildCalculator()
        {
            _inTok     = NumberInput("0");
            _cachedTok = NumberInput("0");
            _outTok    = NumberInput("0");

            return VStack().Class("lp-card").WS().Padding(20.px()).Gap(12.px()).Children(
                TextBlock("Cost calculator").Medium().SemiBold().Foreground("var(--lp-text)"),
                Field("Input tokens", _inTok),
                Field("Cached input tokens", _cachedTok),
                Field("Output tokens", _outTok),
                _results,
                TextBlock("Tip: pick one or more models in the table to compare their cost. Different models use different tokenizers, so token-based comparisons are approximate.")
                    .Tiny().Foreground("var(--lp-muted)"));
        }

        private static IComponent Field(string label, TextBox box)
        {
            box.OnInput((s, e) => RenderResults());
            return VStack().WS().Gap(4.px()).Children(
                TextBlock(label).Small().Foreground("var(--lp-muted)"),
                box.WS());
        }

        private static TextBox NumberInput(string initial)
        {
            return TextBox(initial).Class("lp-input").Do(t =>
            {
                t.InnerElement.type = "number";
                t.InnerElement.min  = "0";
            });
        }

        private static void RenderResults()
        {
            _results.Clear();

            if (_selected.Count == 0)
            {
                _results.Add(VStack().WS().Class("lp-result").Padding(14.px()).Children(
                    TextBlock("Select models from the table to see their estimated cost here.")
                        .Small().Foreground("var(--lp-muted)")));
                return;
            }

            double inTok     = Val(_inTok);
            double cachedTok = Val(_cachedTok);
            double outTok    = Val(_outTok);

            var rows = _all
                .Where(m => _selected.Contains(m.Id))
                .Select(m => new { Model = m, Cost = CostOf(m, inTok, cachedTok, outTok) })
                .OrderBy(x => x.Cost)
                .ToList();

            var inner = VStack().WS().Class("lp-result").Padding(14.px()).Gap(8.px());
            inner.Add(TextBlock("Estimated cost").Small().SemiBold().Foreground("var(--lp-muted)"));

            bool first = true;
            foreach (var r in rows)
            {
                var name = TextBlock(r.Model.Name).Small().Foreground("var(--lp-text)");
                var cost = TextBlock(Money(r.Cost)).Small().SemiBold();
                if (first) { cost.Class("lp-cheapest"); first = false; }
                else       { cost.Foreground("var(--lp-text)"); }

                inner.Add(HStack().WS().AlignItemsCenter().Gap(8.px()).Children(
                    Logo(r.Model.Vendor).W(20.px()).H(20.px()),
                    name.Grow(),
                    cost));
            }

            _results.Add(inner);
        }

        // ---- table -------------------------------------------------------------------

        private static IComponent BuildTable()
        {
            var search = SearchBox("Search models or providers...")
                .WS()
                .SearchAsYouType()
                .OnSearch((s, term) => { _search = term ?? ""; RenderTable(); });

            return VStack().Class("lp-card").WS().Padding(20.px()).Gap(12.px()).Children(
                HStack().WS().AlignItemsCenter().Children(
                    TextBlock("Model prices").Medium().SemiBold().Foreground("var(--lp-text)").Grow(),
                    TextBlock("per 1M tokens").Small().Foreground("var(--lp-muted)")),
                search,
                _compareHolder,
                _headerHolder,
                _rowsScroll);
        }

        private static void RenderTable()
        {
            RenderCompareBar();
            RenderHeader();

            _rowsScroll.Clear();

            var rows = _all
                .Where(m => !_onlySel || _selected.Contains(m.Id))
                .Where(Matches)
                .ToList();

            rows = Sort(rows);

            if (rows.Count == 0)
            {
                _rowsScroll.Add(VStack().WS().Padding(24.px()).Children(
                    TextBlock(_all.Count == 0 ? "Loading prices..." : "No models match your search.")
                        .Small().Foreground("var(--lp-muted)").TextCenter()));
                return;
            }

            foreach (var m in rows)
            {
                _rowsScroll.Add(Row(m));
            }
        }

        private static void RenderCompareBar()
        {
            _compareHolder.Clear();
            if (_selected.Count == 0)
            {
                _onlySel = false;
                return;
            }

            var only = CheckBox("Show only selected (" + _selected.Count + ")")
                .Checked(_onlySel)
                .OnChange((s, e) => { _onlySel = s.IsChecked; RenderTable(); });

            var clear = Button("Clear").Class("lp-ghost-btn").OnClick(() =>
            {
                _selected.Clear();
                _onlySel = false;
                RenderTable();
                RenderResults();
            });

            _compareHolder.Add(HStack().WS().AlignItemsCenter().Gap(12.px()).Children(
                only.Grow(),
                clear));
        }

        private static void RenderHeader()
        {
            _headerHolder.Clear();

            _headerHolder.Add(HStack().WS().Class("lp-thead").AlignItemsCenter().Gap(10.px())
                .PaddingTop(6.px()).PaddingBottom(10.px()).PaddingLeft(8.px()).PaddingRight(8.px()).Children(
                    Th("Model", "name").Grow(),
                    Th("Input" + Arrow("input"), "input").W(120.px()).TextRight(),
                    Th("Output" + Arrow("output"), "output").W(96.px()).TextRight(),
                    TextBlock("").W(44.px())));
        }

        private static TextBlock Th(string label, string col)
        {
            var tb = TextBlock(label).Small()
                .Class(_sortCol == col ? "lp-th lp-th-active" : "lp-th")
                .PaddingTop(8.px()).PaddingBottom(8.px()).PaddingLeft(10.px()).PaddingRight(10.px());
            tb.OnClick((s, e) =>
            {
                if (_sortCol == col) { _sortAsc = !_sortAsc; }
                else { _sortCol = col; _sortAsc = true; }
                RenderTable();
            });
            return tb;
        }

        private static string Arrow(string col) => _sortCol == col ? (_sortAsc ? "  ↑" : "  ↓") : "";

        private static IComponent Row(ModelPrice m)
        {
            bool sel = _selected.Contains(m.Id);

            var name = VStack().Gap(3.px()).Grow().Children(
                TextBlock(m.Name).Small().SemiBold().Foreground("var(--lp-text)"),
                HStack().Children(TextBlock(m.Vendor).Class("lp-vendor-chip")));

            var inputCol = VStack().W(120.px()).Children(
                TextBlock(Price(m.Input)).Small().Class("lp-price-in").TextRight());
            if (m.InputCached.HasValue)
            {
                inputCol.Add(TextBlock("cached " + Price(m.InputCached.Value)).Class("lp-cached").TextRight());
            }

            var outputCol = TextBlock(Price(m.Output)).Small().Class("lp-price-out").W(96.px()).TextRight();

            var check = TextBlock(sel ? "✓" : "").Class(sel ? "lp-check lp-check-on" : "lp-check");

            var row = HStack()
                .Class(sel ? "lp-row lp-row-selected" : "lp-row")
                .WS().AlignItemsCenter().Gap(10.px())
                .Padding(10.px())
                .Children(
                    Logo(m.Vendor),
                    name,
                    inputCol,
                    outputCol,
                    HStack().W(44.px()).Children(check));

            row.Do(r => r.Render().addEventListener("click", e => Toggle(m.Id)));
            return row;
        }

        private static void Toggle(string id)
        {
            if (!_selected.Remove(id)) _selected.Add(id);
            RenderTable();
            RenderResults();
        }

        // ---- helpers -----------------------------------------------------------------

        private static Image Logo(string vendor)
        {
            return Image("assets/img/logos/" + vendor + ".svg", "assets/img/logos/generic.svg")
                .Class("lp-logo-img").W(26.px()).H(26.px());
        }

        private static bool Matches(ModelPrice m)
        {
            if (string.IsNullOrEmpty(_search)) return true;
            var q = _search.ToLower();
            return m.Name.ToLower().Contains(q) || m.Vendor.ToLower().Contains(q);
        }

        private static List<ModelPrice> Sort(List<ModelPrice> rows)
        {
            IEnumerable<ModelPrice> q;
            switch (_sortCol)
            {
                case "name":   q = rows.OrderBy(m => m.Name); break;
                case "output": q = rows.OrderBy(m => m.Output); break;
                default:       q = rows.OrderBy(m => m.Input); break;
            }
            var list = q.ToList();
            if (!_sortAsc) list.Reverse();
            return list;
        }

        private static double Val(TextBox t)
        {
            return double.TryParse(t.Text, out var d) && d > 0 ? d : 0;
        }

        private static double CostOf(ModelPrice m, double inTok, double cachedTok, double outTok)
        {
            double cachedPrice = m.InputCached ?? m.Input;
            return inTok * m.Input / 1_000_000.0
                 + cachedTok * cachedPrice / 1_000_000.0
                 + outTok * m.Output / 1_000_000.0;
        }

        private static string Price(double p)
        {
            if (p <= 0) return "$0";
            return "$" + (p >= 1 ? p.ToString("0.00") : p.ToString("0.####"));
        }

        private static string Money(double v)
        {
            if (v <= 0)      return "$0.00";
            if (v < 0.01)    return "$" + v.ToString("0.######");
            if (v < 1)       return "$" + v.ToString("0.####");
            if (v < 1000)    return "$" + v.ToString("0.00");
            return "$" + v.ToString("#,##0.00");
        }

        // ---- footer / about ----------------------------------------------------------

        private static Stack _footer;

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
                TextBlock("This is a pricing calculator for large language models. Enter your expected token usage and pick one or more models to compare their cost per request.")
                    .Foreground("var(--lp-text)"),
                TextBlock("Pricing data").SemiBold().Foreground("var(--lp-text)"),
                Raw(P(_(styles: s => s.color = "var(--lp-text)"),
                    Span(_(text: "All pricing data comes from the open-source ")),
                    A(_("lp-link", href: "https://github.com/simonw/llm-prices", target: "_blank", text: "simonw/llm-prices")),
                    Span(_(text: " project by Simon Willison, which also powers ")),
                    A(_("lp-link", href: "https://www.llm-prices.com/", target: "_blank", text: "llm-prices.com")),
                    Span(_(text: ".")))),
                TextBlock("Built with Tesserae, a C# UI toolkit compiled to JavaScript.")
                    .Small().Foreground("var(--lp-muted)"));

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
