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
    using System;
    using System.IO;

    /// <summary>
    /// Provides the base class for both <see cref="DiscFileInfo"/> and <see cref="DiscDirectoryInfo"/> objects.
    /// </summary>
    public class DiscFileSystemInfo
    {
        private DiscFileSystem _fileSystem;
        private string _path;

        internal DiscFileSystemInfo(DiscFileSystem fileSystem, string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            _fileSystem = fileSystem;
            _path = path.Trim('\\');
        }

        /// <summary>
        /// Gets the file system the referenced file or directory exists on.
        /// </summary>
        public DiscFileSystem FileSystem
        {
            get { return _fileSystem; }
        }

        /// <summary>
        /// Gets the name of the file or directory.
        /// </summary>
        public virtual string Name
        {
            get { return Utilities.GetFileFromPath(_path); }
        }

        /// <summary>
        /// Gets the full path of the file or directory.
        /// </summary>
        public virtual string FullName
        {
            get { return _path; }
        }

        /// <summary>
        /// Gets the extension part of the file or directory name.
        /// </summary>
        public virtual string Extension
        {
            get
            {
                string name = Name;
                int sepIdx = name.LastIndexOf('.');
                if (sepIdx >= 0)
                {
                    return name.Substring(sepIdx + 1);
                }

                return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.IO.FileAttributes"/> of the current <see cref="DiscFileSystemInfo"/> object.
        /// </summary>
        public virtual FileAttributes Attributes
        {
            get { return FileSystem.GetAttributes(_path); }
            set { FileSystem.SetAttributes(_path, value); }
        }

        /// <summary>
        /// Gets the <see cref="DiscDirectoryInfo"/> of the directory containing the current <see cref="DiscFileSystemInfo"/> object.
        /// </summary>
        public virtual DiscDirectoryInfo Parent
        {
            get
            {
                if (string.IsNullOrEmpty(_path))
                {
                    return null;
                }

                return new DiscDirectoryInfo(FileSystem, Utilities.GetDirectoryFromPath(_path));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the file system object exists.
        /// </summary>
        public virtual bool Exists
        {
            get { return FileSystem.Exists(_path); }
        }

        /// <summary>
        /// Gets or sets the creation time (in local time) of the current <see cref="DiscFileSystemInfo"/> object.
        /// </summary>
        public virtual DateTime CreationTime
        {
            get { return CreationTimeUtc.ToLocalTime(); }
            set { CreationTimeUtc = value.ToUniversalTime(); }
        }

        /// <summary>
        /// Gets or sets the creation time (in UTC) of the current <see cref="DiscFileSystemInfo"/> object.
        /// </summary>
        public virtual DateTime CreationTimeUtc
        {
            get { return FileSystem.GetCreationTimeUtc(_path); }
            set { FileSystem.SetCreationTimeUtc(_path, value); }
        }

        /// <summary>
        /// Gets or sets the last time (in local time) the file or directory was accessed.
        /// </summary>
        /// <remarks>Read-only file systems will never update this value, it will remain at a fixed value.</remarks>
        public virtual DateTime LastAccessTime
        {
            get { return LastAccessTimeUtc.ToLocalTime(); }
            set { LastAccessTimeUtc = value.ToUniversalTime(); }
        }

        /// <summary>
        /// Gets or sets the last time (in UTC) the file or directory was accessed.
        /// </summary>
        /// <remarks>Read-only file systems will never update this value, it will remain at a fixed value.</remarks>
        public virtual DateTime LastAccessTimeUtc
        {
            get { return FileSystem.GetLastAccessTimeUtc(_path); }
            set { FileSystem.SetLastAccessTimeUtc(_path, value); }
        }

        /// <summary>
        /// Gets or sets the last time (in local time) the file or directory was written to.
        /// </summary>
        public virtual DateTime LastWriteTime
        {
            get { return LastWriteTimeUtc.ToLocalTime(); }
            set { LastWriteTimeUtc = value.ToUniversalTime(); }
        }

        /// <summary>
        /// Gets or sets the last time (in UTC) the file or directory was written to.
        /// </summary>
        public virtual DateTime LastWriteTimeUtc
        {
            get { return FileSystem.GetLastWriteTimeUtc(_path); }
            set { FileSystem.SetLastWriteTimeUtc(_path, value); }
        }

        /// <summary>
        /// Gets the path to the referenced file.
        /// </summary>
        protected string Path
        {
            get { return _path; }
        }

        /// <summary>
        /// Deletes a file or directory.
        /// </summary>
        public virtual void Delete()
        {
            if ((Attributes & FileAttributes.Directory) != 0)
            {
                FileSystem.DeleteDirectory(_path);
            }
            else
            {
                FileSystem.DeleteFile(_path);
            }
        }

        /// <summary>
        /// Indicates if <paramref name="obj"/> is equivalent to this object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is equivalent, else <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            DiscFileSystemInfo asInfo = obj as DiscFileSystemInfo;
            if (obj == null)
            {
                return false;
            }

            return string.Compare(Path, asInfo.Path, StringComparison.Ordinal) == 0 && DiscFileSystem.Equals(FileSystem, asInfo.FileSystem);
        }

        /// <summary>
        /// Gets the hash code for this object.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _path.GetHashCode() ^ _fileSystem.GetHashCode();
        }
    }
}
