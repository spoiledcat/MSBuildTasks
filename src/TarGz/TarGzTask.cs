#region Copyright 2019 Andreia Gaita. All rights reserved.
/*
Copyright (c) 2019 Andreia Gaita

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using SpoiledCat.NiceIO;

namespace SpoiledCat.MSBuild.Community.Tasks
{
	/// <summary>
	/// Create a targz archive from a list of input files and/or directories
	/// </summary>
	/// <example>Search for a version number and update the revision.
	/// <code><![CDATA[
	/// <TarGz Include="version.txt;some\directory" Out="file.tgz" />
	/// ]]></code>
	/// </example>
	public class TarGz : Task
	{

		#region Properties

		/// <summary>
		/// Gets or sets the files to update.
		/// </summary>
		/// <value>The files.</value>
		[Required]
		public ITaskItem[] Include { get; set; }

		public string RootName { get; set; }

		[Required]
		public string Out { get; set; }

#endregion

		#region Output Properties

		#endregion

		/// <summary>
		/// When overridden in a derived class, executes the task.
		/// </summary>
		/// <returns>
		/// true if the task successfully executed; otherwise, false.
		/// </returns>
		public override bool Execute()
		{
			var buffer = new byte[32 * 1024];
			var rootName = string.IsNullOrWhiteSpace(RootName) ? "" : RootName + "/";
			using (var gz = new GZipOutputStream(File.Create(Out)))
			using (var outStream = new TarOutputStream(gz))
			{
				gz.SetLevel(3);

				foreach (var file in Include.Select(x => x.ItemSpec.ToNPath()).Where(x => x.FileExists()))
				{
					AddToArchive(rootName, file, outStream, ref buffer);
				}

				foreach (var dir in Include.Select(x => x.ItemSpec.ToNPath()).Where(x => x.DirectoryExists()))
				{
					foreach (var file in dir.Contents(true))
					{
						var relativePath = file.DirectoryExists() ? file.RelativeTo(dir) : file.Parent.RelativeTo(dir);
						var baseName = rootName;
						if (!relativePath.IsEmpty)
							baseName += relativePath.ToString(SlashMode.Forward) + "/";
						AddToArchive(baseName, file, outStream, ref buffer);
					}
				}
			}
			return true;
		}

		private void AddToArchive(string rootName, NPath file, TarOutputStream outStream, ref byte[] buffer)
		{
			rootName += file.FileName;

			var entry = TarEntry.CreateTarEntry(rootName);

			if (file.FileExists())
			{
				using (var data = File.OpenRead(file))
				{
					entry.Size = data.Length;
					outStream.PutNextEntry(entry);
					while (true)
					{
						var read = data.Read(buffer, 0, buffer.Length);
						if (read <= 0)
							break;
						outStream.Write(buffer, 0, read);
					}
					outStream.CloseEntry();
				}
			}
		}
	}
}
