using static H5.Core.dom;

namespace LlmPrices
{
    /// <summary>
    /// Injects the page-level CSS (theme variables, card / table styling, responsive rules).
    /// Tesserae components are used for structure; this stylesheet gives the site its look,
    /// loosely inspired by llmpricecheck.com.
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
    --lp-bg-top:     #eef2fb;
    --lp-bg-bottom:  #f6f8fc;
    --lp-card:       #ffffff;
    --lp-text:       #1a2230;
    --lp-muted:      #66758c;
    --lp-border:     #e6eaf1;
    --lp-accent:     #4f6bed;
    --lp-accent-soft:#eaf0ff;
    --lp-row-hover:  #f3f6fc;
    --lp-selected:   #eaf0ff;
    --lp-good:       #1f9d57;
    --lp-shadow:     0 1px 3px rgba(20,30,60,.08), 0 8px 24px rgba(20,30,60,.06);
    --lp-chip:       #f1f4fa;
}

body.lp-dark {
    --lp-bg-top:     #0b0e15;
    --lp-bg-bottom:  #11151f;
    --lp-card:       #161b25;
    --lp-text:       #e6ebf2;
    --lp-muted:      #8b97a8;
    --lp-border:     #232a38;
    --lp-accent:     #6b86ff;
    --lp-accent-soft:#1f2740;
    --lp-row-hover:  #1c2230;
    --lp-selected:   #1f2740;
    --lp-good:       #3ecb7e;
    --lp-shadow:     0 1px 3px rgba(0,0,0,.4), 0 8px 24px rgba(0,0,0,.35);
    --lp-chip:       #1c2230;
}

html, body {
    margin: 0;
    padding: 0;
    background: linear-gradient(180deg, var(--lp-bg-top) 0%, var(--lp-bg-bottom) 320px, var(--lp-bg-bottom) 100%);
    color: var(--lp-text);
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
}

.lp-shell { max-width: 1180px; }

.lp-card {
    background: var(--lp-card);
    border: 1px solid var(--lp-border);
    border-radius: 16px;
    box-shadow: var(--lp-shadow);
}

/* Header */
.lp-logo {
    width: 38px; height: 38px;
    border-radius: 10px;
    background: linear-gradient(135deg, #4f6bed, #8a5cf6);
    color: #fff;
    display: flex; align-items: center; justify-content: center;
    font-weight: 800; font-size: 18px;
}
.lp-ghost-btn .tss-btn, .lp-ghost-btn { border-radius: 10px !important; }

/* Table */
.lp-thead {
    border-bottom: 1px solid var(--lp-border);
}
.lp-th {
    cursor: pointer;
    user-select: none;
    color: var(--lp-muted);
    font-weight: 600;
    border-radius: 8px;
    transition: background .15s, color .15s;
}
.lp-th:hover { background: var(--lp-row-hover); color: var(--lp-text); }
.lp-th-active { color: var(--lp-accent); }

.lp-row {
    border-bottom: 1px solid var(--lp-border);
    cursor: pointer;
    transition: background .12s;
}
.lp-row:hover { background: var(--lp-row-hover); }
.lp-row-selected { background: var(--lp-selected) !important; }
.lp-row:last-child { border-bottom: none; }

.lp-logo-img { width: 26px; height: 26px; border-radius: 6px; object-fit: contain; }

.lp-vendor-chip {
    background: var(--lp-chip);
    color: var(--lp-muted);
    border-radius: 999px;
    padding: 2px 10px;
    font-size: 12px;
    font-weight: 600;
    text-transform: capitalize;
}

.lp-price-in  { color: var(--lp-text); font-variant-numeric: tabular-nums; }
.lp-price-out { color: var(--lp-text); font-variant-numeric: tabular-nums; }
.lp-cached    { color: var(--lp-muted); font-size: 12px; }

.lp-check {
    width: 20px; height: 20px;
    border: 2px solid var(--lp-border);
    border-radius: 6px;
    display: inline-flex; align-items: center; justify-content: center;
    color: #fff; font-size: 13px; line-height: 1;
}
.lp-check-on { background: var(--lp-accent); border-color: var(--lp-accent); }

/* Calculator */
.lp-input input, .lp-input .tss-textbox {
    border-radius: 10px !important;
}
.lp-result {
    background: var(--lp-accent-soft);
    border-radius: 12px;
}
.lp-cheapest {
    color: var(--lp-good);
    font-weight: 700;
}

/* Scroll area for the table body */
.lp-scroll {
    overflow-y: auto;
    overflow-x: hidden;
}
.lp-scroll::-webkit-scrollbar { width: 10px; }
.lp-scroll::-webkit-scrollbar-thumb { background: var(--lp-border); border-radius: 8px; }

a.lp-link, .lp-link { color: var(--lp-accent); text-decoration: none; }
a.lp-link:hover { text-decoration: underline; }

.lp-muted { color: var(--lp-muted); }
";
    }
}
