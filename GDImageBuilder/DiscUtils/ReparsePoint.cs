//
// Copyright (c) 2008-2011, Kenneth Bell
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

namespace GDImageBuilder.DiscUtils
{
    /// <summary>
    /// Represents a Reparse Point, which can be associated with a file or directory.
    /// </summary>
    public sealed class ReparsePoint
    {
        private int _tag;
        private byte[] _content;

        /// <summary>
        /// Initializes a new instance of the ReparsePoint class.
        /// </summary>
        /// <param name="tag">The defined reparse point tag.</param>
        /// <param name="content">The reparse point's content.</param>
        public ReparsePoint(int tag, byte[] content)
        {
            _tag = tag;
            _content = content;
        }

        /// <summary>
        /// Gets or sets the defined reparse point tag.
        /// </summary>
        public int Tag
        {
            get { return _tag; }
            set { _tag = value; }
        }

        /// <summary>
        /// Gets or sets the reparse point's content.
        /// </summary>
        public byte[] Content
        {
            get { return _content; }
            set { _content = value; }
        }
    }
}
