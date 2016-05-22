using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CloudFareBypass.Extensions
{
    public static class RegexScraper
    {
        public static List<T> ScrapeFromString<T>(string data, string regexString, Dictionary<string, Func<string, object>> methods = null, RegexOptions options = RegexOptions.None)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (regexString == null)
            {
                throw new ArgumentNullException("regexString");
            }

            List<T> returnValues = new List<T>();
            Regex regex = new Regex(regexString, options, TimeSpan.FromSeconds(10));

            List<string> groupNames = new List<string>();
            int totalGroups = regex.GetGroupNames().Length;
            Type objectType = typeof(T);
            PropertyInfo[] objectProperties = objectType.GetProperties();

            foreach (string group in regex.GetGroupNames())
            {
                int num = 0;

                if (!Int32.TryParse(group, out num))
                {
                    groupNames.Add(group);
                }
            }

            if (groupNames.Count == 0)
            {
                throw new ArgumentException("Named groups are required", "regexString");
            }

            if (!objectType.IsPrimitive && objectType != typeof(string))
            {
                foreach (string groupName in groupNames)
                {
                    PropertyInfo pInfo = objectType.GetProperty(groupName);

                    if (pInfo == null)
                    {
                        throw new FormatException(String.Format("Property {0} not found", groupName));
                    }

                    if (pInfo.PropertyType == typeof(bool))
                    {
                        if (methods == null || !methods.ContainsKey(groupName))
                        {
                            throw new ArgumentException("All bool properties must contain a method to determine true or false", "boolMethods");
                        }
                    }
                }
            }
            else if ((objectType.IsPrimitive || objectType == typeof(string)) && groupNames.Count != 1)
            {
                throw new ArgumentException("A primitive datatype or string can only have a single capture group", "regexString");
            }

            MatchCollection matches = regex.Matches(data);

            foreach (Match match in matches)
            {

                if (objectType.IsPrimitive)
                {
                    T capturedMatch = (T)Convert.ChangeType(match.Groups[1].Value, typeof(T));

                    returnValues.Add(capturedMatch);
                }
                else if (objectType == typeof(string))
                {
                    T capturedMatch = (T)(object)match.Groups[1].Value;

                    returnValues.Add(capturedMatch);
                }
                else
                {
                    T capturedMatch = Activator.CreateInstance<T>();

                    foreach (string group in groupNames)
                    {
                        PropertyInfo property = objectType.GetProperty(group);
                        object result = null;

                        if (property.PropertyType == typeof(bool) || (methods != null && methods.ContainsKey(group)))
                        {
                            result = methods[group](match.Groups[group].Value);
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(match.Groups[group].Value))
                            {
                                if (property.PropertyType == typeof(string))
                                {
                                    result = String.Empty;
                                }
                                else
                                {
                                    result = Activator.CreateInstance(property.PropertyType);
                                }
                            }
                            else
                            {
                                result = Convert.ChangeType(match.Groups[group].Value, property.PropertyType);
                            }
                        }

                        objectType.GetProperty(group).SetMethod.Invoke(capturedMatch, new object[] { result });
                    }

                    returnValues.Add(capturedMatch);
                }
            }

            return returnValues;
        }
    }
}
