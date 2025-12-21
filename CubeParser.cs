using System.Globalization;
using System.IO;

namespace YMM4LUT
{
    internal class LUTData
    {
        public int Size { get; set; }
        public bool Is3D { get; set; }
        public byte[] Data { get; set; } = [];
    }

    internal static class CubeParser
    {
        public static LUTData? Parse(string path)
        {
            int size = 0;
            bool is3D = true;
            var colorList = new List<float>();

            using (var reader = new StreamReader(path))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

                    if (line.StartsWith("LUT_3D_SIZE"))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2) { size = int.Parse(parts[1]); is3D = true; }
                        continue;
                    }
                    if (line.StartsWith("LUT_1D_SIZE"))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2) { size = int.Parse(parts[1]); is3D = false; }
                        continue;
                    }

                    var colors = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (colors.Length >= 3)
                    {
                        if (float.TryParse(colors[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float r) &&
                            float.TryParse(colors[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float g) &&
                            float.TryParse(colors[2], NumberStyles.Any, CultureInfo.InvariantCulture, out float b))
                        {
                            colorList.Add(r);
                            colorList.Add(g);
                            colorList.Add(b);
                            colorList.Add(1.0f); // RGBA形式
                        }
                    }
                }
            }

            long expectedElements = is3D ? (long)size * size * size * 4 : (long)size * 4;
            if (size == 0 || colorList.Count < expectedElements) return null;

            float[] dataArray = colorList.ToArray();
            byte[] byteArray = new byte[dataArray.Length * 4];
            Buffer.BlockCopy(dataArray, 0, byteArray, 0, byteArray.Length);

            return new LUTData { Size = size, Is3D = is3D, Data = byteArray };
        }
    }
}