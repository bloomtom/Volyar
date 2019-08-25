using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Volyar
{
    public class MediaManagerQuery
    {
        public int? ID { get; private set; } = null;
        public string LibraryName { get; private set; } = null;
        public string SeriesName { get; private set; } = null;
        public string EpisodeName { get; private set; } = null;
        public string GeneralQuery { get; private set; } = null;

        public MediaManagerQuery(string query)
        {
            // Globals for tracking forward state
            List<string> components = new List<string>();
            var currentComponent = new System.Text.StringBuilder();
            bool escape = false;

            // Commits the given string builder to components.
            var commitComponent = new Action<System.Text.StringBuilder>((component) =>
            {
                string output = component.ToString();
                if (!string.IsNullOrWhiteSpace(output))
                {
                    components.Add(output);
                }
                component.Clear();
            });

            // Do a single pass parse and pull out groups into components.
            foreach (char c in query)
            {
                if (escape)
                {
                    escape = false;
                    currentComponent.Append(c);
                }
                else if (c == '\\')
                {
                    escape = true;
                }
                else if (c == ' ')
                {
                    commitComponent(currentComponent);
                }
                else
                {
                    currentComponent.Append(c);
                }
            }
            commitComponent(currentComponent);

            // Parse components for tags and the general query.
            currentComponent.Clear();
            foreach (var component in components)
            {
                if (component.Contains(':'))
                {
                    var tag = new string[2];
                    int splitPoint = component.IndexOf(':');
                    tag[0] = component.Substring(0, splitPoint);
                    tag[1] = component.Substring(splitPoint + 1);

                    switch (tag[0].ToLower())
                    {
                        case "id":
                        case "mediaid":
                            int.TryParse(component.Substring(3), out int id);
                            ID = id;
                            break;
                        case "library":
                            LibraryName = tag[1];
                            break;
                        case "series":
                            SeriesName = tag[1];
                            break;
                        case "episode":
                        case "episodetitle":
                            EpisodeName = tag[1];
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    currentComponent.Append(component + " ");
                }
            }
            GeneralQuery = currentComponent.ToString();
            currentComponent.Clear();
            if (string.IsNullOrWhiteSpace(GeneralQuery)) { GeneralQuery = null; }
            else
            {
                if (GeneralQuery[GeneralQuery.Length - 1] == ' ')
                {
                    GeneralQuery = GeneralQuery.Substring(0, GeneralQuery.Length - 1);
                }
            }
        }
    }
}
