using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlickSkipper
{
    public class LineReader
    {
        public Func<IEnumerable<string>> GetLines;
        public static LineReader New(string path)
        {
            var offsets = Lines.FromFile(path).ToArray();
            return new LineReader()
            {
                GetLines = () => Lines.FromFile(path, offsets)
            };
        }

        public void Test()
        {
            var test = "line one\nline poo💩\r\nline three\nこんにちは\n\n";
            var bytes = new UTF8Encoding().GetBytes(test);
            
            string line;
            IEnumerable<string> GetLines()
            {
                var ms = new MemoryStream(bytes);
                var sr = new StreamReader(ms, Encoding.UTF8);
                while ((line = sr.ReadLine()) != null) yield return line;
            }
            var ms2 = new MemoryStream(bytes);

            var offsets = Lines.FromStream(ms2);
            //todo
        }
    }
    class Lines
    {
        static UTF8Encoding enc = new System.Text.UTF8Encoding();
        
        public static IEnumerable<string> FromFile(string path, IEnumerable<(long, long)> offsets)
        {
            using var mmf = MemoryMappedFile.CreateFromFile(path);
            using var mmv = mmf.CreateViewAccessor();
            foreach (var line in FromFile(path))
            {
                var buffer = new byte[line.Item2];
                mmv.ReadArray(line.Item1, buffer, 0, (int)line.Item2);
                var str = enc.GetString(buffer);
                yield return str;
            }
        }

        public static IEnumerable<(long, long)> FromFile(string path)
        {
            using var mmf = MemoryMappedFile.CreateFromFile(path);
            using var mms = mmf.CreateViewStream();
            return FromStream(mms);
        }

        public static IEnumerable<(long, long)> FromStream(Stream mms)
        {
            var tr = new StreamReader(mms, Encoding.UTF8);
            var line = (0L, 0L);
            long pos = 0;
            while ((line = ReadLine(tr, ref pos)) != (-1L, -1L))
            {
                yield return line;
            }
        }

        static (long, long) ReadLine(TextReader tr, ref long pos)
        {
            var start = pos;
            var sawChar = false;
            while (true)
            {
                var ch = tr.Read();
                if (ch == -1) break;
                var c = (char)ch;
                pos += enc.GetByteCount(new[] { c }); //track byte count
                if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && tr.Peek() == '\n')
                    {
                        tr.Read();
                        pos += 1;
                    }

                    return (start, pos - start);
                }

                sawChar = true;
            }
            if (sawChar)
            {
                return (start, pos - start);
            }

            return (-1, -1);
        }
    }
}
