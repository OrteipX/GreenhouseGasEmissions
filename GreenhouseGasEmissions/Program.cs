/* Program Name: Program.cs
* Date: Jul 20, 2021
*/

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.XPath;

namespace GreenhouseGasEmissions.Main
{
    public enum ReportType
    {
        GHG,
        REGION
    }

    class Program
    {
        private const string XML_FILE = "./ghg-canada.xml";

        static void Main(string[] args)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(XML_FILE);

                Helper helper = new Helper(xmlDoc);

                Ghg ghg = new Ghg();
                Initialization(ref ghg, helper);

                char userChoice;

                do
                {
                    Console.Clear();

                    UI.PrintHeader();
                    userChoice = Console.ReadKey().KeyChar;

                    switch (userChoice)
                    {
                        case 'Y':
                            int yearMin = helper.SetupYearRange().Item1;
                            int yearMax = helper.SetupYearRange().Item2;

                            UI.PrintYearsSelection(ref ghg, yearMin, yearMax);
                            break;
                        case 'R':
                            Dictionary<int, string> regions = helper.SetupRegions();
                            UI.PrintRegionsSelection(ref ghg, regions);

                            Dictionary<string, Dictionary<string, string>> sourcesData = helper.GetDataByRegion(ghg);

                            string regionName = helper.GetRegionName(ghg.regionId);
                            UI.PrintReport(sourcesData, ReportType.REGION, regionName: regionName);

                            break;
                        case 'S':
                            Dictionary<int, string> sources = helper.SetupSources();
                            UI.PrintSourcesSelection(ref ghg, sources);

                            string sourceName = helper.GetSourceName(ghg.sourceId);
                            Dictionary<string, Dictionary<string, string>> regionsData = helper.GetDataBySource(ghg, sourceName);

                            UI.PrintReport(regionsData, ReportType.GHG, sourceName: sourceName);
                            break;
                        case 'X':
                            Console.WriteLine();
                            break;
                        default:
                            Console.WriteLine("\n\nError: Selection not valid!");
                            System.Threading.Thread.Sleep(1000);
                            break;
                    }

                } while (userChoice != 'X');

