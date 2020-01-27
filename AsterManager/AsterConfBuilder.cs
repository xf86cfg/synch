using System;
using System.Collections.Generic;
using System.Linq;

namespace synch
{
    public class AsterConfBuilder
    {
        public List<string> BuildConfig(List<Dictionary<string, string>> parsedConfig)
        {
            var config = new List<string>();
            try
            {
                foreach (var category in parsedConfig)
                {
                    foreach (var confLine in category)
                    {
                        if (confLine.Key == "categoryname")
                        {
                            config.Add($"[{confLine.Value}]");
                        }
                        config.Add($"{confLine.Key}={confLine.Value}");
                    }
                    config.Add("");
                }
                
            }
            catch (Exception e)
            {
                throw new FormatException($"Exception occured while processing {e.Message}", e);
            }
            return config;
        }

        public List<string> BuildRoutingConfig(List<Dictionary<string, string>> parsedConfig)
        {
            var config = new List<string>();
            try
            {
                foreach (var category in parsedConfig)
                {
                    foreach (var confLine in category)
                    {
                        if (confLine.Key == "categoryname")
                        {
                            config.Add($"[{confLine.Value}]");
                            config.Add($"exten => {confLine.Value},1,Dial(SIP/{confLine.Value})");
                            config.Add("");
                        }
                    }
                }

            }
            catch (Exception e)
            {
                throw new FormatException($"Exception occured while processing {e.Message}", e);
            }
            return config;
        }
    }
}
