using static H5.Core.dom;

namespace LlmPrices
{
    /// <summary>
    /// Injects the page-level CSS (theme variables, navbar, cards, chart, table styling).
    /// Tesserae components are used for structure; this stylesheet gives the site its
    /// "Compare Studio" look.
    /// </summary>
    public static class Styles
    {
        public static void Inject()
        {
            var style = document.createElement("style");
            style.innerHTML = Css;
            document.head.appendChild(style);
        }

        private const string Css = @"
:root {
    --lp-bg:         #f0f1f4;
    --lp-card:       #ffffff;
    --lp-text:       #18213a;
    --lp-muted:      #69758b;
    --lp-border:     #e6e9f0;
    --lp-accent:     #2f6fed;
    --lp-accent-soft:#eaf1ff;
    --lp-navy:       #0d1d4e;
    --lp-row-hover:  #f5f7fb;
    --lp-selected:   #e9f1ff;
    --lp-good:       #16a34a;
    --lp-shadow:     0 1px 2px rgba(16,24,52,.06), 0 6px 20px rgba(16,24,52,.05);
    --lp-mono:       ui-monospace, SFMono-Regular, Menlo, Consolas, 'Liberation Mono', monospace;
}

html, body {
    margin: 0;
    padding: 0;
    background: var(--lp-bg);
    color: var(--lp-text);
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
}

.lp-card {
    background: var(--lp-card);
    border: 1px solid var(--lp-border);
    border-radius: 12px;
    box-shadow: var(--lp-shadow);
    padding:8px 16px;
}

.lp-card-title {
    font-size: 19px;
    font-weight: 700;
    color: var(--lp-text);
}

/* ---- navbar ---- */
.lp-nav {
    background: linear-gradient(90deg, #0b193f 0%, var(--lp-navy) 60%, #122a66 100%);
    padding-top: 16px;
    padding-bottom: 16px;
}
.lp-nav-logo { border-radius: 8px; }
.lp-nav-title {
    color: #fff;
    font-size: 19px;
    font-weight: 700;
    letter-spacing: .2px;
}
.lp-nav-sub { color: rgba(255,255,255,.55); font-size: 14px; }
.lp-nav-by  { color: rgba(255,255,255,.65); font-size: 14px; }
.lp-nav-brand { color: #fff; font-weight: 700; text-decoration: none; }
.lp-nav-brand:hover { text-decoration: underline; }
.lp-nav-link {
    color: rgba(255,255,255,.85);
    font-size: 14px;
    font-weight: 600;
    cursor: pointer;
}
.lp-nav-link:hover { color: #fff; }

/* ---- chart ---- */
.lp-chart { width: 100%; }
.lp-svg { width: 100%; height: auto; display: block; }
.lp-chart-empty { padding: 60px 0; text-align: center; color: var(--lp-muted); }
.lp-grid { stroke: #edeff4; stroke-width: 1; }
.lp-tick { fill: var(--lp-muted); font-size: 13px; font-family: var(--lp-mono); }
.lp-axis { fill: var(--lp-muted); font-size: 13px; font-weight: 600; }
.lp-best-label {
    fill: var(--lp-good);
    font-size: 13px;
    font-weight: 800;
    letter-spacing: .8px;
    paint-order: stroke;
    stroke: #fff;
    stroke-width: 4px;
    stroke-linejoin: round;
    pointer-events: none;
}
.lp-pt { cursor: pointer; transition: fill-opacity .12s; }
.lp-pt:hover { fill-opacity: 1; stroke: #fff; stroke-width: 1.5; }
.lp-pt-label {
    fill: var(--lp-text);
    font-size: 14px;
    font-weight: 600;
    paint-order: stroke;
    stroke: #fff;
    stroke-width: 4px;
    stroke-linejoin: round;
    pointer-events: none;
}

.lp-legend { display: flex; flex-wrap: wrap; gap: 6px 18px; padding-top: 4px; }
.lp-legend-item {
    display: inline-flex; align-items: center; gap: 7px;
    color: var(--lp-muted); font-size: 13px; font-weight: 500;
}
.lp-legend-dot { width: 10px; height: 10px; border-radius: 50%; display: inline-block; }

/* ---- compute panel ---- */
.lp-field-label {
    color: var(--lp-muted);
    font-size: 11px;
    font-weight: 700;
    letter-spacing: .8px;
}
.lp-input input, .lp-input .tss-textbox {
    border-radius: 8px !important;
    font-family: var(--lp-mono);
    font-size: 14px;
}
.lp-tray-title {
    color: var(--lp-muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: .8px;
}
.lp-clear-link { color: var(--lp-accent); font-size: 13px; font-weight: 600; cursor: pointer; }
.lp-clear-link:hover { text-decoration: underline; }
.lp-tray-empty { background: var(--lp-accent-soft); border-radius: 10px; }
.lp-tray-item { cursor: pointer; border-radius: 10px; padding: 8px; margin: -8px; transition: background .12s; }
.lp-tray-item:hover { background: var(--lp-row-hover); }
.lp-tray-cost { font-family: var(--lp-mono); font-weight: 700; font-size: 14px; color: var(--lp-text); }
.lp-tray-note { color: var(--lp-muted); font-size: 13px; padding-top: 4px; }
.lp-tray-note b { font-weight: 700; }

.lp-bar { height: 6px; border-radius: 999px; background: #eef1f6; overflow: hidden; }
.lp-bar-fill {
    height: 100%;
    border-radius: 999px;
    background: linear-gradient(90deg, #7aa5f8, var(--lp-accent));
    transition: width .25s;
}
.lp-bar-best { background: linear-gradient(90deg, #4ade80, var(--lp-good)); }

/* ---- vendor tiles ---- */
.lp-tile {
    width: 30px; height: 30px;
    border-radius: 8px;
    display: inline-flex; align-items: center; justify-content: center;
    color: #fff;
    font-size: 11px;
    font-weight: 800;
    letter-spacing: .3px;
    user-select: none;
}

/* ---- table ---- */
.lp-thead { border-bottom: 1px solid var(--lp-border); }
.lp-th {
    cursor: pointer;
    user-select: none;
    color: var(--lp-muted);
    font-size: 12px;
    font-weight: 700;
    letter-spacing: .6px;
    transition: color .15s;
}
.lp-th:hover { color: var(--lp-text); }
.lp-th-active { color: var(--lp-accent); }

.lp-row {
    border-bottom: 1px solid var(--lp-border);
    cursor: pointer;
    transition: background .12s;
}
.lp-row:hover { background: var(--lp-row-hover); }
.lp-row-selected { background: var(--lp-selected) !important; }
.lp-row:last-child { border-bottom: none; }

.lp-cell { font-family: var(--lp-mono); font-size: 14px; color: var(--lp-text); }
.lp-blended { font-weight: 700; }
.lp-good { color: var(--lp-good) !important; }

.lp-cb {
    width: 18px; height: 18px;
    border: 2px solid #cdd5e1;
    border-radius: 5px;
    display: inline-flex; align-items: center; justify-content: center;
    color: #fff; font-size: 12px; line-height: 1;
    background: #fff;
    transition: background .12s, border-color .12s;
}
.lp-cb-on { background: var(--lp-accent); border-color: var(--lp-accent); }

a.lp-link, .lp-link { color: var(--lp-accent); text-decoration: none; }
a.lp-link:hover { text-decoration: underline; }
";
    }
}
