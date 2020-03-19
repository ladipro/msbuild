// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Shared;
using Xunit;

namespace Microsoft.Build.UnitTests.OM.Evaluation
{
    /// <summary>
    /// Tests for ProjectStringCache
    /// </summary>
    public class ProjectStringCache_Tests
    {
        /// <summary>
        /// Test that loading two instances of the same xml file uses the same strings
        /// to store read values.
        /// </summary>
        [Fact]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void ContentIsSameAcrossInstances()
        {
            string content = ObjectModelHelpers.CleanupFileContents(@"
                    <Project xmlns='msbuildnamespace' ToolsVersion='msbuilddefaulttoolsversion'>
                        <ItemGroup>
                           Item group content
                        </ItemGroup>
                    </Project>
                    ");

            string path = FileUtilities.GetTemporaryFile();

            try
            {
                File.WriteAllText(path, content);

                ProjectStringCache cache = new ProjectStringCache();
                XmlDocumentWithLocation document1 = new XmlDocumentWithLocation();
                document1.StringCache = cache;
#if FEATURE_XML_LOADPATH
                document1.Load(path);
#else
                var xmlReadSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
                using (XmlReader xmlReader = XmlReader.Create(path, xmlReadSettings))
                {
                    document1.Load(xmlReader);
                }
#endif

                XmlDocumentWithLocation document2 = new XmlDocumentWithLocation();
                document2.StringCache = cache;
#if FEATURE_XML_LOADPATH
                document2.Load(path);
#else
                using (XmlReader xmlReader = XmlReader.Create(path, xmlReadSettings))
                {
                    document2.Load(xmlReader);
                }
#endif

                XmlNodeList nodes1 = document1.GetElementsByTagName("ItemGroup");
                XmlNodeList nodes2 = document2.GetElementsByTagName("ItemGroup");

                Assert.Equal(1, nodes1.Count);
                Assert.Equal(1, nodes2.Count);

                XmlNode node1 = nodes1[0].FirstChild;
                XmlNode node2 = nodes2[0].FirstChild;

                Assert.NotNull(node1);
                Assert.NotNull(node2);
                Assert.NotSame(node1, node2);
                Assert.Same(node1.Value, node2.Value);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Test that modifying one instance of a file does not affect the other file.
        /// </summary>
        [Fact]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void ContentCanBeModified()
        {
            string content = ObjectModelHelpers.CleanupFileContents(@"
                    <Project xmlns='msbuildnamespace' ToolsVersion='msbuilddefaulttoolsversion'>
                        <ItemGroup attr1='attr1value'>
                           Item group content
                        </ItemGroup>
                    </Project>
                    ");

            string path = FileUtilities.GetTemporaryFile();

            try
            {
                File.WriteAllText(path, content);
                ProjectStringCache cache = new ProjectStringCache();
                XmlDocumentWithLocation document1 = new XmlDocumentWithLocation();
                document1.StringCache = cache;
#if FEATURE_XML_LOADPATH
                document1.Load(path);
#else
                var xmlReadSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
                using (XmlReader xmlReader = XmlReader.Create(path, xmlReadSettings))
                {
                    document1.Load(xmlReader);
                }
#endif

                XmlDocumentWithLocation document2 = new XmlDocumentWithLocation();
                document2.StringCache = cache;
#if FEATURE_XML_LOADPATH
                document2.Load(path);
#else
                using (XmlReader xmlReader = XmlReader.Create(path, xmlReadSettings))
                {
                    document2.Load(xmlReader);
                }
#endif

                string outerXml1 = document1.OuterXml;
                string outerXml2 = document2.OuterXml;
                Assert.Equal(outerXml1, outerXml2);

                XmlNodeList nodes1 = document1.GetElementsByTagName("ItemGroup");
                XmlNodeList nodes2 = document2.GetElementsByTagName("ItemGroup");

                Assert.Equal(1, nodes1.Count);
                Assert.Equal(1, nodes2.Count);

                XmlNode node1 = nodes1[0];
                XmlNode node2 = nodes2[0];
                Assert.NotNull(node1);
                Assert.NotNull(node2);
                Assert.NotSame(node1, node2);
                Assert.Single(node1.Attributes);
                Assert.Single(node2.Attributes);
                Assert.Same(node1.Attributes[0].Value, node2.Attributes[0].Value);

                node2.Attributes[0].Value = "attr1value";
                Assert.Equal(node1.Attributes[0].Value, node2.Attributes[0].Value);
                Assert.NotSame(node1.Attributes[0].Value, node2.Attributes[0].Value);

                node1 = nodes1[0].FirstChild;
                node2 = nodes2[0].FirstChild;
                Assert.NotSame(node1, node2);
                Assert.Same(node1.Value, node2.Value);

                XmlText newText = document2.CreateTextNode("New Value");
                XmlNode parent = node2.ParentNode;
                parent.ReplaceChild(newText, node2);

                Assert.NotEqual(outerXml1, document2.OuterXml);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Test that unloading a project file makes its string entries disappear from
        /// the string cache.
        /// </summary>
        [Fact]
        [Trait("Category", "netcore-osx-failing")]
        [Trait("Category", "netcore-linux-failing")]
        public void RemovingFilesRemovesEntries()
        {
            string content = ObjectModelHelpers.CleanupFileContents(@"
                    <Project xmlns='msbuildnamespace' ToolsVersion='msbuilddefaulttoolsversion'>
                        <ItemGroup>Content</ItemGroup>
                    </Project>
                    ");

            string path = FileUtilities.GetTemporaryFile();

            try
            {
                File.WriteAllText(path, content);

                ProjectStringCache cache = new ProjectStringCache();
                ProjectCollection collection = new ProjectCollection();
                int entryCount;

                ProjectRootElement pre1 = ProjectRootElement.Create(collection);
                pre1.XmlDocument.StringCache = cache;
                pre1.FullPath = path;
#if FEATURE_XML_LOADPATH
                pre1.XmlDocument.Load(path);
#else
                var xmlReadSettings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
                using (XmlReader xmlReader = XmlReader.Create(path, xmlReadSettings))
                {
                    pre1.XmlDocument.Load(xmlReader);
                }
#endif

                entryCount = cache.Count;
                Assert.True(entryCount > 0);

                ProjectRootElement pre2 = ProjectRootElement.Create(collection);
                pre2.XmlDocument.StringCache = cache;
                pre2.FullPath = path;
#if FEATURE_XML_LOADPATH
                pre2.XmlDocument.Load(path);
#else
                using (XmlReader xmlReader = XmlReader.Create(path, xmlReadSettings))
                {
                    pre2.XmlDocument.Load(xmlReader);
                }
#endif

                // Entry count should not have changed
                Assert.Equal(entryCount, cache.Count);

                string itemGroupContent = cache.Get("Content");
                Assert.NotNull(itemGroupContent);

                XmlNodeList nodes1 = pre1.XmlDocument.GetElementsByTagName("ItemGroup");
                XmlNodeList nodes2 = pre2.XmlDocument.GetElementsByTagName("ItemGroup");

                Assert.Equal(1, nodes1.Count);
                Assert.Equal(1, nodes2.Count);

                XmlNode node1 = nodes1[0];
                XmlNode node2 = nodes2[0];
                Assert.NotNull(node1);
                Assert.NotNull(node2);
                Assert.NotSame(node1, node2);
                Assert.Same(node1.Value, node2.Value);

                // Now remove one document
                collection.UnloadProject(pre1);
                pre1 = null;
                System.GC.Collect();

                // We should still be able to get Content
                itemGroupContent = cache.Get("Content");
                Assert.NotNull(itemGroupContent);

                // Now remove the second document
                collection.UnloadProject(pre2);
                System.GC.Collect();

                // Now we should not be able to get Content
                itemGroupContent = cache.Get("Content");
                Assert.Equal(0, cache.Count);
                Assert.Null(itemGroupContent);

                // And there should be no entries
                Assert.Equal(0, cache.Count);
            }
            finally
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Adding a string equivalent to an existing instance should return the existing instance.
        /// </summary>
        [Fact]
        public void AddReturnsSameInstanceForSameString()
        {
            ProjectStringCache cache = new ProjectStringCache();

            string stringToAdd = "Test1";
            string return1 = cache.Add(stringToAdd);

            // Content of string should be the same.
            Assert.Equal(1, cache.Count);
            Assert.Equal(stringToAdd, return1);

            // Build a new string guaranteed not to be optimized by the compiler into the same instance.
            StringBuilder builder = new StringBuilder();
            builder.Append("Test");
            builder.Append("1");

            string return2 = cache.Add(builder.ToString());

            // Content of string should be the same.            
            Assert.Equal(builder.ToString(), return2);

            // Returned references should be the same
            Assert.Same(return1, return2);

            // Should not have added any new string instances to the cache.
            Assert.Equal(1, cache.Count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddStringToCache(ProjectStringCache cache)
        {
            // Build a new string guaranteed not to be optimized by the compiler into the same instance.
            StringBuilder builder = new StringBuilder();
            builder.Append("Test");
            builder.Append("1x");
            cache.Add(builder.ToString());
        }

        /// <summary>
        /// Collecing a string should remove the corresponding string entry.
        /// </summary>
        [Fact]
        public void CollectInstanceDeallocatesEntry()
        {
            ProjectStringCache cache = new ProjectStringCache();

            AddStringToCache(cache);

            System.GC.Collect();

            // Should be no instances left.
            Assert.Equal(0, cache.Count);

            // Build a new string guaranteed not to be optimized by the compiler into the same instance.
            //builder.Append("Test");
            //builder.Append("1");

            //string return2 = cache.Add(builder.ToString());

            // Returned references should NOT be the same
            //Assert.NotSame(return1, return2);
        }
    }
}
