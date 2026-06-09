using System;
using System.Collections.Generic;
using H5;
using H5.Core;

namespace LlmPrices
{
    /// <summary>
    /// A single model's current pricing, mirroring an entry of the "prices" array in
    /// current-v1.json (see https://github.com/simonw/llm-prices).
    /// </summary>
    public sealed class ModelPrice
    {
        public string  Id          { get; set; }
        public string  Vendor      { get; set; }
        public string  Name        { get; set; }
        public double  Input       { get; set; }
        public double  Output      { get; set; }
        public double? InputCached { get; set; }
    }

    /// <summary>
    /// Loads and parses the generated current-v1.json file that sits next to index.html.
    /// </summary>
    public static class PriceData
    {
        /// <summary>
        /// Fetches current-v1.json and parses it. On success calls <paramref name="onLoaded"/>
        /// with the parsed prices and the "updated_at" date; on failure calls it with (null, null).
        /// </summary>
        public static void Load(Action<List<ModelPrice>, string> onLoaded)
        {
            Action<string> onText = json =>
            {
                if (string.IsNullOrEmpty(json))
                {
                    onLoaded(null, null);
                    return;
                }

                try
                {
                    var data      = es5.JSON.parse(json);
                    var pricesArr = data["prices"].As<object[]>();
                    var updatedAt = data["updated_at"].As<string>();

                    var result = new List<ModelPrice>();

                    foreach (var p in pricesArr)
                    {
                        var cached = p["input_cached"];

                        result.Add(new ModelPrice
                        {
                            Id          = p["id"].As<string>(),
                            Vendor      = p["vendor"].As<string>(),
                            Name        = p["name"].As<string>(),
                            Input       = p["input"].As<double>(),
                            Output      = p["output"].As<double>(),
                            InputCached = cached is null ? (double?)null : cached.As<double>()
                        });
                    }

                    onLoaded(result, updatedAt);
                }
                catch (Exception)
                {
                    onLoaded(null, null);
                }
            };

            // Fetch the JSON file that lives next to this page. Done via a tiny JS bridge so we
            // don't depend on a specific h5 Promise binding.
            Script.Write(@"
                fetch('current-v1.json')
                    .then(function (r) { return r.text(); })
                    .then(function (t) { {0}(t); })
                    .catch(function (e) { {0}(null); });
            ", onText);
        }
    }
}
