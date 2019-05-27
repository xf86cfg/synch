using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace synch
{
    public class AsterConfParser
    {
        public List<Dictionary<string, string>> ParseConfigAttributes(Dictionary<int, string> rawCategories, Dictionary<string, string> rawAttributes)
        {
            var confLineStart = "line-";
            var rawIndexLength = 6;
            var parsedCategories = new Dictionary<string, Dictionary<string, string>>();

            foreach (var rawCategory in rawCategories)
            {
                var categoryName = rawCategory.Value;
                parsedCategories[categoryName] = new Dictionary<string, string>()
                {
                    { "categoryname", categoryName }
                };
            }

            foreach (var rawAttribute in rawAttributes)
            {
                if (!rawAttribute.Key.Contains(confLineStart)) continue;
                try
                {
                    var index = int.Parse(rawAttribute.Key.Substring(confLineStart.Length, rawIndexLength));
                    var categoryName = rawCategories[index];
                    var category = parsedCategories[categoryName];
                    var categoryAttributes = rawAttribute.Value.Split("=");
                    var key = categoryAttributes[0].Trim();
                    var value = categoryAttributes[1].Trim();
                    category[key] = value;
                }
                catch (Exception e)
                {
                    throw new FormatException($"Parsing exception occured {e.Message}", e);
                }
            }
            return parsedCategories.Values.ToList();
        }

        public List<Dictionary<string, string>> ParseConfigAttributes(Dictionary<int, string> rawCategories, Dictionary<string, string> rawAttributes, KeyValuePair<string, string> filter)
        {
            var parsedResult = ParseConfigAttributes(rawCategories, rawAttributes);
            return parsedResult.Where(cat => cat.ContainsKey(filter.Key) && Regex.Match(cat[filter.Key], filter.Value).Success).ToList();
        }
    }
}
