using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace test
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var reader = new System.IO.StreamReader("rules.json"))
            {
                rules = JsonConvert.DeserializeObject<List<Rule>>(reader.ReadToEnd());
            } 
            string buffer;
            Dictionary<string, string> record;
            while ((buffer = Console.In.ReadLine()) != null)
            {
                if ((record = ParseRFC3164(buffer)) != null)
                {
                    foreach (var kvp in record)
                    {
                        Console.Out.WriteLine(kvp.Key + ":" + kvp.Value);
                    }
                    Console.Out.WriteLine();
                }
            }
        }

        static List<Rule> rules;

        static Dictionary<string,string> ParseRFC3164(string recordText)
        {
            var result = new Dictionary<string, string>();
            var header = recordText.Split(new char[] { ' ' });
            result["RawMessage"] = recordText;
            result["Month"] = header[0];
            result["Day"] = header[1];
            result["Time"] = header[2];
            result["Sender"] = header[3];
            var from = 0;
            for (var i = 0; i <= 3; i++)
            {
                from = recordText.IndexOf(' ', from + 1);
            }
            from++;
            var tags = recordText.Substring(from).Split(new string[] { ": " },StringSplitOptions.None).ToList();
            result["Message"] = tags.Last();
            tags.RemoveAt(tags.Count - 1);
            result["Tags"] = string.Join(" ",tags.Select(tag => "(" + tag + ")"));
            Dictionary<string, string> extracted = null;
            return
                rules.Any(rule => 
                {
                    extracted = rule.Apply(tags, result["Message"]);
                    return extracted != null;
                }) 
                ? extracted.Concat(result).ToDictionary(pair => pair.Key, pair => pair.Value)
                : null;
        }
    }

    public class Rule
    {
        public List<Condition> messages { get; set; }
        public List<Condition> tags { get; set; }

        public Rule(List<Condition> messages, List<Condition> tags)
        {
            this.messages = messages;
            this.tags = tags;
        }
        public Dictionary<string,string> Apply(List<string> tagsText, string messageText)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            return
                tags.All(condition =>
                    tagsText.Any(tag =>
                    {
                        var match = condition._body.Match(tag);
                        if (match.Success && condition.extract)
                        {
                            result[condition.name] = match.Groups[1].Value;
                        }
                        return match.Success;
                    })) &&
                messages.All(condition =>
                    {
                        var match = condition._body.Match(messageText);
                        if (match.Success && condition.extract)
                        {
                            result[condition.name] = match.Groups[1].Value;
                        }
                        return match.Success;
                    }) 
                    ? result
                    : null;
                
        }
    }

    public class Condition
    {
        public string name { get; set; }
        public string body {
            get
            {
                return _body.ToString();
            }
            set
            {
                _body = new Regex(value);
            }
        }
        public Regex _body;
        public bool extract { get; set; }

        public Condition(string name, string body, bool extract)
        {
            this.name = name;
            this.body = body;
            this.extract = extract;
        }
    }
}
