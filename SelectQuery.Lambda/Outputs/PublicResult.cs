using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SelectQuery.Results;

namespace SelectQuery.Lambda.Outputs
{
    public class PublicResult
    {
        public static Stream Serialize(Result result)
        {
            var output = new MemoryStream();

            result.Switch(
                direct =>
                {
                    output.WriteByte(1);
                    using var writer = new StreamWriter(output, Encoding.UTF8, 1024, true);
                    writer.Write(JsonConvert.SerializeObject(direct.Rows.Select(x => x.Fields)));
                },
                serialized =>
                {
                    output.WriteByte(2);
                    output.Write(serialized.Data);
                },
                indirect =>
                {
                    output.WriteByte(3);
                    using var writer = new StreamWriter(output, Encoding.UTF8, 1024, true);
                    writer.Write(indirect.Location);
                }
            );

            output.Seek(0, SeekOrigin.Begin);
            return output;
        }
        public static async Task<Result> DeserializeAsync(Stream stream)
        {
            var type = stream.ReadByte();

            switch (type)
            {
                case 1:
                    {
                        using var reader = new StreamReader(stream);
                        var data = reader.ReadToEnd();
                        var rows = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(data);
                        return new Result.Direct(rows.Select(x => new ResultRow(x)).ToList());
                    }
                case 2:
                    {
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms).ConfigureAwait(false);
                        return new Result.Serialized(ms.ToArray());
                    }
                case 3:
                    {
                        using var reader = new StreamReader(stream);
                        var data = reader.ReadToEnd();
                        return new Result.InDirect(new Uri(data));
                    }

                case -1: throw new InvalidOperationException("Expected type but got EOF");
                default: throw new ArgumentOutOfRangeException($"Expected type 1, 2, or 3 but got {type} instead");
            }
        }
    }
}
