using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Storage
{
    /// <summary>Encapsulates serialization for row items.</summary>
    /// <typeparam name="RowTypeEnum">Row Type enum</typeparam>
    public class RowSerializer<RowTypeEnum>
        where RowTypeEnum : struct, Enum
    {
        private readonly Func<Type, JsonTypeInfo?> _resolver;
        private readonly IImmutableDictionary<RowTypeEnum, Type> _RTIndex;
        private readonly IImmutableDictionary<Type, RowTypeEnum> _typeIndex;

        #region Constructors
        public RowSerializer(Func<Type, JsonTypeInfo?> resolver)
        {
            _resolver = resolver;
            _RTIndex = ImmutableDictionary<RowTypeEnum, Type>.Empty;
            _typeIndex = ImmutableDictionary<Type, RowTypeEnum>.Empty;
        }

        private RowSerializer(
            Func<Type, JsonTypeInfo?> resolver,
            IImmutableDictionary<RowTypeEnum, Type> RTIndex,
            IImmutableDictionary<Type, RowTypeEnum> typeIndex)
        {
            _resolver = resolver;
            _RTIndex = RTIndex;
            _typeIndex = typeIndex;
        }
        #endregion

        public RowSerializer<RowTypeEnum> AddType<T>(RowTypeEnum RT)
        {
            var type = typeof(T);

            return new RowSerializer<RowTypeEnum>(
                _resolver,
                _RTIndex.Add(RT, type),
                _typeIndex.Add(type, RT));
        }

        #region Serialize
        public string Serialize(object item)
        {
            if (_typeIndex.TryGetValue(item.GetType(), out var RT))
            {
                var itemText = JsonSerializer.Serialize(
                    item,
                    _resolver(item.GetType())!);
                var wrapperText = @$"{{ ""RT"" : ""{RT}"", ""row"" : {itemText} }}";

                return wrapperText + '\n';
            }
            else
            {
                throw new NotSupportedException(
                    $"Row type {item.GetType().Name} isn't supported");
            }
        }
        #endregion

        #region Deserialize
        public object Deserialize(string text)
        {
            var document = JsonDocument.Parse(text);

            if (document.RootElement.TryGetProperty("RT", out var RTElement))
            {
                var RTText = RTElement.GetString()!;

                if (Enum.TryParse<RowTypeEnum>(RTText, out var RT))
                {
                    var rowItemType = _RTIndex[RT];

                    if (document.RootElement.TryGetProperty("row", out var rowElement))
                    {
                        var rowElementText = rowElement.GetRawText();
                        var itemObject = JsonSerializer.Deserialize(
                            rowElementText,
                            _resolver(rowItemType)!);

                        if (itemObject != null)
                        {
                            return itemObject;
                        }
                        else
                        {
                            throw new InvalidDataException(
                                $"Can't deserialize row:  {rowElement.GetRawText()}");
                        }
                    }
                    else
                    {
                        throw new InvalidDataException($"Expected a property 'RT':  {text}");
                    }
                }
                else
                {
                    throw new InvalidDataException($"Unexpected 'RT':  '{RTText}'");
                }
            }
            else
            {
                throw new InvalidDataException($"Expect 'RT' JSON Property:  {text}");
            }
        }
        #endregion
    }
}