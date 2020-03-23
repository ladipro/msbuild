// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Shared;
using System;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.Build.Construction
{
    /// <summary>
    /// Derivation of XmlAttribute to implement IXmlLineInfo
    /// </summary>
    internal class XmlAttributeWithLocation : XmlAttribute, IXmlLineInfo, IElementLocation
    {
        private static int _totalCreated = 0;
        private static int _totalQueried = 0;

        /// <summary>
        /// Line, column, file information
        /// </summary>
        private ElementLocation _elementLocation;
        private bool _elementLocationQueried = false;

        private ushort _locationLine;
        private ushort _locationColumn;

        /// <summary>
        /// Constructor without location information
        /// </summary>
        public XmlAttributeWithLocation(string prefix, string localName, string namespaceURI, XmlDocument document)
            : this(prefix, localName, namespaceURI, document, 0, 0)
        {
        }

        /// <summary>
        /// Constructor with location information
        /// </summary>
        public XmlAttributeWithLocation(string prefix, string localName, string namespaceURI, XmlDocument document, int lineNumber, int columnNumber)
            : base(prefix, localName, namespaceURI, document)
        {
            _totalCreated++;

            if (lineNumber <= 65535 && columnNumber <= 65535)
            {
                this._locationLine = Convert.ToUInt16(lineNumber);
                this._locationColumn = Convert.ToUInt16(columnNumber);
            }
            else
            {
                XmlDocumentWithLocation documentWithLocation = (XmlDocumentWithLocation)document;
                _elementLocation = ElementLocation.Create(documentWithLocation.FullPath, lineNumber, columnNumber);
            }
        }

        /// <summary>
        /// Returns the line number if available, else 0.
        /// IXmlLineInfo member.
        /// </summary>
        public int LineNumber
        {
            [DebuggerStepThrough]
            get
            { return Location.Line; }
        }

        /// <summary>
        /// Returns the column number if available, else 0.
        /// IXmlLineInfo member.
        /// </summary>
        public int LinePosition
        {
            [DebuggerStepThrough]
            get
            { return Location.Column; }
        }

        internal IElementLocation Location
        {
            get
            {
                return this;
            }
        }

        /// <summary>
        /// Provides an ElementLocation for this attribute.
        /// </summary>
        /// <remarks>
        /// Should have at least the file name if the containing project has been given a file name,
        /// even if it wasn't loaded from disk, or has been edited since. That's because we set that
        /// path on our XmlDocumentWithLocation wrapper class.
        /// </remarks>
        internal ElementLocation TrueLocation
        {
            get
            {
                if (_elementLocation == null)
                {
                    if (!_elementLocationQueried)
                    {
                        _totalQueried++;
                        //Console.WriteLine("### ATTR Total queried/created: {0}/{1}", _totalQueried, _totalCreated);
                    }
                    _elementLocationQueried = true;

                    XmlDocumentWithLocation ownerDocumentWithLocation = (XmlDocumentWithLocation)OwnerDocument;
                    _elementLocation = ElementLocation.Create(ownerDocumentWithLocation.FullPath, _locationLine, _locationColumn);
                }
                return _elementLocation;
            }
        }

        /// <summary>
        /// Whether location is available.
        /// IXmlLineInfo member.
        /// </summary>
        public bool HasLineInfo()
        {
            return Location.Line != 0;
        }

        string IElementLocation.File => TrueLocation.File;
        int IElementLocation.Line => TrueLocation.Line;
        int IElementLocation.Column => TrueLocation.Column;
        string IElementLocation.LocationString => TrueLocation.LocationString;
        public void Translate(Microsoft.Build.BackEnd.ITranslator translator) => ((Microsoft.Build.BackEnd.ITranslatable)TrueLocation).Translate(translator);
    }
}
