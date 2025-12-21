using System.Globalization;
using System.IO;

namespace YMM4LUT
{
    internal class LUTData
    {
        public int Size { get; set; }
        public byte[] Data { get; set; } = [];
    }

    internal static class CubeParser
    {
        public static LUTData? Parse(string path)
        {
            int size = 0;
            var colorList = new List<float>(262144 * 4);

            using (var reader = new StreamReader(path))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0 || line[0] == '#') continue;

                    if (line.StartsWith("LUT_3D_SIZE"))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2) size = int.Parse(parts[1]);
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
                            colorList.Add(1.0f);
                        }
                    }
                }
            }

            if (size == 0 || colorList.Count < size * size * size * 4) return null;

            // float配列をbyte配列に変換（3Dデータのまま）
            float[] dataArray = colorList.ToArray();
            byte[] byteArray = new byte[dataArray.Length * 4];
            Buffer.BlockCopy(dataArray, 0, byteArray, 0, byteArray.Length);

            return new LUTData { Size = size, Data = byteArray };
        }
    }
}