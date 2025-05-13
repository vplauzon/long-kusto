using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Runtime.Entity.RowItem
{
    [JsonSourceGenerationOptions(
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(FileVersionHeader))]
    [JsonSerializable(typeof(ViewHeader))]
    [JsonSerializable(typeof(ProcedureRunRow))]
    [JsonSerializable(typeof(ProcedureRunStepRow))]
    internal partial class RowJsonContext : JsonSerializerContext
    {
    }
}