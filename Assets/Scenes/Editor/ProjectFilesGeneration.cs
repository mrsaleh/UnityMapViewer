﻿#if ENABLE_VSTU

using SyntaxTree.VisualStudio.Unity.Bridge;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ProjectFilesGeneration
{
    private class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }

    static ProjectFilesGeneration()
    {
        ProjectFilesGenerator.ProjectFileGeneration += (string name, string content) =>
        {
            // Ignore projects you do not want to edit here:
            if (name.EndsWith("Editor.csproj", StringComparison.InvariantCultureIgnoreCase)) return content;

            Debug.Log($"CUSTOMIZING PROJECT FILE: '{name}'");

            // Load csproj file:
            XNamespace ns = XNamespace.Get("http://schemas.microsoft.com/developer/msbuild/2003");
            XDocument xml = XDocument.Parse(content);

            // Find all PropertyGroups with Condition defining a Configuration and a Platform:
            XElement[] nodes = xml.Descendants()
                .Where(child =>
                    child.Name.LocalName == "PropertyGroup"
                    && (child.Attributes().FirstOrDefault(attr => attr.Name.LocalName == "Condition")?.Value.Contains("'$(Configuration)|$(Platform)'") ?? false)
                )
                .ToArray();

            // Add <LangVersion>7.3</LangVersion> to these PropertyGroups:
            foreach (XElement node in nodes)
                node.Add(new XElement(ns + "LangVersion", "7.3"));

            // Write to the csproj file:
            using (Utf8StringWriter str = new Utf8StringWriter())
            {
                xml.Save(str);
                return str.ToString();
            }
        };
    }
}

#endif