﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Cake.Core.IO;

namespace Cake.Common.Solution.Project.XmlDoc
{
    /// <summary>
    /// The MSBuild Xml documentation example code parser
    /// </summary>
    public sealed class XmlDocExampleCodeParser
    {
        private readonly IFileSystem _fileSystem;
        private readonly IGlobber _globber;

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlDocExampleCodeParser"/> class.
        /// </summary>
        /// <param name="fileSystem">The file system.</param>
        /// <param name="globber">The globber.</param>
        public XmlDocExampleCodeParser(IFileSystem fileSystem, IGlobber globber)
        {
            if (fileSystem == null)
            {
                throw new ArgumentNullException("fileSystem");
            }

            if (globber == null)
            {
                throw new ArgumentNullException("globber");
            }

            _fileSystem = fileSystem;
            _globber = globber;
        }

        /// <summary>
        /// Parses Xml documentation example code from given path
        /// </summary>
        /// <param name="xmlFilePath">Path to the file to parse.</param>
        /// <returns>Parsed Example Code</returns>
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public IEnumerable<XmlDocExampleCode> Parse(FilePath xmlFilePath)
        {
            if (xmlFilePath == null)
            {
                throw new ArgumentNullException("xmlFilePath", "Invalid xml file path supplied.");
            }

            var xmlFile = _fileSystem.GetFile(xmlFilePath);
            if (!xmlFile.Exists)
            {
                throw new FileNotFoundException("Supplied xml file not found.", xmlFilePath.FullPath);
            }

            using (var xmlStream = xmlFile.OpenRead())
            {
                using (var xmlReader = XmlReader.Create(xmlStream))
                {
                    return (
                        from doc in XDocument.Load(xmlReader).Elements("doc")
                        from members in doc.Elements("members")
                        from member in members.Elements("member")
                        from example in member.Elements("example")
                        from code in example.Elements("code")
                        let cleanedCode = string.Join("\r\n",
                            code.Value.Split('\r', '\n')
                                .Where(line => !string.IsNullOrWhiteSpace(line)))
                        select new XmlDocExampleCode(
                                member.Attributes("name").Select(name => name.Value).FirstOrDefault(),
                                cleanedCode)).ToArray();
                }
            }
        }

        /// <summary>
        /// Parses Xml documentation example code from file(s) using given pattern 
        /// </summary>
        /// <param name="pattern">The globber file pattern.</param>
        /// <returns>Parsed Example Code</returns>
        public IEnumerable<XmlDocExampleCode> ParseFiles(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentNullException("pattern", "Invalid pattern supplied.");
            }

            return _globber.GetFiles(pattern)
                .SelectMany(Parse);
        }
    }
}