using System.Collections.Generic;
using System.Text;
using Transformalize.Transforms;

namespace Transformalize.Model
{
    public interface IField {
        string Schema { get; }
        string Entity { get; }
        string Parent { get; }
        string Name { get; }
        string Type { get; }
        string Alias { get; }
        bool Input { get; }
        int Length { get; }
        int Precision { get; }
        int Scale { get; }
        bool Output { get; }
        FieldType FieldType { get; set; }
        string SqlDataType { get; }
        string AsJoin(string left, string right);
        Dictionary<string, Xml> InnerXml { get; }
        object Default { get; }
        string Quote { get; }
        string XPath { get; }
        int Index { get; }
        FieldSqlWriter SqlWriter { get; }
        KeyValuePair<string, string> References { get; set; }
        ITransform[] Transforms { get; set; }
        StringBuilder StringBuilder { get; set; }
        bool Clustered { get; }
        bool UseStringBuilder { get; }
    }
}