using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Verse;

namespace TranslatedNames;

public static class TranslationInfo
{
	private static readonly Dictionary<string, string> translations;

	static TranslationInfo()
	{
		translations = new Dictionary<string, string>();
		string[] files = Directory.GetFiles(StaticConstructor.translationPath);
		string[] array = files;
		foreach (string uri in array)
		{
			try
			{
				foreach (XElement item in XDocument.Load(uri).Root.Descendants())
				{
					try
					{
						string key = (string)item.Attribute("name");
						string value = (string)item.Attribute("t-Name");
						if (!translations.ContainsKey(key))
						{
							translations.Add(key, value);
						}
					}
					catch (Exception ex)
					{
						Log.Error(ex.ToString());
					}
				}
			}
			catch (Exception ex2)
			{
				Log.Error(ex2.ToString());
			}
		}
	}

	public static string GetTranslation(string name)
	{
		string result = name;
		string pattern = "[A-Za-z]+";
		if (Regex.IsMatch(name, pattern) && translations.ContainsKey(name))
		{
			result = translations[name];
		}
		return result;
	}
}
