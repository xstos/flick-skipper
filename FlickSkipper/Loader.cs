using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using FASTER.core;

namespace FlickSkipper
{
    public static class Ext
    {

    }
    public struct NameRecord
    {
        public uint Id;
        public uint[] Name;
        public ushort Birth;
        public ushort Death;
        public byte[] Profession;
        public uint[] Titles;
    }
    class Loader
    {
        static ushort ParseAge(string value)
        {
            return value.Equals("\\N") ? ushort.MaxValue : Convert.ToUInt16(value);
        }

        static uint[] ParseTitles(string value)
        {
            if (value.Equals("\\N")) return Array.Empty<uint>();
            var split = value.Split(",");
            var l = split.Length;
            var ret = new uint[l];
            for (var i = 0; i < l; i++)
            {
                ret[i] = Convert.ToUInt32(split[i][2..]);
            }

            return ret;
        }

        public static Dictionary<uint, (float, uint)> GetRatings()
        {
            var lines = File.ReadAllLines("../../../ratings.tsv").Skip(1);
            var ret = new Dictionary<uint, (float, uint)>();
            foreach (var line in lines)
            {
                var split = line.Split("\t");
                var id = Convert.ToUInt32(split[0][2..]);
                var ratingStr = split[1];
                var voteStr = split[2];
                var rating = ratingStr.Equals("\\N") ? 0f : Convert.ToSingle(ratingStr);
                var votes = voteStr.Equals("\\N") ? 0u : Convert.ToUInt32(voteStr);
                ret.Add(id, (rating,votes));
            }

            return ret;
        }

        public static void IndexTitles()
        {
            var ratings = GetRatings();

            var inputFile = "../../../title.akas.tsv";
            using var mmf = MemoryMappedFile.CreateFromFile(inputFile);
            using var mms = mmf.CreateViewStream();
            using var sr = new StreamReader(mms);

            using var titleStream = File.OpenWrite("../../../title.akas.bin");
            using var titleWriter = new BinaryWriter(titleStream, Encoding.UTF8);

            var s = sr.ReadLine(); //skip header
            while ((s = sr.ReadLine()) != null)
            {
                if (s.StartsWith('\0')) break;
                var items = s.Split("\t");
                var id = Convert.ToUInt32(items[0][2..]);
                if (!ratings.TryGetValue(id, out var rating)) continue;
                if (rating.Item1 < 5f)
                {
                    ratings.Remove(id);
                    continue;
                }

                var region = items[3];
                if (!region.Equals("US")) continue;

                var lang = items[4];
                if (!lang.Equals("\\N")) continue;
                titleWriter.Write(id);
                titleWriter.Write(items[2]);
            }

        }

        public static void Index()
        {
            IndexTitles();
            //IndexPeople();
        }
        public static void IndexPeople()
        {
            var inputFile = "../../../name.basics.tsv";
            
            using var recordStream = File.OpenWrite("../../../name.basics.records.bin");
            using var recordWriter = new BinaryWriter(recordStream, Encoding.UTF8);

            using var nameStream = File.OpenWrite("../../../name.basics.names.bin");
            using var nameWriter = new BinaryWriter(nameStream, Encoding.UTF8);
            
            using var professionStream = File.OpenWrite("../../../name.basics.professions.bin");
            using var professionWriter = new BinaryWriter(professionStream, Encoding.UTF8);

            using var mmf = MemoryMappedFile.CreateFromFile(inputFile);
            using var mms = mmf.CreateViewStream();
            using var sr = new StreamReader(mms);
            
            string[] items;
            
            NameRecord rec;
            var names = new Dictionary<string, uint>();
            uint AddName(string text)
            {
                if (names.TryGetValue(text, out var ret)) return ret;
                var c = (uint)(names.Count);
                names.Add(text, c);
                return c;
            }
            var professions = new Dictionary<string, byte>();
            byte AddProfession(string text)
            {
                if (professions.TryGetValue(text, out var ret)) return ret;
                var c = (byte)(professions.Count);
                professions.Add(text, c);
                return c;
            }

            var s = sr.ReadLine(); //skip header
            while ((s = sr.ReadLine()) != null)
            {
                if (s.StartsWith('\0')) break;
                items = s.Split('\t');
                
                var id = Convert.ToUInt32(items[0][2..]);
                var name = items[1].Split(" ").Select(AddName).ToArray();
                var birth = ParseAge(items[2]);
                var death = ParseAge(items[3]);
                var prof = items[4].Split(",").Select(AddProfession).ToArray();
                var titles = ParseTitles(items[5]);

                rec = new NameRecord()
                {
                    Id = id,
                    Name = name,
                    Birth = birth,
                    Death = death,
                    Profession = prof,
                    Titles = titles
                };

                recordWriter.Write(id);
                recordWriter.Write((byte)name.Length);
                foreach (var ix in name) recordWriter.Write(ix);
                recordWriter.Write(birth);
                recordWriter.Write(death);
                recordWriter.Write((byte)prof.Length);
                foreach (var ix in prof) recordWriter.Write(ix);
                recordWriter.Write((byte)titles.Length);
                foreach (var ix in titles) recordWriter.Write(ix);
                
            }

            foreach (var name in names)
            {
                nameWriter.Write(name.Key);
                nameWriter.Write(name.Value);
            }
            foreach (var profession in professions)
            {
                professionWriter.Write(profession.Key);
                professionWriter.Write(profession.Value);
            }
        }

        public static void FasterExample()
        {
            //more here https://github.com/Microsoft/FASTER/issues/60

            using var log = Devices.CreateLogDevice("hlog.log"); // backing storage device
            using var store = new FasterKV<long, long>(1L << 20, // hash table size (number of 64-byte buckets)
                new LogSettings { LogDevice = log } // log settings (devices, page size, memory size, etc.)
            );

            // Create a session per sequence of interactions with FASTER
            using var s = store.NewSession(new SimpleFunctions<long, long>());
            long key = 1, value = 1, input = 10, output = 0;

            // Upsert and Read
            s.Upsert(ref key, ref value);
            s.Read(ref key, ref output);
            Debug.Assert(output == value);

            // Read-Modify-Write (add input to value)
            s.RMW(ref key, ref input);
            s.RMW(ref key, ref input);
            s.Read(ref key, ref output);
            Debug.Assert(output == value + 20);

        }
    }
}