                UI.PressAnyKey();
                Console.WriteLine("\n\nAll done!\n");
            }
            catch (XmlException ex)
            {
                Console.WriteLine("\nXML ERROR: " + ex.Message);
            }
            catch (XPathException ex)
            {
                Console.WriteLine("\n XPath ERROR: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nERROR: " + ex.Message);
            }
        }

        private static void Initialization(ref Ghg ghg, Helper  helper)
        {
            int yearMin = helper.SetupYearRange().Item1;
            //ghg.yearFrom = yearMin;
            ghg.yearFrom = 2000;

            int yearMax = helper.SetupYearRange().Item2;
            //ghg.yearTo = yearMax;
            ghg.yearTo = 2005;
        }
    }

    public class Helper
    {
        private XPathNavigator m_nav;

        public Helper()
        {
        }

        public Helper(XmlDocument xmlDoc)
        {
            m_nav =  xmlDoc.CreateNavigator();
        }

        public static void ParseData(ref Dictionary<string, string> dic, Ghg ghg)
        {
            Dictionary<string, string> tempDic = dic;

            int yearFrom =  ghg.yearFrom;

            for (int i = 0; i <= (ghg.yearTo - ghg.yearFrom); ++i)
            {
                if (!tempDic.ContainsKey((ghg.yearFrom + i).ToString()))
                    tempDic.Add((ghg.yearFrom + i).ToString(), "-");
            }

            var list = tempDic.Keys.ToList();
            list.Sort();

            dic = new Dictionary<string, string>();

            foreach (var data in list)
                dic.Add(data, tempDic[data]);
        }

        public Tuple<int, int> SetupYearRange()
        {
            XPathNodeIterator node = m_nav.Select("//emissions/@year");
            HashSet<int> yearsRange = new HashSet<int>();

            while (node.MoveNext())
            {
                int temp;
                Int32.TryParse(node.Current.Value, out temp);

                yearsRange.Add(temp);
            }

            int yearMin = GetMinYear(yearsRange);
            int yearMax = GetMaxYear(yearsRange);

            return Tuple.Create(yearMin, yearMax);
        }

        private int GetMinYear(HashSet<int> years)
        {
            int min = Int32.MaxValue;

            foreach (int year in years)
            {
                if (year < min)
                {
                    min = year;
                }
            }

            return min;
        }

        private int GetMaxYear(HashSet<int> years)
        {
            int max = Int32.MinValue;

            foreach (int year in years)
            {
                if (year > max)
                {
                    max = year;
                }
            }

            return max;
        }


        public List<string> GetYears(Dictionary<string, Dictionary<string, string>> data)
        {
            List<string> years = new List<string>();

            foreach (var source in data)
            {
                foreach (var d in source.Value)
                {
                    years.Add(d.Key);
                }

                break;
            }

            return years;
        }

        public int GetMaxString(Dictionary<string, Dictionary<string, string>> data)
        {
            int maxString = Int32.MinValue;

            foreach (var source in data)
            {
                if (source.Key.Length > maxString)
                    maxString = source.Key.Length;
            }

            return maxString;
        }

        public Dictionary<int, string> SetupRegions()
        {
            XPathNodeIterator node = m_nav.Select("//region/@name");
            Dictionary<int, string> regions = new Dictionary<int, string>();

            while(node.MoveNext())
                regions.Add(node.CurrentPosition, node.Current.Value);

            return regions;
        }

        public Dictionary<int, string> SetupSources()
        {
            XPathNodeIterator node = m_nav.Select("//region/source/@description");
            Dictionary<int, string> sources = new Dictionary<int, string>();

            while (node.MoveNext())
            {
                if (!sources.ContainsValue(node.Current.Value))
                {
                    sources.Add(node.CurrentPosition, node.Current.Value);
                }
            }

            return sources;
        }

        public string GetRegionName(int regionId)
        {
            XPathNodeIterator node = m_nav.Select($"//region[{regionId}]/@name");
            node.MoveNext();

            return node.Current.Value;
        }

        public string GetSourceName(int sourceId)
        {
            Dictionary<int, string> sourcesNames = GetSourcesNames();

            return sourcesNames[sourceId];
        }


        private Dictionary<int, string> GetSourcesNames()
        {
            XPathNodeIterator node = m_nav.Select($"//region//@description");
            HashSet<string> sourcesNames = new HashSet<string>();

            while (node.MoveNext())
                sourcesNames.Add(node.Current.Value);

            Dictionary<int, string> sources = new Dictionary<int, string>();

            int counter = 0;
            foreach (var sourceName in sourcesNames)
                sources.Add(++counter, sourceName);

            return sources;
        }

        public Dictionary<string, Dictionary<string, string>> GetDataByRegion(Ghg ghg)
        {
            XPathNodeIterator node = m_nav.Select($"//region[{ghg.regionId}]/source");
            // <Source, <Year, Value>>
            Dictionary<string, Dictionary<string, string>> sources = new Dictionary<string, Dictionary<string, string>>();

            while (node.MoveNext())
            {
                string source = node.Current.GetAttribute("description", "");
                sources.Add(source, new Dictionary<string, string>());
            }

            foreach (var source in sources)
            {
                Dictionary<string, string> sourceData = new Dictionary<string, string>();

                XPathNodeIterator subNode =
                    m_nav.Select($"//region[{ghg.regionId}]/source[@description = \"{source.Key}\"]/emissions[@year >= {ghg.yearFrom} and @year <= {ghg.yearTo}]");

                while (subNode.MoveNext())
                {
                    double temp;
                    string dataValue;

                    if (Double.TryParse(subNode.Current.Value, out temp))
                        dataValue = String.Format("{0,0:#0.000}", temp);
                    else
                        dataValue = "-";

                    sourceData.Add(subNode.Current.GetAttribute("year", ""), dataValue);
                }

                Helper.ParseData(ref sourceData, ghg);
                sources[source.Key] = sourceData;
            }

            return sources;
        }

        public Dictionary<string, Dictionary<string, string>> GetDataBySource(Ghg ghg, string sourceName)
        {
            XPathNodeIterator node = m_nav.Select($"//region");
            // <Region, <Year, Value>>
            Dictionary<string, Dictionary<string, string>> regions = new Dictionary<string, Dictionary<string, string>>();

            while (node.MoveNext())
            {
                string region = node.Current.GetAttribute("name", "");
                regions.Add(region, new Dictionary<string, string>());
            }

            foreach (var region in regions)
            {
                Dictionary<string, string> regionData = new Dictionary<string, string>();

                XPathNodeIterator subNode =
                    m_nav.Select($"//region[@name = \"{region.Key}\"]/source[@description = \"{sourceName}\"]/emissions[@year >= {ghg.yearFrom} and @year <= {ghg.yearTo}]");

                while (subNode.MoveNext())
                {
                    double temp;
                    string dataValue;

                    if (Double.TryParse(subNode.Current.Value, out temp))
                    {
                        //Console.WriteLine($"{subNode.CurrentPosition} - Node value: {subNode.Current.Value}");
                        dataValue = String.Format("{0,0:#0.000}", temp);
                    }
                    else
                        dataValue = "-";

                    regionData.Add(subNode.Current.GetAttribute("year", ""), dataValue);
                }

                Helper.ParseData(ref regionData, ghg);
                regions[region.Key] = regionData;
            }

            return regions;
        }
    }

    public class UI
    {
        public static void PressAnyKey()
        {
            Console.Write("\nPress any key to continue.");
            char dummy = Console.ReadKey().KeyChar;
        }

        public static void PrintHeader()
        {
            Console.Write(
                    "Greenhouse Gas Emissions in Canada"
                    + "\n=================================="
                    + "\n"
                    + "\n\'Y\' to adjust the range of years"
                    + "\n\'R\' to select a region"
                    + "\n\'S\' to select a specific GHG source"
                    + "\n\'X\' to exit the program"
                    + "\nYour selection: "
                    );
        }

        public static void PrintYearsSelection(ref Ghg ghg, int yearMin, int yearMax)
        {
            Console.WriteLine();

            bool valid;

            do
            {
                Console.Write($"\nStarting year (from {yearMin} to {yearMax}): ");

                int temp;

                if (!Int32.TryParse(Console.ReadLine(), out temp)
                        || temp < yearMin
                        || temp > yearMax)
                {
                    valid = false;
                    Console.WriteLine($"ERROR: Starting year must be an integer between {yearMin} and {yearMax}.");
                }
                else
                {
                    ghg.yearFrom = temp;
                    valid = true;
                }

            } while (!valid);

            do
            {
                Console.Write($"\nEnding year (from {ghg.yearFrom} to {yearMax}): ");

                int temp;

                if (!Int32.TryParse(Console.ReadLine(), out temp)
                        || temp < yearMin
                        || temp > yearMax
                        || temp < ghg.yearFrom)
                {
                    valid = false;
                    Console.WriteLine($"ERROR: Starting year must be an integer and between {ghg.yearFrom} and {yearMax}.");
                }
                else
                {
                    ghg.yearTo = temp;
                    valid = true;
                }

            } while (!valid);

            PressAnyKey();
        }

        public static void PrintRegionsSelection(ref Ghg ghg, Dictionary<int, string> regions)
        {
            bool valid;
            int regionMinId = Int32.MaxValue;
            int regionMaxId = Int32.MinValue;

            Console.WriteLine("\n\nSelect a region by number as shown below...");

            foreach (var region in regions)
            {
                if (regionMinId > region.Key)
                    regionMinId = region.Key;

                if (regionMaxId < region.Key)
                    regionMaxId = region.Key;

                Console.WriteLine(String.Format("{0,3}. {1,0}", region.Key, region.Value));
            }

            do
            {
                Console.Write("\nEnter a region #: ");

                int temp;

                if (!Int32.TryParse(Console.ReadLine(), out temp)
                        || temp < regionMinId
                        || temp > regionMaxId)
                {
                    valid = false;
                    Console.WriteLine($"ERROR: Region must be an integer between {regionMinId} and {regionMaxId}.");
                }
                else
                {
                    ghg.regionId = temp;
                    valid = true;
                }

            } while (!valid);
        }

        public static void PrintSourcesSelection(ref Ghg ghg, Dictionary<int, string> sources)
        {
            bool valid;
            int sourceMinId = Int32.MaxValue;
            int sourceMaxId = Int32.MinValue;

            Console.WriteLine("\n\nSelect a source by number as shown below...");

            foreach (var source in sources)
            {
                if (sourceMinId > source.Key)
                    sourceMinId = source.Key;

                if (sourceMaxId < source.Key)
                    sourceMaxId = source.Key;

                Console.WriteLine(String.Format("{0,3}. {1,0}", source.Key, source.Value));
            }

            do
            {
                Console.Write("\nEnter a source #: ");

                int temp;

                if (!Int32.TryParse(Console.ReadLine(), out temp)
                        || temp < sourceMinId
                        || temp > sourceMaxId)
                {
                    valid = false;
                    Console.WriteLine($"ERROR: Region must be an integer between {sourceMinId} and {sourceMaxId}.");
                }
                else
                {
                    ghg.sourceId = temp;
                    valid = true;
                }

            } while (!valid);
        }

        public static void PrintReport(
                Dictionary<string, Dictionary<string, string>> data,
                ReportType type,
                string? regionName = "",
                string? sourceName = ""
                )
        {
            Helper helper = new Helper();

            if (type == ReportType.GHG)
            {
                string header = $"Emissions from {sourceName} (Megatonnes)";
                string dashes = new String('-', header.Length);

                Console.WriteLine($"\n{header}\n{dashes}");

                List<string> years = helper.GetYears(data);
                int truncateCounter = (years.Count() - 1) / 5;

                bool printHeader = true;
                const int basePadding = 55;
                const int middlePadding = 10;

                foreach (var region in data)
                {
                    if (printHeader)
                    {
                        Console.Write($"{"Source", basePadding}");

                        for (int i = 0; i < years.Count(); ++i)
                        {
                            Console.Write($"{years[i], middlePadding}");
                        }

                        printHeader = false;
                        Console.WriteLine();
                    }

                    Console.Write($"\n{region.Key, basePadding}");

                    foreach (var d in region.Value)
                        Console.Write($"{d.Value, middlePadding}");
                }

                Console.WriteLine("\n");

                PressAnyKey();
            }
            else if (type == ReportType.REGION)
            {
                // <Source, <Year, Value>>
                string header = $"Emissions in {regionName} (Megatonnes)";
                string dashes = new String('-', header.Length);

                Console.WriteLine($"\n{header}\n{dashes}");

                List<string> years = helper.GetYears(data);
                int truncateCounter = (years.Count() - 1) / 5;

                bool printHeader = true;
                const int basePadding = 55;
                const int middlePadding = 10;

                foreach (var source in data)
                {
                    if (printHeader)
                    {
                        Console.Write($"{"Source", basePadding}");

                        for (int i = 0; i < years.Count(); ++i)
                        {
                            Console.Write($"{years[i], middlePadding}");
                        }

                        printHeader = false;
                        Console.WriteLine();
                    }

                    Console.Write($"\n{source.Key, basePadding}");

                    foreach (var d in source.Value)
                        Console.Write($"{d.Value, middlePadding}");
                }

                Console.WriteLine("\n");

                PressAnyKey();
            }
        }
    }

    public class Ghg
    {
        public int yearFrom { get; set; } = 0;
        public int yearTo { get; set; } = 0;
        public int regionId { get; set; } = 0;
        public int sourceId { get; set; } = 0;
    }
}
