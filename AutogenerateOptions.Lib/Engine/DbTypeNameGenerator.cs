using NJsonSchema;

namespace AutogenerateOptions.Lib.Engine;

public class DbTypeNameGenerator : ITypeNameGenerator
{
    public string Generate(JsonSchema schema, string? typeNameHint, IEnumerable<string> reservedTypeNames)
    {
        schema.AllowAdditionalProperties = false;
        schema.AdditionalPropertiesSchema = null;

        if (!reservedTypeNames.Contains("Databases") && typeNameHint != "Databases")
            return typeNameHint;

        if (typeNameHint == "StoredProcedures")
        {
            if(schema.Parent is JsonSchemaProperty parent && !string.IsNullOrEmpty(parent.Name))
                typeNameHint = parent.Name + "StoredProcedures";
        }

        return typeNameHint;
    }
}
