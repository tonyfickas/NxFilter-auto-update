using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ionic
{
    /// <summary>
    ///   A class to create, list, or extract TAR archives. This is the
    ///   primary, central class for the Tar library.
    /// </summary>
    ///
    /// <remarks>
    /// Bugs:
    /// <list type="bullet">
    ///   <item> does not read or write bzip2 compressed tarballs  (.tar.bz2)</item>
    ///   <item> uses Marshal.StructureToPtr and thus requires a LinkDemand, full trust.d </item>
    /// </list>
    /// </remarks>
    public class Tar
    {
        /// <summary>
        /// Specifies the options to use for tar creation or extraction
        /// </summary>
        public class Options
        {
            /// <summary>
            /// The compression to use.  Applies only during archive
            /// creation. Ignored during extraction.
            /// </summary>
            public TarCompression Compression
            {
                get; set;
            }

            /// <summary>
            ///   A TextWriter to which verbose status messages will be
            ///   written during operation.
            /// </summary>
            /// <remarks>
            ///   <para>
            ///     Use this to see messages emitted by the Tar logic.
            ///     You can use this whether Extracting or creating an archive.
            ///   </para>
            /// </remarks>
            /// <example>
            ///   <code lang="C#">
            ///     var options = new Tar.Options();
            ///     options.StatusWriter = Console.Out;
            ///     Ionic.Tar.Extract("Archive2.tgz", options);
            ///   </code>
            /// </example>
            public TextWriter StatusWriter
            {
                get; set;
            }

            public bool FollowSymLinks
            {
                get; set;
            }

            public bool Overwrite
            {
                get; set;
            }
            public string Path
            {
                get; set;
            }

            public bool DoNotSetTime
            {
                get; set;
            }
        }
        public enum TarCompression
        {
            None = 0,
            GZip,
        }
        public class TarEntry
        {
            internal TarEntry() { }
            public string Name
            {
                get;
                internal set;
            }
            public int Size
            {
                get;
                internal set;
            }

            public DateTime Mtime
            {
                get;
                internal set;
            }

            public TarEntryType @Type
            {
                get;
                internal set;
            }

            public char TypeChar
            {
                get
                {
                    switch (@Type)
                    {
                        case TarEntryType.File_Old:
                        case TarEntryType.File:
                        case TarEntryType.File_Contiguous:
                            return 'f';
                        case TarEntryType.HardLink:
                            return 'l';
                        case TarEntryType.SymbolicLink:
                            return 's';
                        case TarEntryType.CharSpecial:
                            return 'c';
                        case TarEntryType.BlockSpecial:
                            return 'b';
                        case TarEntryType.Directory:
                            return 'd';
                        case TarEntryType.Fifo:
                            return 'p';
                        case TarEntryType.GnuLongLink:
                        case TarEntryType.GnuLongName:
                        case TarEntryType.GnuSparseFile:
                        case TarEntryType.GnuVolumeHeader:
                            return (char)(@Type);
                        default: return '?';
                    }
                }
            }
        }

        public enum TarEntryType : byte
        {
            File_Old = 0,
            File = 48,
            HardLink = 49,
            SymbolicLink = 50,
            CharSpecial = 51,
            BlockSpecial = 52,
            Directory = 53,
            Fifo = 54,
            File_Contiguous = 55,
            GnuLongLink = (byte)'K',
            GnuLongName = (byte)'L',
            GnuSparseFile = (byte)'S',
            GnuVolumeHeader = (byte)'V',
        }

        [StructLayout(LayoutKind.Sequential, Size = 512)]
        internal struct HeaderBlock
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            public byte[] name;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] mode;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] uid;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] gid;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] size;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] mtime;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] chksum;

            public byte typeflag;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 100)]
            public byte[] linkname;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] magic;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] version;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] uname;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] gname;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] devmajor;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] devminor;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 155)]
            public byte[] prefix;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] pad;

            public static HeaderBlock CreateOne()
            {
                HeaderBlock hb = new HeaderBlock
                {
                    name = new byte[100],
                    mode = new byte[8],
                    uid = new byte[8],
                    gid = new byte[8],
                    size = new byte[12],
                    mtime = new byte[12],
                    chksum = new byte[8],
                    linkname = new byte[100],
                    magic = new byte[6],
                    version = new byte[2],
                    uname = new byte[32],
                    gname = new byte[32],
                    devmajor = new byte[8],
                    devminor = new byte[8],
                    prefix = new byte[155],
                    pad = new byte[12],
                };

                Array.Copy(System.Text.Encoding.ASCII.GetBytes("ustar "), 0, hb.magic, 0, 6);
                hb.version[0] = hb.version[1] = (byte)TarEntryType.File;

                return hb;
            }


            public bool VerifyChksum()
            {
                int stored = GetChksum();
                int calculated = SetChksum();
                TraceOutput("stored({0})  calc({1})", stored, calculated);

                return (stored == calculated);
            }


            public int GetChksum()
            {
                TraceData("chksum", this.chksum);

                bool allZeros = true;
                Array.ForEach(this.chksum, (x) => { if (x != 0) allZeros = false; });
                if (allZeros) return 256;

                if (!(((this.chksum[6] == 0) && (this.chksum[7] == 0x20)) ||
                    ((this.chksum[7] == 0) && (this.chksum[6] == 0x20))))
                    return -1;

                string v = System.Text.Encoding.ASCII.GetString(this.chksum, 0, 6).Trim();
                TraceOutput("chksum string: '{0}'", v);
                return Convert.ToInt32(v, 8);
            }


            public int SetChksum()
            {
                var a = System.Text.Encoding.ASCII.GetBytes(new String(' ', 8));
                Array.Copy(a, 0, this.chksum, 0, a.Length);

                int rawSize = 512;
                byte[] block = new byte[rawSize];
                IntPtr buffer = Marshal.AllocHGlobal(rawSize);
                try
                {
                    Marshal.StructureToPtr(this, buffer, false);
                    Marshal.Copy(buffer, block, 0, rawSize);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }

                int sum = 0;
                Array.ForEach(block, (x) => sum += x);
                string s = "000000" + Convert.ToString(sum, 8);

                a = System.Text.Encoding.ASCII.GetBytes(s.Substring(s.Length - 6));
                Array.Copy(a, 0, this.chksum, 0, a.Length);
                this.chksum[6] = 0;
                this.chksum[7] = 0x20;

                return sum;
            }


            public void SetSize(int sz)
            {
                string ssz = String.Format("          {0} ", Convert.ToString(sz, 8));
                var a = System.Text.Encoding.ASCII.GetBytes(ssz.Substring(ssz.Length - 12));
                Array.Copy(a, 0, this.size, 0, a.Length);
            }

            public int GetSize()
            {
                return Convert.ToInt32(System.Text.Encoding.ASCII.GetString(this.size).TrimNull(), 8);
            }

            public void InsertLinkName(string linkName)
            {
                var a = System.Text.Encoding.ASCII.GetBytes(linkName);
                Array.Copy(a, 0, this.linkname, 0, a.Length);
            }

            public void InsertName(string itemName)
            {
                if (itemName.Length <= 100)
                {
                    var a = System.Text.Encoding.ASCII.GetBytes(itemName);
                    Array.Copy(a, 0, this.name, 0, a.Length);
                }
                else
                {
                    var a = System.Text.Encoding.ASCII.GetBytes(itemName);
                    Array.Copy(a, a.Length - 100, this.name, 0, 100);
                    Array.Copy(a, 0, this.prefix, 0, a.Length - 100);
                }

                DateTime dt = File.GetLastWriteTimeUtc(itemName);
                int time_t = TimeConverter.DateTime2TimeT(dt);
                string mtime = "     " + Convert.ToString(time_t, 8) + " ";
                var a1 = System.Text.Encoding.ASCII.GetBytes(mtime.Substring(mtime.Length - 12));
                Array.Copy(a1, 0, this.mtime, 0, a1.Length);
            }


            public DateTime GetMtime()
            {
                int time_t = Convert.ToInt32(System.Text.Encoding.ASCII.GetString(this.mtime).TrimNull(), 8);
                return DateTime.SpecifyKind(TimeConverter.TimeT2DateTime(time_t), DateTimeKind.Utc);
            }

            public string GetName()
            {
                string n = null;
                string m = GetMagic();
                if (m != null && m.Equals("ustar"))
                {
                    n = (this.prefix[0] == 0)
                        ? System.Text.Encoding.ASCII.GetString(this.name).TrimNull()
                        : System.Text.Encoding.ASCII.GetString(this.prefix).TrimNull() + System.Text.Encoding.ASCII.GetString(this.name).TrimNull();
                }
                else
                {
                    n = System.Text.Encoding.ASCII.GetString(this.name).TrimNull();
                }
                return n;
            }


            private string GetMagic()
            {
                string m = (this.magic[0] == 0) ? null : System.Text.Encoding.ASCII.GetString(this.magic).Trim();
                return m;
            }

        }

        private Options TarOptions { get; set; }

        private Tar() { }

        public static System.Collections.ObjectModel.ReadOnlyCollection<TarEntry>
            Extract(string archive)
        {
            return ListOrExtract(archive, true, null).AsReadOnly();
        }

        public static System.Collections.ObjectModel.ReadOnlyCollection<TarEntry>
            Extract(string archive, Options options)
        {
            return ListOrExtract(archive, true, options).AsReadOnly();
        }
        public static System.Collections.ObjectModel.ReadOnlyCollection<TarEntry>
            List(string archive)
        {
            return ListOrExtract(archive, false, null).AsReadOnly();
        }

        private static List<TarEntry> ListOrExtract(string archive,
                                                    bool wantExtract,
                                                    Options options)
        {
            var t = new Tar();
            t.TarOptions = options ?? new Options();
            return t._internal_ListOrExtract(archive, wantExtract, options.Path);
        }


        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode)]
        private static extern int CreateSymbolicLink(string symlinkFileName,
                                                     string targetFileName, int flags);


        private List<TarEntry> _internal_ListOrExtract(string archive, bool wantExtract, string path)
        {
            var entryList = new List<TarEntry>();
            byte[] block = new byte[512];
            int n = 0;
            int blocksToMunch = 0;
            int remainingBytes = 0;
            Stream output = null;
            DateTime mtime = DateTime.Now;
            string name = null;
            TarEntry entry = null;
            var deferredDirTimestamp = new Dictionary<String, DateTime>();

            if (!File.Exists(archive))
                throw new InvalidOperationException("The specified file does not exist.");

            using (Stream fs = _internal_GetInputStream(archive))
            {
                while ((n = fs.Read(block, 0, block.Length)) > 0)
                {
                    if (blocksToMunch > 0)
                    {
                        if (output != null)
                        {
                            int bytesToWrite = (block.Length < remainingBytes)
                                ? block.Length
                                : remainingBytes;

                            output.Write(block, 0, bytesToWrite);
                            remainingBytes -= bytesToWrite;
                        }

                        blocksToMunch--;

                        if (blocksToMunch == 0)
                        {
                            if (output != null)
                            {
                                if (output is MemoryStream)
                                {
                                    entry.Name = name = System.Text.Encoding.ASCII.GetString((output as MemoryStream).ToArray()).TrimNull();
                                }

                                output.Close();
                                output.Dispose();

                                if (output is FileStream && !TarOptions.DoNotSetTime)
                                {
                                    File.SetLastWriteTimeUtc(name, mtime);
                                }

                                output = null;
                            }
                        }
                        continue;
                    }

                    HeaderBlock hb = serializer.RawDeserialize(block);

                    if (!hb.VerifyChksum())
                        throw new Exception("header checksum is invalid.");

                    if (entry == null || entry.Type != TarEntryType.GnuLongName)
                        name = hb.GetName();

                    if (name == null || name.Length == 0) break;
                    mtime = hb.GetMtime();
                    remainingBytes = hb.GetSize();

                    if (hb.typeflag == 0) hb.typeflag = (byte)'0';

                    entry = new TarEntry() { Name = name, Mtime = mtime, Size = remainingBytes, @Type = (TarEntryType)hb.typeflag };

                    if (entry.Type != TarEntryType.GnuLongName)
                        entryList.Add(entry);

                    blocksToMunch = (remainingBytes > 0)
                        ? ((remainingBytes - 1) / 512) + 1
                        : 0;

                    if (entry.Type == TarEntryType.GnuLongName)
                    {
                        if (name != "././@LongLink")
                        {
                            if (wantExtract)
                                throw new Exception(String.Format("unexpected name for type 'L' (expected '././@LongLink', got '{0}')", name));
                        }
                        output = new MemoryStream();
                        continue;
                    }

                    if (wantExtract)
                    {
                        name = path + name;
                        switch (entry.Type)
                        {
                            case TarEntryType.Directory:
                                if (!Directory.Exists(name))
                                {
                                    Directory.CreateDirectory(name);
                                    if (!TarOptions.DoNotSetTime)
                                        deferredDirTimestamp.Add(name.TrimSlash(), mtime);
                                }
                                else if (TarOptions.Overwrite)
                                {
                                    if (!TarOptions.DoNotSetTime)
                                        deferredDirTimestamp.Add(name.TrimSlash(), mtime);
                                }
                                break;

                            case TarEntryType.File_Old:
                            case TarEntryType.File:
                            case TarEntryType.File_Contiguous:
                                string p = Path.GetDirectoryName(name);
                                if (!String.IsNullOrEmpty(p))
                                {
                                    if (!Directory.Exists(p))
                                        Directory.CreateDirectory(p);
                                }
                                output = _internal_GetExtractOutputStream(name);
                                break;

                            case TarEntryType.GnuVolumeHeader:
                            case TarEntryType.CharSpecial:
                            case TarEntryType.BlockSpecial:
                                break;

                            case TarEntryType.SymbolicLink:
                                break;


                            default:
                                throw new Exception(String.Format("unsupported entry type ({0})", hb.typeflag));
                        }
                    }
                }
            }

            if (deferredDirTimestamp.Count > 0)
            {
                foreach (var s in deferredDirTimestamp.Keys)
                {
                    Directory.SetLastWriteTimeUtc(s, deferredDirTimestamp[s]);
                }
            }

            return entryList;
        }

        private Stream _internal_GetInputStream(string archive)
        {
            if (archive.EndsWith(".tgz") || archive.EndsWith(".tar.gz"))
            {
                var fs = File.Open(archive, FileMode.Open, FileAccess.Read);
                return new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Decompress, false);
            }

            return File.Open(archive, FileMode.Open, FileAccess.Read);
        }

        private Stream _internal_GetExtractOutputStream(string name)
        {
            if (TarOptions.Overwrite || !File.Exists(name))
            {
                if (TarOptions.StatusWriter != null)
                    TarOptions.StatusWriter.WriteLine("{0}", name);
                return File.Open(name, FileMode.Create, FileAccess.ReadWrite);
            }

            if (TarOptions.StatusWriter != null)
                TarOptions.StatusWriter.WriteLine("{0} (not overwriting)", name);

            return null;
        }

        public static void CreateArchive(string outputFile,
                                         IEnumerable<String> filesOrDirectories)
        {
            var t = new Tar();
            t._internal_CreateArchive(outputFile, filesOrDirectories);
        }
        public static void CreateArchive(string outputFile,
                                         IEnumerable<String> filesOrDirectories,
                                         Options options)
        {
            var t = new Tar();
            t.TarOptions = options;
            t._internal_CreateArchive(outputFile, filesOrDirectories);
        }


        private void _internal_CreateArchive(string outputFile, IEnumerable<String> files)
        {
            if (String.IsNullOrEmpty(outputFile))
                throw new InvalidOperationException("You must specify an output file.");
            if (File.Exists(outputFile))
                throw new InvalidOperationException("The output file you specified already exists.");
            if (Directory.Exists(outputFile))
                throw new InvalidOperationException("The output file you specified is a directory.");

            int fcount = 0;
            try
            {

                using (_outfs = _internal_GetOutputArchiveStream(outputFile))
                {
                    foreach (var f in files)
                    {
                        fcount++;

                        if (Directory.Exists(f))
                            AddDirectory(f);
                        else if (File.Exists(f))
                            AddFile(f);
                        else
                            throw new InvalidOperationException(String.Format("The file you specified ({0}) was not found.", f));
                    }

                    if (fcount < 1)
                        throw new InvalidOperationException("Specify one or more input files to place into the archive.");

                    // terminator
                    byte[] block = new byte[512];
                    _outfs.Write(block, 0, block.Length);
                    _outfs.Write(block, 0, block.Length);
                }
            }
            finally
            {
                if (fcount < 1)
                    try { File.Delete(outputFile); } catch { }
            }
        }
        private Stream _internal_GetOutputArchiveStream(string filename)
        {
            switch (TarOptions.Compression)
            {
                case TarCompression.None:
                    return File.Open(filename, FileMode.Create, FileAccess.ReadWrite);

                case TarCompression.GZip:
                    {
                        var fs = File.Open(filename, FileMode.Create, FileAccess.ReadWrite);
                        return new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Compress, false);
                    }

                default:
                    throw new Exception("bad state");
            }
        }
        private void AddDirectory(string dirName)
        {
            dirName = dirName.TrimVolume();

            if (!dirName.EndsWith("/"))
                dirName += "/";

            if (TarOptions.StatusWriter != null)
                TarOptions.StatusWriter.WriteLine("{0}", dirName);

            HeaderBlock hb = HeaderBlock.CreateOne();
            hb.InsertName(dirName);
            hb.typeflag = 5 + (byte)'0';
            hb.SetSize(0);
            hb.SetChksum();
            byte[] block = serializer.RawSerialize(hb);
            _outfs.Write(block, 0, block.Length);

            String[] filenames = Directory.GetFiles(dirName);
            foreach (String filename in filenames)
            {
                AddFile(filename);
            }

            String[] dirnames = Directory.GetDirectories(dirName);
            foreach (String d in dirnames)
            {
                var a = System.IO.File.GetAttributes(d);
                if ((a & FileAttributes.ReparsePoint) == 0)
                    AddDirectory(d);
                else if (this.TarOptions.FollowSymLinks)
                    AddDirectory(d);
                else
                    AddSymlink(d);
            }
        }



        private void AddSymlink(string name)
        {
            if (TarOptions.StatusWriter != null)
                TarOptions.StatusWriter.WriteLine("{0}", name);

            HeaderBlock hb = HeaderBlock.CreateOne();
            hb.InsertName(name);
            hb.InsertLinkName(name);
            hb.typeflag = (byte)TarEntryType.SymbolicLink;
            hb.SetSize(0);
            hb.SetChksum();
            byte[] block = serializer.RawSerialize(hb);
            _outfs.Write(block, 0, block.Length);
        }



        private void AddFile(string fileName)
        {
            var a = System.IO.File.GetAttributes(fileName);
            if ((a & FileAttributes.ReparsePoint) != 0)
            {
                AddSymlink(fileName);
                return;
            }
            if (TarOptions.StatusWriter != null)
                TarOptions.StatusWriter.WriteLine("{0}", fileName);

            HeaderBlock hb = HeaderBlock.CreateOne();
            hb.InsertName(fileName);
            hb.typeflag = (byte)TarEntryType.File;
            FileInfo fi = new FileInfo(fileName);
            hb.SetSize((int)fi.Length);

            hb.SetChksum();
            byte[] block = serializer.RawSerialize(hb);
            _outfs.Write(block, 0, block.Length);

            using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                int n = 0;
                Array.Clear(block, 0, block.Length);
                while ((n = fs.Read(block, 0, block.Length)) > 0)
                {
                    _outfs.Write(block, 0, block.Length);
                    Array.Clear(block, 0, block.Length);
                }
            }
        }
        private RawSerializer<HeaderBlock> _s;
        private RawSerializer<HeaderBlock> serializer
        {
            get
            {
                if (_s == null)
                    _s = new RawSerializer<HeaderBlock>();
                return _s;
            }
        }
        [System.Diagnostics.ConditionalAttribute("Trace")]
        private static void TraceData(string label, byte[] data)
        {
            Console.WriteLine("{0}:", label);
            Array.ForEach(data, (x) => Console.Write("{0:X} ", (byte)x));
            System.Console.WriteLine();
        }
        [System.Diagnostics.ConditionalAttribute("Trace")]
        private static void TraceOutput(string format, params object[] varParams)
        {
            Console.WriteLine(format, varParams);
        }
        private Stream _outfs = null;
    }

    internal static class Extensions
    {
        public static string TrimNull(this string t)
        {
            return t.Trim(new char[] { (char)0x20, (char)0x00 });
        }
        public static string TrimSlash(this string t)
        {
            return t.TrimEnd(new char[] { (char)'/' });
        }

        public static string TrimVolume(this string t)
        {
            if (t.Length > 3 && t[1] == ':' && t[2] == '/')
                return t.Substring(3);
            if (t.Length > 2 && t[0] == '/' && t[1] == '/')
                return t.Substring(2);
            return t;
        }
    }
    internal static class TimeConverter
    {
        private static System.DateTime _unixEpoch = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static System.DateTime _win32Epoch = new System.DateTime(1601, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static Int32 DateTime2TimeT(System.DateTime datetime)
        {
            System.TimeSpan delta = datetime - _unixEpoch;
            return (System.Int32)(delta.TotalSeconds);
        }


        public static System.DateTime TimeT2DateTime(int timet)
        {
            return _unixEpoch.AddSeconds(timet);
        }

        public static Int64 DateTime2Win32Ticks(System.DateTime datetime)
        {
            System.TimeSpan delta = datetime - _win32Epoch;
            return (Int64)(delta.TotalSeconds * 10000000L);
        }

        public static DateTime Win32Ticks2DateTime(Int64 ticks)
        {
            return _win32Epoch.AddSeconds(ticks / 10000000);
        }
    }
    internal class RawSerializer<T>
    {
        public T RawDeserialize(byte[] rawData)
        {
            return RawDeserialize(rawData, 0);
        }

        public T RawDeserialize(byte[] rawData, int position)
        {
            int rawsize = Marshal.SizeOf(typeof(T));
            if (rawsize > rawData.Length)
                return default(T);

            IntPtr buffer = Marshal.AllocHGlobal(rawsize);
            try
            {
                Marshal.Copy(rawData, position, buffer, rawsize);
                return (T)Marshal.PtrToStructure(buffer, typeof(T));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public byte[] RawSerialize(T item)
        {
            int rawSize = Marshal.SizeOf(typeof(T));
            byte[] rawData = new byte[rawSize];
            IntPtr buffer = Marshal.AllocHGlobal(rawSize);
            try
            {
                Marshal.StructureToPtr(item, buffer, false);
                Marshal.Copy(buffer, rawData, 0, rawSize);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return rawData;
        }
    }

}